using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileFormatAIStudio.Data;
using FileFormatAIStudio.Data.Entities;
using FileFormatAIStudio.Services.Benchmarking;
using FileFormatAIStudio.Services.Knowledgebase;
using FileFormatAIStudio.Services.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileFormatAIStudio.Services.Chat
{
    public class ChatAttachmentService : IChatAttachmentService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDocumentEnginePreferenceService _preferenceService;
        private readonly IDocumentParserFactory _parserFactory;
        private readonly IDocumentCategoryRegistry _categoryRegistry;
        private readonly ITextChunker _textChunker;
        private readonly IAsposeLicenseService? _licenseService;
        private readonly ILogger<ChatAttachmentService> _logger;
        private readonly string _storageDirectory;

        public ChatAttachmentService(
            AppDbContext dbContext,
            IDocumentEnginePreferenceService preferenceService,
            IDocumentParserFactory parserFactory,
            IDocumentCategoryRegistry categoryRegistry,
            ITextChunker textChunker,
            IAsposeLicenseService? licenseService = null,
            ILogger<ChatAttachmentService>? logger = null,
            string? storageDirectory = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
            _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
            _categoryRegistry = categoryRegistry ?? throw new ArgumentNullException(nameof(categoryRegistry));
            _textChunker = textChunker ?? throw new ArgumentNullException(nameof(textChunker));
            _licenseService = licenseService;
            _logger = logger ?? NullLogger<ChatAttachmentService>.Instance;

            if (string.IsNullOrWhiteSpace(storageDirectory))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _storageDirectory = Path.Combine(localAppData, "FileFormatAIStudio", "Attachments");
            }
            else
            {
                _storageDirectory = storageDirectory;
            }

            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        public async Task<ChatSessionAttachmentEntity> AttachDocumentAsync(
            Guid sessionId,
            string sourceFilePath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                throw new ArgumentException("Source file path must not be null or empty.", nameof(sourceFilePath));
            }

            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException($"Attachment source file was not found: '{sourceFilePath}'", sourceFilePath);
            }

            // Ensure session exists
            var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session == null)
            {
                throw new InvalidOperationException($"Chat session with ID '{sessionId}' does not exist.");
            }

            var fileInfo = new FileInfo(sourceFilePath);
            var attachmentId = Guid.NewGuid();
            string fileName = Path.GetFileName(sourceFilePath);
            string extension = fileInfo.Extension.ToLowerInvariant();

            // Destination directory for session attachments
            string sessionFolder = Path.Combine(_storageDirectory, sessionId.ToString());
            if (!Directory.Exists(sessionFolder))
            {
                Directory.CreateDirectory(sessionFolder);
            }

            string targetPath = Path.Combine(sessionFolder, $"{attachmentId}_{fileName}");
            File.Copy(sourceFilePath, targetPath, overwrite: true);

            // Determine category
            string categoryName = "Document";
            try
            {
                var cat = _categoryRegistry.ResolveCategory(extension);
                categoryName = cat.ToString();
            }
            catch
            {
                categoryName = GetFallbackCategory(extension);
            }

            var attachment = new ChatSessionAttachmentEntity
            {
                Id = attachmentId,
                SessionId = sessionId,
                FileName = fileName,
                FilePath = targetPath,
                FileType = extension,
                DocumentCategory = categoryName,
                FileSize = fileInfo.Length,
                Status = "Parsing",
                AttachedAt = DateTime.UtcNow
            };

            _dbContext.ChatSessionAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync(ct);

            // Parse document
            try
            {
                ct.ThrowIfCancellationRequested();

                var parser = await _preferenceService.ResolveParserForDocumentAsync(targetPath, ct)
                             ?? _parserFactory.ResolveParser(targetPath);

                if (parser == null)
                {
                    throw new NotSupportedException($"No registered parser engine supports file format '{extension}'.");
                }

                bool isAsposeEval = parser.EngineId.Contains("aspose", StringComparison.OrdinalIgnoreCase) &&
                                    !(_licenseService?.IsEngineLicensed(parser.EngineId) ?? false);

                string rawText = await parser.ExtractTextAsync(targetPath, ct);

                int tokenCount = Math.Max(1, (int)Math.Ceiling((rawText?.Length ?? 0) / 4.0));
                int chunkCount = string.IsNullOrWhiteSpace(rawText)
                    ? 0
                    : _textChunker.ChunkText(rawText).Count;

                attachment.ParserEngineUsed = isAsposeEval ? $"{parser.DisplayName} (Evaluation Mode)" : parser.DisplayName;
                attachment.RawExtractedText = rawText;
                attachment.TokenCount = tokenCount;
                attachment.ChunkCount = chunkCount;
                attachment.Status = "Ready";
                attachment.ErrorMessage = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to parse chat attachment '{FileName}' for session '{SessionId}'.", fileName, sessionId);
                attachment.Status = "Failed";
                attachment.ErrorMessage = ex.Message;
            }

            await _dbContext.SaveChangesAsync(ct);
            return attachment;
        }

        public async Task<IReadOnlyList<ChatSessionAttachmentEntity>> GetSessionAttachmentsAsync(
            Guid sessionId,
            CancellationToken ct = default)
        {
            return await _dbContext.ChatSessionAttachments
                .Where(a => a.SessionId == sessionId)
                .OrderBy(a => a.AttachedAt)
                .ToListAsync(ct);
        }

        public async Task<ChatSessionAttachmentEntity?> GetAttachmentAsync(
            Guid attachmentId,
            CancellationToken ct = default)
        {
            return await _dbContext.ChatSessionAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
        }

        public async Task<bool> DetachDocumentAsync(
            Guid attachmentId,
            CancellationToken ct = default)
        {
            var attachment = await _dbContext.ChatSessionAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment == null)
            {
                return false;
            }

            try
            {
                if (File.Exists(attachment.FilePath))
                {
                    File.Delete(attachment.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete stored attachment file '{FilePath}'.", attachment.FilePath);
            }

            _dbContext.ChatSessionAttachments.Remove(attachment);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<AttachmentBudgetResult> AssessBudgetAsync(
            Guid sessionId,
            int modelContextWindowTokens,
            int reservedPromptTokens = 2000,
            CancellationToken ct = default)
        {
            var attachments = await _dbContext.ChatSessionAttachments
                .Where(a => a.SessionId == sessionId && a.Status == "Ready")
                .ToListAsync(ct);

            return EvaluateBudget(attachments, modelContextWindowTokens, reservedPromptTokens);
        }

        public AttachmentBudgetResult EvaluateBudget(
            IReadOnlyList<ChatSessionAttachmentEntity> attachments,
            int modelContextWindowTokens,
            int reservedPromptTokens = 2000)
        {
            int contextWindow = modelContextWindowTokens > 0 ? modelContextWindowTokens : 8192;
            int reserved = Math.Max(0, reservedPromptTokens);
            int usableBudget = Math.Max(0, contextWindow - reserved);

            var activeAttachments = attachments ?? Array.Empty<ChatSessionAttachmentEntity>();
            int totalTokens = activeAttachments.Sum(a => a.TokenCount);

            var details = activeAttachments.Select(a => new AttachmentBudgetDetail
            {
                AttachmentId = a.Id,
                FileName = a.FileName,
                TokenCount = a.TokenCount,
                FitsWithinBudget = a.TokenCount <= usableBudget
            }).ToList();

            bool fitsInline = totalTokens <= usableBudget;

            return new AttachmentBudgetResult
            {
                TotalTokens = totalTokens,
                ContextWindowTokens = contextWindow,
                ReservedPromptTokens = reserved,
                UsableAttachmentBudgetTokens = usableBudget,
                FitsInline = fitsInline,
                Details = details
            };
        }

        private static string GetFallbackCategory(string extension) => extension.ToLowerInvariant() switch
        {
            ".docx" or ".doc" or ".rtf" or ".odt" => "Word",
            ".xlsx" or ".xls" or ".csv" or ".tsv" => "Excel",
            ".pptx" or ".ppt" => "PowerPoint",
            ".pdf" => "Pdf",
            ".txt" or ".md" or ".json" or ".xml" => "PlainText",
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tiff" => "Image",
            _ => "Document"
        };
    }
}


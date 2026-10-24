using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileFormatAIStudio.Data;
using FileFormatAIStudio.Data.Entities;
using FileFormatAIStudio.Services.Benchmarking;
using FileFormatAIStudio.Services.Chat;
using FileFormatAIStudio.Services.Knowledgebase;
using FileFormatAIStudio.Services.Parsing;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FileFormatAIStudio.Tests
{
    public class ChatAttachmentServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly string _tempDir;
        private readonly FakePreferenceService _preferenceService;
        private readonly FakeParserFactory _parserFactory;
        private readonly DocumentCategoryRegistry _categoryRegistry;
        private readonly FakeTextChunker _chunker;

        public ChatAttachmentServiceTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();

            _tempDir = Path.Combine(Path.GetTempPath(), "ChatAttachmentServiceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            _preferenceService = new FakePreferenceService();
            _parserFactory = new FakeParserFactory();
            _categoryRegistry = new DocumentCategoryRegistry();
            _chunker = new FakeTextChunker();
        }

        public void Dispose()
        {
            _connection.Dispose();
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        [Fact]
        public async Task AttachDocumentAsync_ValidFile_ParsesAndPersistsAttachment()
        {
            var sessionId = Guid.NewGuid();
            using (var context = new AppDbContext(_options))
            {
                context.Sessions.Add(new ChatSessionEntity { Id = sessionId, Title = "Test Chat" });
                await context.SaveChangesAsync();
            }

            string sourceFile = Path.Combine(_tempDir, "sample.pdf");
            await File.WriteAllTextAsync(sourceFile, "This is the source document content.");

            var testParser = new TestDocumentParser("pdfpig", "PdfPig Parser", "Extracted plain text from PDF.");
            _preferenceService.ParserToReturn = testParser;

            using var dbContext = new AppDbContext(_options);
            var service = new ChatAttachmentService(
                dbContext,
                _preferenceService,
                _parserFactory,
                _categoryRegistry,
                _chunker,
                storageDirectory: _tempDir);

            var attachment = await service.AttachDocumentAsync(sessionId, sourceFile);

            attachment.Should().NotBeNull();
            attachment.SessionId.Should().Be(sessionId);
            attachment.FileName.Should().Be("sample.pdf");
            attachment.FileType.Should().Be(".pdf");
            attachment.Status.Should().Be("Ready");
            attachment.ParserEngineUsed.Should().Be("PdfPig Parser");
            attachment.RawExtractedText.Should().Be("Extracted plain text from PDF.");
            attachment.TokenCount.Should().BeGreaterThan(0);
            attachment.ChunkCount.Should().Be(1);
            File.Exists(attachment.FilePath).Should().BeTrue();

            // Verify in DB
            using var verifyContext = new AppDbContext(_options);
            var saved = await verifyContext.ChatSessionAttachments.FindAsync(attachment.Id);
            saved.Should().NotBeNull();
            saved!.Status.Should().Be("Ready");
        }

        [Fact]
        public async Task AttachDocumentAsync_NoParserAvailable_SetsStatusToFailed()
        {
            var sessionId = Guid.NewGuid();
            using (var context = new AppDbContext(_options))
            {
                context.Sessions.Add(new ChatSessionEntity { Id = sessionId, Title = "Test Chat" });
                await context.SaveChangesAsync();
            }

            string sourceFile = Path.Combine(_tempDir, "unsupported.xyz");
            await File.WriteAllTextAsync(sourceFile, "Binary or unknown content");

            _preferenceService.ParserToReturn = null;
            _parserFactory.ParserToReturn = null;

            using var dbContext = new AppDbContext(_options);
            var service = new ChatAttachmentService(
                dbContext,
                _preferenceService,
                _parserFactory,
                _categoryRegistry,
                _chunker,
                storageDirectory: _tempDir);

            var attachment = await service.AttachDocumentAsync(sessionId, sourceFile);

            attachment.Should().NotBeNull();
            attachment.Status.Should().Be("Failed");
            attachment.ErrorMessage.Should().Contain("No registered parser engine");
        }

        [Fact]
        public async Task DetachDocumentAsync_RemovesFromDatabaseAndDeletesFile()
        {
            var sessionId = Guid.NewGuid();
            var attachmentId = Guid.NewGuid();
            string storedFile = Path.Combine(_tempDir, "stored.pdf");
            await File.WriteAllTextAsync(storedFile, "Stored file text");

            using (var context = new AppDbContext(_options))
            {
                context.Sessions.Add(new ChatSessionEntity { Id = sessionId, Title = "Chat" });
                context.ChatSessionAttachments.Add(new ChatSessionAttachmentEntity
                {
                    Id = attachmentId,
                    SessionId = sessionId,
                    FileName = "stored.pdf",
                    FilePath = storedFile,
                    FileType = ".pdf",
                    DocumentCategory = "Pdf",
                    Status = "Ready"
                });
                await context.SaveChangesAsync();
            }

            using var dbContext = new AppDbContext(_options);
            var service = new ChatAttachmentService(
                dbContext,
                _preferenceService,
                _parserFactory,
                _categoryRegistry,
                _chunker,
                storageDirectory: _tempDir);

            var detached = await service.DetachDocumentAsync(attachmentId);

            detached.Should().BeTrue();
            File.Exists(storedFile).Should().BeFalse();

            using var verifyContext = new AppDbContext(_options);
            var exists = await verifyContext.ChatSessionAttachments.AnyAsync(a => a.Id == attachmentId);
            exists.Should().BeFalse();
        }

        [Fact]
        public void EvaluateBudget_SmallAttachments_FitsInline()
        {
            using var dbContext = new AppDbContext(_options);
            var service = new ChatAttachmentService(
                dbContext,
                _preferenceService,
                _parserFactory,
                _categoryRegistry,
                _chunker,
                storageDirectory: _tempDir);

            var attachments = new[]
            {
                new ChatSessionAttachmentEntity { Id = Guid.NewGuid(), FileName = "a.txt", TokenCount = 500 },
                new ChatSessionAttachmentEntity { Id = Guid.NewGuid(), FileName = "b.pdf", TokenCount = 1200 }
            };

            var budget = service.EvaluateBudget(attachments, modelContextWindowTokens: 8192, reservedPromptTokens: 2000);

            budget.TotalTokens.Should().Be(1700);
            budget.ContextWindowTokens.Should().Be(8192);
            budget.ReservedPromptTokens.Should().Be(2000);
            budget.UsableAttachmentBudgetTokens.Should().Be(6192);
            budget.FitsInline.Should().BeTrue();
            budget.RequiresEphemeralChunking.Should().BeFalse();
            budget.Details.Should().HaveCount(2);
            budget.Details.All(d => d.FitsWithinBudget).Should().BeTrue();
        }

        [Fact]
        public void EvaluateBudget_OversizedAttachments_RequiresEphemeralChunking()
        {
            using var dbContext = new AppDbContext(_options);
            var service = new ChatAttachmentService(
                dbContext,
                _preferenceService,
                _parserFactory,
                _categoryRegistry,
                _chunker,
                storageDirectory: _tempDir);

            var attachments = new[]
            {
                new ChatSessionAttachmentEntity { Id = Guid.NewGuid(), FileName = "huge_manual.pdf", TokenCount = 15000 }
            };

            var budget = service.EvaluateBudget(attachments, modelContextWindowTokens: 8192, reservedPromptTokens: 2000);

            budget.TotalTokens.Should().Be(15000);
            budget.FitsInline.Should().BeFalse();
            budget.RequiresEphemeralChunking.Should().BeTrue();
            budget.Details[0].FitsWithinBudget.Should().BeFalse();
        }

        private sealed class TestDocumentParser : IDocumentParser
        {
            public string EngineId { get; }
            public string DisplayName { get; }
            public DocumentCategory Category => DocumentCategory.Pdf;
            public int Priority => 100;
            public bool IsAvailable => true;
            public IReadOnlySet<string> SupportedExtensions => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };

            private readonly string _textToReturn;

            public TestDocumentParser(string engineId, string displayName, string textToReturn)
            {
                EngineId = engineId;
                DisplayName = displayName;
                _textToReturn = textToReturn;
            }

            public Task<string> ExtractTextAsync(string filePath, CancellationToken ct = default)
            {
                return Task.FromResult(_textToReturn);
            }
        }

        private sealed class FakePreferenceService : IDocumentEnginePreferenceService
        {
            public IDocumentParser? ParserToReturn { get; set; }

            public Task<string> GetPreferredEngineIdAsync(DocumentCategory category, CancellationToken cancellationToken = default) =>
                Task.FromResult("Auto");

            public Task SetPreferredEngineIdAsync(DocumentCategory category, string engineId, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;

            public Task<IReadOnlyDictionary<DocumentCategory, string>> GetAllPreferencesAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyDictionary<DocumentCategory, string>>(new Dictionary<DocumentCategory, string>());

            public Task<IDocumentParser?> ResolveParserForDocumentAsync(string filePath, CancellationToken cancellationToken = default) =>
                Task.FromResult(ParserToReturn);

            public Task<string?> GetBenchmarkWinnerEngineIdAsync(DocumentCategory category, CancellationToken cancellationToken = default) =>
                Task.FromResult<string?>(null);

            public Task<string?> GetBenchmarkWinnerDisplayNameAsync(DocumentCategory category, CancellationToken cancellationToken = default) =>
                Task.FromResult<string?>(null);

            public Task RecordBenchmarkWinnerAsync(DocumentCategory category, string engineId, string displayName, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }

        private sealed class FakeParserFactory : IDocumentParserFactory
        {
            public IDocumentParser? ParserToReturn { get; set; }

            public IReadOnlyList<IDocumentParser> GetAllParsers() =>
                ParserToReturn != null ? new[] { ParserToReturn } : Array.Empty<IDocumentParser>();

            public IReadOnlyList<IDocumentParser> GetParsersByCategory(DocumentCategory category) =>
                GetAllParsers();

            public IDocumentParser? GetParser(string engineId) => ParserToReturn;

            public IDocumentParser? ResolveParser(string filePathOrExtension, string? engineId = null) => ParserToReturn;

            public IReadOnlySet<string> GetSupportedExtensions() =>
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };

            public Task<string> ExtractTextAsync(string filePath, string? engineId = null, CancellationToken cancellationToken = default) =>
                Task.FromResult(ParserToReturn?.ExtractTextAsync(filePath, cancellationToken).Result ?? string.Empty);
        }

        private sealed class FakeTextChunker : ITextChunker
        {
            public IReadOnlyList<TextChunk> ChunkText(string text, ChunkingOptions? options = null)
            {
                return new[] { new TextChunk { Id = Guid.NewGuid(), SequenceIndex = 0, Content = text, CharacterStart = 0, CharacterEnd = text.Length, EstimatedTokenCount = 10 } };
            }
        }
    }
}


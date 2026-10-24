using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FileFormatAIStudio.Data;
using FileFormatAIStudio.Data.Entities;
using FileFormatAIStudio.Services.Chat;
using FileFormatAIStudio.Services.Knowledgebase;
using FileFormatAIStudio.Services.Settings;
using FileFormatAIStudio.ViewModels;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Xunit;

namespace FileFormatAIStudio.Tests
{
    public class DocumentChatAttachmentWorkflowTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public DocumentChatAttachmentWorkflowTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        [Theory]
        [InlineData("Word", "\uE8A5", "#2B579A")]
        [InlineData("Excel", "\uF0E3", "#217346")]
        [InlineData("PowerPoint", "\uE8B8", "#D24726")]
        [InlineData("Pdf", "\uEA90", "#E81123")]
        [InlineData("Image", "\uEB9F", "#9B4F96")]
        [InlineData("PlainText", "\uE8A5", "#0078D7")]
        public void ChatAttachmentItemViewModel_FormattingMatchesCategory(string category, string expectedGlyph, string expectedColor)
        {
            var entity = new ChatSessionAttachmentEntity
            {
                Id = Guid.NewGuid(),
                FileName = "test_doc",
                DocumentCategory = category,
                Status = "Ready",
                TokenCount = 2500
            };

            var vm = new ChatAttachmentItemViewModel(entity);

            vm.CategoryGlyph.Should().Be(expectedGlyph);
            vm.CategoryColorHex.Should().Be(expectedColor);
            vm.TokenBadgeText.Should().Be("2.5k tokens");
            vm.IsReady.Should().BeTrue();
            vm.IsParsing.Should().BeFalse();
            vm.IsFailed.Should().BeFalse();
        }

        [Fact]
        public void ChatAttachmentItemViewModel_StatusFlagsReflectState()
        {
            var entity = new ChatSessionAttachmentEntity
            {
                Id = Guid.NewGuid(),
                FileName = "doc.pdf",
                DocumentCategory = "Pdf",
                Status = "Parsing",
                TokenCount = 0
            };

            var vm = new ChatAttachmentItemViewModel(entity);
            vm.IsParsing.Should().BeTrue();
            vm.TokenBadgeText.Should().Be("Parsing...");

            entity.Status = "Failed";
            entity.ErrorMessage = "Corrupted file";
            vm.UpdateFromEntity(entity);
            vm.IsFailed.Should().BeTrue();
            vm.TokenBadgeText.Should().Be("Failed");

            entity.Status = "Ready";
            entity.TokenCount = 350;
            vm.UpdateFromEntity(entity);
            vm.IsReady.Should().BeTrue();
            vm.TokenBadgeText.Should().Be("350 tokens");
        }

        [Fact]
        public void RankChunksForPrompt_ReturnsTopScoredChunksBasedOnTerms()
        {
            var chunks = new List<TextChunk>
            {
                new() { Id = Guid.NewGuid(), SequenceIndex = 0, Content = "The quarterly revenue was $5 million with high gross margins.", CharacterStart = 0, CharacterEnd = 60, EstimatedTokenCount = 15 },
                new() { Id = Guid.NewGuid(), SequenceIndex = 1, Content = "Weather forecast predicts snow and cold temperatures in Chicago.", CharacterStart = 61, CharacterEnd = 120, EstimatedTokenCount = 15 },
                new() { Id = Guid.NewGuid(), SequenceIndex = 2, Content = "Revenue trends and profit margins exceeded analyst expectations.", CharacterStart = 121, CharacterEnd = 180, EstimatedTokenCount = 15 }
            };

            var ranked = ChatViewModel.RankChunksForPrompt("What was the revenue and gross margin?", chunks, topK: 2);

            ranked.Should().HaveCount(2);
            ranked[0].Content.Should().Contain("quarterly revenue");
            ranked[1].Content.Should().Contain("Revenue trends");
        }

        [Fact]
        public void ViewExtractedTextViewModel_LoadAttachment_ComputesStatistics()
        {
            var mockKbService = new FakeKnowledgebaseService();
            var vm = new ViewExtractedTextViewModel(mockKbService);

            var attachment = new ChatSessionAttachmentEntity
            {
                Id = Guid.NewGuid(),
                FileName = "AnnualReport.pdf",
                FileType = ".pdf",
                FileSize = 2048,
                ParserEngineUsed = "AsposePdfParser",
                Status = "Ready",
                RawExtractedText = "First line of extracted text.\nSecond line with some numbers 12345.\nThird line ending.",
                TokenCount = 25,
                ChunkCount = 1
            };

            vm.LoadAttachment(attachment);

            vm.DocumentName.Should().Be("AnnualReport.pdf");
            vm.FileType.Should().Be("PDF");
            vm.FileSize.Should().Be(2048);
            vm.Status.Should().Be("Ready");
            vm.ExtractedText.Should().Be(attachment.RawExtractedText);
            vm.CharacterCount.Should().Be(attachment.RawExtractedText.Length);
            vm.WordCount.Should().Be(14);
            vm.LineCount.Should().Be(3);
            vm.HasExtractedText.Should().BeTrue();
            vm.HasError.Should().BeFalse();
        }

        [Fact]
        public async Task ChatViewModel_AttachAndDetachFiles_UpdatesCollectionAndFlags()
        {
            var sessionId = Guid.NewGuid();
            var session = new ChatSessionEntity { Id = sessionId, Title = "Test Session" };

            using (var context = new AppDbContext(_options))
            {
                context.Sessions.Add(session);
                await context.SaveChangesAsync();
            }

            var fakeSessionService = new FakeChatSessionService(session);
            var fakeExecService = new FakeChatExecutionService();
            var fakeSettingsService = new FakeSettingsService();
            var fakeKbService = new FakeKnowledgebaseService();
            var fakeAttachmentService = new FakeChatAttachmentService();
            var chunker = new TextChunker();

            var vm = new ChatViewModel(
                fakeSessionService,
                fakeExecService,
                fakeSettingsService,
                fakeKbService,
                fakeAttachmentService,
                chunker);

            await vm.InitializeAsync(sessionId);

            vm.HasAttachedDocuments.Should().BeFalse();

            // Attach files
            await vm.AttachFilesAsync(new[] { @"C:\test1.pdf", @"C:\test2.docx" });

            vm.AttachedDocuments.Should().HaveCount(2);
            vm.HasAttachedDocuments.Should().BeTrue();
            vm.AttachedDocumentsCountText.Should().Be("2 documents attached");

            // Detach 1 file
            var itemToDetach = vm.AttachedDocuments[0];
            await vm.DetachDocumentAsync(itemToDetach);

            vm.AttachedDocuments.Should().HaveCount(1);
            vm.AttachedDocumentsCountText.Should().Be("1 document attached");
        }

        [Fact]
        public async Task ChatViewModel_SendMessage_WithInlineAttachments_AugmentsPromptAndGeneratesCitations()
        {
            var sessionId = Guid.NewGuid();
            var session = new ChatSessionEntity { Id = sessionId, Title = "Test Session" };

            using (var context = new AppDbContext(_options))
            {
                context.Sessions.Add(session);
                await context.SaveChangesAsync();
            }

            var fakeSessionService = new FakeChatSessionService(session);
            var fakeExecService = new FakeChatExecutionService();
            var fakeSettingsService = new FakeSettingsService();
            var fakeKbService = new FakeKnowledgebaseService();
            var fakeAttachmentService = new FakeChatAttachmentService();
            var chunker = new TextChunker();

            var vm = new ChatViewModel(
                fakeSessionService,
                fakeExecService,
                fakeSettingsService,
                fakeKbService,
                fakeAttachmentService,
                chunker);

            await vm.InitializeAsync(sessionId);

            // Add ready attachment
            await vm.AttachFilesAsync(new[] { @"C:\summary.txt" });
            vm.AttachedDocuments[0].RawExtractedText = "Company profit was 45% above expectations in Q2.";
            vm.AttachedDocuments[0].Status = "Ready";
            vm.AttachedDocuments[0].TokenCount = 15;

            // Send prompt
            vm.InputText = "What was the profit?";
            await vm.SendMessageAsync();

            // Verify Assistant message received citations
            var assistantMsg = vm.Messages.FirstOrDefault(m => m.IsAssistant);
            assistantMsg.Should().NotBeNull();
            assistantMsg!.HasCitations.Should().BeTrue();
            assistantMsg.Citations.Should().HaveCount(1);
            assistantMsg.Citations[0].DocumentName.Should().Be("summary.txt");
            assistantMsg.Citations[0].SourceHeaderInfo.Should().Contain("Attached: summary.txt");

            // Verify execution history received augmented context
            fakeExecService.LastRecordedHistory.Should().NotBeNull();
            var systemMsg = fakeExecService.LastRecordedHistory!.FirstOrDefault(h => h.Role == ChatRole.System);
            systemMsg.Should().NotBeNull();
            systemMsg!.Text.Should().Contain("--- BEGIN ATTACHED DOCUMENTS ---");
            systemMsg.Text.Should().Contain("Company profit was 45% above expectations");
        }

        // Fake Implementations
        private sealed class FakeChatAttachmentService : IChatAttachmentService
        {
            private readonly List<ChatSessionAttachmentEntity> _attachments = new();

            public Task<ChatSessionAttachmentEntity> AttachDocumentAsync(Guid sessionId, string sourceFilePath, CancellationToken ct = default)
            {
                string fileName = Path.GetFileName(sourceFilePath);
                var att = new ChatSessionAttachmentEntity
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    FileName = fileName,
                    FilePath = sourceFilePath,
                    FileType = Path.GetExtension(sourceFilePath),
                    DocumentCategory = "Pdf",
                    Status = "Ready",
                    TokenCount = 50,
                    RawExtractedText = $"Sample text for {fileName}"
                };
                _attachments.Add(att);
                return Task.FromResult(att);
            }

            public Task<IReadOnlyList<ChatSessionAttachmentEntity>> GetSessionAttachmentsAsync(Guid sessionId, CancellationToken ct = default)
            {
                return Task.FromResult<IReadOnlyList<ChatSessionAttachmentEntity>>(_attachments.Where(a => a.SessionId == sessionId).ToList());
            }

            public Task<ChatSessionAttachmentEntity?> GetAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
            {
                return Task.FromResult(_attachments.FirstOrDefault(a => a.Id == attachmentId));
            }

            public Task<bool> DetachDocumentAsync(Guid attachmentId, CancellationToken ct = default)
            {
                var item = _attachments.FirstOrDefault(a => a.Id == attachmentId);
                if (item != null)
                {
                    _attachments.Remove(item);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }

            public Task<AttachmentBudgetResult> AssessBudgetAsync(Guid sessionId, int modelContextWindowTokens, int reservedPromptTokens = 2000, CancellationToken ct = default)
            {
                return Task.FromResult(EvaluateBudget(_attachments.Where(a => a.SessionId == sessionId).ToList(), modelContextWindowTokens, reservedPromptTokens));
            }

            public AttachmentBudgetResult EvaluateBudget(IReadOnlyList<ChatSessionAttachmentEntity> attachments, int modelContextWindowTokens, int reservedPromptTokens = 2000)
            {
                int total = attachments.Sum(a => a.TokenCount);
                int usable = Math.Max(0, modelContextWindowTokens - reservedPromptTokens);
                return new AttachmentBudgetResult
                {
                    TotalTokens = total,
                    ContextWindowTokens = modelContextWindowTokens,
                    ReservedPromptTokens = reservedPromptTokens,
                    UsableAttachmentBudgetTokens = usable,
                    FitsInline = total <= usable
                };
            }
        }

        private sealed class FakeChatSessionService : IChatSessionService
        {
            public List<ChatSessionEntity> Sessions { get; } = new();

            public FakeChatSessionService(ChatSessionEntity session)
            {
                Sessions.Add(session);
            }

            public Task<List<ChatSessionEntity>> GetSessionsAsync() => Task.FromResult(Sessions);
            public Task<ChatSessionEntity?> GetEmptySessionAsync() => Task.FromResult(Sessions.FirstOrDefault(s => s.Messages.Count == 0));
            public Task<ChatSessionEntity> CreateSessionAsync(string title = "New Chat", Guid? defaultModelId = null)
            {
                var s = new ChatSessionEntity { Id = Guid.NewGuid(), Title = title, SelectedModelId = defaultModelId };
                Sessions.Add(s);
                return Task.FromResult(s);
            }
            public Task<ChatSessionEntity?> GetSessionAsync(Guid sessionId) => Task.FromResult(Sessions.FirstOrDefault(s => s.Id == sessionId));
            public Task UpdateSessionTitleAsync(Guid sessionId, string title) => Task.CompletedTask;
            public Task UpdateSessionModelAsync(Guid sessionId, Guid modelId) => Task.CompletedTask;
            public Task DeleteSessionAsync(Guid sessionId) => Task.CompletedTask;
            public Task<ChatMessageEntity> AddMessageAsync(Guid sessionId, string role, string content, string? citationJson = null)
            {
                var msg = new ChatMessageEntity { SessionId = sessionId, Role = role, Content = content, CitationJson = citationJson };
                var s = Sessions.FirstOrDefault(x => x.Id == sessionId);
                s?.Messages.Add(msg);
                return Task.FromResult(msg);
            }
            public Task<List<KnowledgebaseEntity>> GetAttachedKnowledgebasesAsync(Guid sessionId) => Task.FromResult(new List<KnowledgebaseEntity>());
            public Task AttachKnowledgebaseAsync(Guid sessionId, Guid knowledgebaseId) => Task.CompletedTask;
            public Task DetachKnowledgebaseAsync(Guid sessionId, Guid knowledgebaseId) => Task.CompletedTask;
        }

        private sealed class FakeChatExecutionService : IChatExecutionService
        {
            public List<ChatMessage>? LastRecordedHistory { get; private set; }

            public async IAsyncEnumerable<ChatResponseUpdate> StreamResponseAsync(
                ProviderConfigEntity provider,
                string modelId,
                IEnumerable<ChatMessage> history,
                [EnumeratorCancellation] CancellationToken ct = default)
            {
                LastRecordedHistory = history.ToList();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "Based on the attached document, the company profit was 45% above expectations.");
                await Task.CompletedTask;
            }

            public Task<GroundedRagResult> RetrieveGroundedContextAsync(
                string userPrompt,
                IReadOnlyList<KnowledgebaseEntity> attachedKnowledgebases,
                int topK = 5,
                float minSimilarity = 0.35f,
                CancellationToken ct = default)
            {
                return Task.FromResult(new GroundedRagResult(Array.Empty<CitationReference>(), string.Empty));
            }
        }

        private sealed class FakeSettingsService : ISettingsService
        {
            private readonly ModelConfigEntity _model = new()
            {
                Id = Guid.NewGuid(),
                DisplayName = "Mock Model",
                ModelId = "mock-model",
                IsDefault = true,
                Provider = new ProviderConfigEntity { Name = "MockProvider", ProviderType = "OpenAI", EndpointUrl = "https://mock" }
            };

            public Task<List<ProviderConfigEntity>> GetProvidersAsync() => Task.FromResult(new List<ProviderConfigEntity> { _model.Provider! });
            public Task<ProviderConfigEntity?> GetProviderByIdAsync(Guid providerId) => Task.FromResult<ProviderConfigEntity?>(_model.Provider);
            public Task SaveProviderAsync(ProviderConfigEntity provider) => Task.CompletedTask;
            public Task DeleteProviderAsync(Guid providerId) => Task.CompletedTask;
            public Task AddModelAsync(ModelConfigEntity model) => Task.CompletedTask;
            public Task DeleteModelAsync(Guid modelId) => Task.CompletedTask;
            public Task<List<ModelConfigEntity>> GetAllEnabledModelsAsync() => Task.FromResult(new List<ModelConfigEntity> { _model });
            public Task RestoreDefaultProvidersAsync() => Task.CompletedTask;
        }

        private sealed class FakeKnowledgebaseService : IKnowledgebaseService
        {
            public Task<List<KnowledgebaseEntity>> GetKnowledgebasesAsync(CancellationToken ct = default) => Task.FromResult(new List<KnowledgebaseEntity>());
            public Task<KnowledgebaseEntity?> GetKnowledgebaseByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<KnowledgebaseEntity?>(null);
            public Task<KnowledgebaseEntity> CreateKnowledgebaseAsync(CreateKnowledgebaseRequest request, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<KnowledgebaseEntity> UpdateKnowledgebaseAsync(Guid id, UpdateKnowledgebaseRequest request, CancellationToken ct = default) => throw new NotImplementedException();
            public Task DeleteKnowledgebaseAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
            public Task<List<KnowledgebaseDocumentEntity>> GetDocumentsAsync(Guid knowledgebaseId, CancellationToken ct = default) => Task.FromResult(new List<KnowledgebaseDocumentEntity>());
            public Task<KnowledgebaseDocumentEntity?> GetDocumentByIdAsync(Guid documentId, CancellationToken ct = default) => Task.FromResult<KnowledgebaseDocumentEntity?>(null);
            public Task DeleteDocumentAsync(Guid documentId, CancellationToken ct = default) => Task.CompletedTask;
            public Task<List<KnowledgebaseDocumentEntity>> IngestDocumentsAsync(Guid knowledgebaseId, IEnumerable<string> filePaths, IngestionOptions? options = null, IProgress<IndexingProgressReport>? progress = null, CancellationToken ct = default) => Task.FromResult(new List<KnowledgebaseDocumentEntity>());
            public Task<string> GetDocumentExtractedTextAsync(Guid documentId, CancellationToken ct = default) => Task.FromResult(string.Empty);
            public Task<List<DocumentChunkEntity>> GetDocumentChunksAsync(Guid documentId, CancellationToken ct = default) => Task.FromResult(new List<DocumentChunkEntity>());
        }
    }
}


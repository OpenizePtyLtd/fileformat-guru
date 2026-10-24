using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileFormatAIStudio.Data.Entities;
using FileFormatAIStudio.Services.Chat;
using FileFormatAIStudio.Services.Knowledgebase;
using FileFormatAIStudio.Services.Settings;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace FileFormatAIStudio.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly IChatSessionService _sessionService;
        private readonly IChatExecutionService _chatExecutionService;
        private readonly ISettingsService _settingsService;
        private readonly IKnowledgebaseService _knowledgebaseService;
        private readonly IChatAttachmentService? _attachmentService;
        private readonly ITextChunker? _textChunker;

        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private ChatSessionEntity? _currentSession;

        [ObservableProperty]
        private ObservableCollection<ChatMessageItemViewModel> _messages = new();

        [ObservableProperty]
        private ObservableCollection<ModelConfigEntity> _availableModels = new();

        [ObservableProperty]
        private ModelConfigEntity? _selectedModel;

        [ObservableProperty]
        private ObservableCollection<KnowledgebaseEntity> _attachedKnowledgebases = new();

        [ObservableProperty]
        private ObservableCollection<KnowledgebaseEntity> _availableKnowledgebasesToAttach = new();

        [ObservableProperty]
        private ObservableCollection<ChatAttachmentItemViewModel> _attachedDocuments = new();

        [ObservableProperty]
        private bool _isAttaching;

        public bool HasAttachedKnowledgebases => AttachedKnowledgebases.Count > 0;
        public bool HasAvailableKnowledgebases => AvailableKnowledgebasesToAttach.Count > 0;
        public string AttachedKnowledgebasesCountText => AttachedKnowledgebases.Count == 1
            ? "1 knowledgebase attached"
            : $"{AttachedKnowledgebases.Count} knowledgebases attached";

        public bool HasAttachedDocuments => AttachedDocuments.Count > 0;
        public string AttachedDocumentsCountText => AttachedDocuments.Count == 1
            ? "1 document attached"
            : $"{AttachedDocuments.Count} documents attached";

        [ObservableProperty]
        private string _inputText = string.Empty;

        [ObservableProperty]
        private bool _isGenerating;

        [ObservableProperty]
        private string _statusText = "Ready";

        public event Action? MessageAdded;

        public ChatViewModel(
            IChatSessionService sessionService,
            IChatExecutionService chatExecutionService,
            ISettingsService settingsService,
            IKnowledgebaseService knowledgebaseService,
            IChatAttachmentService? attachmentService = null,
            ITextChunker? textChunker = null)
        {
            _sessionService = sessionService;
            _chatExecutionService = chatExecutionService;
            _settingsService = settingsService;
            _knowledgebaseService = knowledgebaseService;
            _attachmentService = attachmentService;
            _textChunker = textChunker;
        }

        public async Task InitializeAsync(Guid? sessionId = null)
        {
            await LoadModelsAsync();

            if (sessionId.HasValue)
            {
                await LoadSessionAsync(sessionId.Value);
            }
            else
            {
                var sessions = await _sessionService.GetSessionsAsync();
                if (sessions.Count > 0)
                {
                    await LoadSessionAsync(sessions[0].Id);
                }
                else
                {
                    var newSession = await _sessionService.CreateSessionAsync("New Chat");
                    await LoadSessionAsync(newSession.Id);
                }
            }
        }

        public async Task LoadModelsAsync()
        {
            var models = await _settingsService.GetAllEnabledModelsAsync();
            AvailableModels.Clear();
            foreach (var m in models)
            {
                AvailableModels.Add(m);
            }

            if (SelectedModel == null && AvailableModels.Count > 0)
            {
                SelectedModel = AvailableModels.FirstOrDefault(m => m.IsDefault) ?? AvailableModels[0];
            }
        }

        public async Task LoadSessionAsync(Guid sessionId)
        {
            CurrentSession = await _sessionService.GetSessionAsync(sessionId);
            Messages.Clear();

            if (CurrentSession != null)
            {
                if (CurrentSession.SelectedModelId.HasValue)
                {
                    var matchingModel = AvailableModels.FirstOrDefault(m => m.Id == CurrentSession.SelectedModelId.Value);
                    if (matchingModel != null)
                    {
                        SelectedModel = matchingModel;
                    }
                }

                foreach (var msg in CurrentSession.Messages)
                {
                    var msgVm = new ChatMessageItemViewModel
                    {
                        Id = msg.Id,
                        Role = msg.Role,
                        Content = msg.Content,
                        Timestamp = msg.Timestamp
                    };

                    if (!string.IsNullOrWhiteSpace(msg.CitationJson))
                    {
                        msgVm.LoadCitationsFromJson(msg.CitationJson);
                    }

                    Messages.Add(msgVm);
                }

                await RefreshKnowledgebasesAsync();
                await RefreshAttachmentsAsync();
            }
            else
            {
                AttachedKnowledgebases.Clear();
                AvailableKnowledgebasesToAttach.Clear();
                AttachedDocuments.Clear();
                UpdateKnowledgebaseVisibilityFlags();
            }

            MessageAdded?.Invoke();
        }

        partial void OnSelectedModelChanged(ModelConfigEntity? value)
        {
            if (value != null && CurrentSession != null)
            {
                _ = _sessionService.UpdateSessionModelAsync(CurrentSession.Id, value.Id);
            }
        }

        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText) || IsGenerating || CurrentSession == null)
            {
                return;
            }

            if (SelectedModel == null || SelectedModel.Provider == null)
            {
                StatusText = "Please select an enabled model in Settings first.";
                return;
            }

            string userPrompt = InputText.Trim();
            InputText = string.Empty;

            // 1. Save and display User Message
            var userMsgEntity = await _sessionService.AddMessageAsync(CurrentSession.Id, "User", userPrompt);
            CurrentSession.Messages.Add(userMsgEntity);
            if (CurrentSession.Title == "New Chat")
            {
                CurrentSession.Title = userPrompt.Length > 30 ? userPrompt.Substring(0, 30) + "..." : userPrompt;
            }

            var userMsgVm = new ChatMessageItemViewModel
            {
                Id = userMsgEntity.Id,
                Role = "User",
                Content = userPrompt,
                Timestamp = userMsgEntity.Timestamp
            };
            Messages.Add(userMsgVm);
            MessageAdded?.Invoke();

            // 2. Prepare assistant placeholder
            var assistantMsgVm = new ChatMessageItemViewModel
            {
                Role = "Assistant",
                Content = string.Empty,
                Timestamp = DateTime.Now
            };
            Messages.Add(assistantMsgVm);
            MessageAdded?.Invoke();

            // 3. Grounded RAG Retrieval (if attached KBs exist)
            IReadOnlyList<CitationReference> citations = Array.Empty<CitationReference>();
            string? groundedSystemPrompt = null;
            _cts = new CancellationTokenSource();

            if (AttachedKnowledgebases.Count > 0)
            {
                StatusText = "Retrieving context from attached knowledgebases...";
                try
                {
                    var ragResult = await _chatExecutionService.RetrieveGroundedContextAsync(
                        userPrompt,
                        AttachedKnowledgebases.ToList(),
                        ct: _cts.Token);

                    citations = ragResult.Citations;
                    groundedSystemPrompt = ragResult.GroundedSystemPrompt;

                    if (citations.Count > 0)
                    {
                        assistantMsgVm.LoadCitations(citations);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    StatusText = $"Context retrieval notice: {ex.Message}";
                }
            }

            // 3b. Attached Documents Processing & Prompt Augmentation (TASK-110 & TASK-111)
            var readyAttachments = AttachedDocuments.Where(a => a.IsReady && !string.IsNullOrWhiteSpace(a.RawExtractedText)).ToList();
            var combinedCitations = new List<CitationReference>(citations);

            if (readyAttachments.Count > 0)
            {
                int modelWindow = 8192;
                var budget = _attachmentService != null
                    ? _attachmentService.EvaluateBudget(readyAttachments.Select(a => a.Entity).ToList(), modelWindow, reservedPromptTokens: 2500)
                    : new AttachmentBudgetResult { FitsInline = true };

                var docSb = new System.Text.StringBuilder();
                int nextCitationIndex = combinedCitations.Count + 1;

                if (budget.FitsInline)
                {
                    // Inline prompt augmentation (TASK-110)
                    docSb.AppendLine("--- BEGIN ATTACHED DOCUMENTS ---");
                    foreach (var att in readyAttachments)
                    {
                        docSb.AppendLine($"[Attached Document: {att.FileName} (Category: {att.DocumentCategory}, Est. Tokens: {att.TokenCount})]");
                        docSb.AppendLine(att.RawExtractedText);
                        docSb.AppendLine();

                        string snippet = att.RawExtractedText!.Length > 200
                            ? att.RawExtractedText.Substring(0, 200) + "..."
                            : att.RawExtractedText;

                        combinedCitations.Add(new CitationReference
                        {
                            Index = nextCitationIndex++,
                            ChunkId = att.Id,
                            DocumentId = att.Id,
                            KnowledgebaseId = Guid.Empty,
                            KnowledgebaseName = "Attached Document",
                            DocumentName = att.FileName,
                            PageOrSectionNumber = 1,
                            Snippet = snippet,
                            Score = 1.0f
                        });
                    }
                    docSb.AppendLine("--- END ATTACHED DOCUMENTS ---");
                }
                else
                {
                    // Ephemeral in-memory chunking for oversized attachments (TASK-111)
                    docSb.AppendLine("--- BEGIN RELEVANT EXCERPTS FROM ATTACHED DOCUMENTS ---");
                    var chunker = _textChunker ?? new TextChunker();

                    foreach (var att in readyAttachments)
                    {
                        var chunks = chunker.ChunkText(att.RawExtractedText!);
                        var scoredChunks = RankChunksForPrompt(userPrompt, chunks, topK: 3);

                        foreach (var chunk in scoredChunks)
                        {
                            docSb.AppendLine($"[Source: {att.FileName} (Section {chunk.SequenceIndex + 1})]");
                            docSb.AppendLine(chunk.Content);
                            docSb.AppendLine();

                            string snippet = chunk.Content.Length > 200
                                ? chunk.Content.Substring(0, 200) + "..."
                                : chunk.Content;

                            combinedCitations.Add(new CitationReference
                            {
                                Index = nextCitationIndex++,
                                ChunkId = chunk.Id,
                                DocumentId = att.Id,
                                KnowledgebaseId = Guid.Empty,
                                KnowledgebaseName = "Attached Document",
                                DocumentName = att.FileName,
                                PageOrSectionNumber = chunk.SequenceIndex + 1,
                                Snippet = snippet,
                                Score = 0.9f
                            });
                        }
                    }
                    docSb.AppendLine("--- END RELEVANT EXCERPTS FROM ATTACHED DOCUMENTS ---");
                }

                var attachmentPromptText = docSb.ToString();
                if (!string.IsNullOrWhiteSpace(groundedSystemPrompt))
                {
                    groundedSystemPrompt = $"{groundedSystemPrompt}\n\n{attachmentPromptText}\nWhen answering, cite information from the attached documents and reference their names or indices.";
                }
                else
                {
                    groundedSystemPrompt = $"You are a helpful assistant. Use the following attached document context to answer the user's questions:\n\n{attachmentPromptText}\nWhen quoting or citing information, reference the document name or citation index.";
                }
            }

            if (combinedCitations.Count > 0)
            {
                assistantMsgVm.LoadCitations(combinedCitations);
            }

            // 4. Build History for Microsoft.Extensions.AI
            var history = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(groundedSystemPrompt))
            {
                history.Add(new ChatMessage(ChatRole.System, groundedSystemPrompt));
            }

            foreach (var m in Messages.Where(x => x != assistantMsgVm))
            {
                var role = m.IsUser ? ChatRole.User : ChatRole.Assistant;
                history.Add(new ChatMessage(role, m.Content));
            }

            // 5. Stream Response
            IsGenerating = true;
            StatusText = $"Generating response using {SelectedModel.DisplayName}...";

            try
            {
                await foreach (var update in _chatExecutionService.StreamResponseAsync(
                    SelectedModel.Provider,
                    SelectedModel.ModelId,
                    history,
                    _cts.Token))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        assistantMsgVm.Content += update.Text;
                        MessageAdded?.Invoke();
                    }
                }

                StatusText = "Ready";
            }
            catch (OperationCanceledException)
            {
                StatusText = "Generation stopped.";
            }
            catch (Exception ex)
            {
                assistantMsgVm.Content += $"\n\n*[Error: {ex.Message}]*";
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
                _cts?.Dispose();
                _cts = null;

                // Save final assistant message to DB with CitationJson
                if (!string.IsNullOrWhiteSpace(assistantMsgVm.Content))
                {
                    string? citationJson = combinedCitations.Count > 0
                        ? JsonSerializer.Serialize(combinedCitations)
                        : null;

                    var assistantEntity = await _sessionService.AddMessageAsync(
                        CurrentSession.Id,
                        "Assistant",
                        assistantMsgVm.Content,
                        citationJson);
                    CurrentSession.Messages.Add(assistantEntity);
                }
            }
        }

        [RelayCommand]
        public void CancelGeneration()
        {
            if (IsGenerating && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public void UpdateKnowledgebaseVisibilityFlags()
        {
            OnPropertyChanged(nameof(HasAttachedKnowledgebases));
            OnPropertyChanged(nameof(HasAvailableKnowledgebases));
            OnPropertyChanged(nameof(AttachedKnowledgebasesCountText));
        }

        public async Task RefreshKnowledgebasesAsync()
        {
            if (CurrentSession == null)
            {
                AttachedKnowledgebases.Clear();
                AvailableKnowledgebasesToAttach.Clear();
                AttachedDocuments.Clear();
                UpdateKnowledgebaseVisibilityFlags();
                return;
            }

            var attached = await _sessionService.GetAttachedKnowledgebasesAsync(CurrentSession.Id);
            AttachedKnowledgebases.Clear();
            var attachedIds = new HashSet<Guid>();
            foreach (var kb in attached)
            {
                AttachedKnowledgebases.Add(kb);
                attachedIds.Add(kb.Id);
            }

            var allKbs = await _knowledgebaseService.GetKnowledgebasesAsync();
            AvailableKnowledgebasesToAttach.Clear();
            foreach (var kb in allKbs)
            {
                if (!attachedIds.Contains(kb.Id))
                {
                    AvailableKnowledgebasesToAttach.Add(kb);
                }
            }

            UpdateKnowledgebaseVisibilityFlags();
        }

        [RelayCommand]
        public async Task AttachKnowledgebaseAsync(KnowledgebaseEntity? kb)
        {
            if (kb == null || CurrentSession == null) return;

            await _sessionService.AttachKnowledgebaseAsync(CurrentSession.Id, kb.Id);

            var toRemove = AvailableKnowledgebasesToAttach.FirstOrDefault(x => x.Id == kb.Id);
            if (toRemove != null)
            {
                AvailableKnowledgebasesToAttach.Remove(toRemove);
            }

            if (!AttachedKnowledgebases.Any(x => x.Id == kb.Id))
            {
                AttachedKnowledgebases.Add(kb);
            }

            UpdateKnowledgebaseVisibilityFlags();
        }

        [RelayCommand]
        public void UpdateAttachmentVisibilityFlags()
        {
            OnPropertyChanged(nameof(HasAttachedDocuments));
            OnPropertyChanged(nameof(AttachedDocumentsCountText));
        }

        public async Task RefreshAttachmentsAsync()
        {
            if (CurrentSession == null || _attachmentService == null)
            {
                AttachedDocuments.Clear();
                UpdateAttachmentVisibilityFlags();
                return;
            }

            var attachments = await _attachmentService.GetSessionAttachmentsAsync(CurrentSession.Id);
            AttachedDocuments.Clear();
            foreach (var a in attachments)
            {
                AttachedDocuments.Add(new ChatAttachmentItemViewModel(a));
            }
            UpdateAttachmentVisibilityFlags();
        }

        [RelayCommand]
        public async Task AttachFilesAsync(IEnumerable<string> filePaths)
        {
            if (CurrentSession == null || filePaths == null || _attachmentService == null) return;

            IsAttaching = true;
            StatusText = "Attaching and parsing documents...";

            try
            {
                foreach (var path in filePaths)
                {
                    if (string.IsNullOrWhiteSpace(path)) continue;

                    var entity = await _attachmentService.AttachDocumentAsync(CurrentSession.Id, path);
                    var existing = AttachedDocuments.FirstOrDefault(a => a.Id == entity.Id);
                    if (existing != null)
                    {
                        existing.UpdateFromEntity(entity);
                    }
                    else
                    {
                        AttachedDocuments.Add(new ChatAttachmentItemViewModel(entity));
                    }
                }
                StatusText = "Ready";
            }
            catch (Exception ex)
            {
                StatusText = $"Attachment error: {ex.Message}";
            }
            finally
            {
                IsAttaching = false;
                UpdateAttachmentVisibilityFlags();
            }
        }

        [RelayCommand]
        public async Task DetachDocumentAsync(ChatAttachmentItemViewModel? item)
        {
            if (item == null || _attachmentService == null) return;

            await _attachmentService.DetachDocumentAsync(item.Id);
            AttachedDocuments.Remove(item);
            UpdateAttachmentVisibilityFlags();
        }

        public static IReadOnlyList<TextChunk> RankChunksForPrompt(string prompt, IReadOnlyList<TextChunk> chunks, int topK = 3)
        {
            if (chunks == null || chunks.Count == 0) return Array.Empty<TextChunk>();
            if (string.IsNullOrWhiteSpace(prompt)) return chunks.Take(topK).ToList();

            var promptTerms = prompt.Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2)
                .Select(t => t.ToLowerInvariant())
                .Distinct()
                .ToHashSet();

            if (promptTerms.Count == 0) return chunks.Take(topK).ToList();

            return chunks
                .Select(chunk =>
                {
                    int score = 0;
                    string chunkLower = chunk.Content.ToLowerInvariant();
                    foreach (var term in promptTerms)
                    {
                        if (chunkLower.Contains(term))
                        {
                            score += 1;
                        }
                    }
                    return (Chunk: chunk, Score: score);
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => x.Chunk)
                .ToList();
        }

        [RelayCommand]
        public async Task DetachKnowledgebaseAsync(KnowledgebaseEntity? kb)
        {
            if (kb == null || CurrentSession == null) return;

            await _sessionService.DetachKnowledgebaseAsync(CurrentSession.Id, kb.Id);

            var toRemove = AttachedKnowledgebases.FirstOrDefault(x => x.Id == kb.Id);
            if (toRemove != null)
            {
                AttachedKnowledgebases.Remove(toRemove);
            }

            if (!AvailableKnowledgebasesToAttach.Any(x => x.Id == kb.Id))
            {
                AvailableKnowledgebasesToAttach.Add(kb);
            }

            UpdateKnowledgebaseVisibilityFlags();
        }
    }
}


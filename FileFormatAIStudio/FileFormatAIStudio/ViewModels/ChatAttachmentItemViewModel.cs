using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileFormatAIStudio.Data.Entities;

namespace FileFormatAIStudio.ViewModels
{
    /// <summary>
    /// ViewModel representing an individual document attached to a chat session.
    /// </summary>
    public partial class ChatAttachmentItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private Guid _sessionId;

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _fileType = string.Empty;

        [ObservableProperty]
        private string _documentCategory = string.Empty;

        [ObservableProperty]
        private long _fileSize;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsParsing))]
        [NotifyPropertyChangedFor(nameof(IsReady))]
        [NotifyPropertyChangedFor(nameof(IsFailed))]
        [NotifyPropertyChangedFor(nameof(TokenBadgeText))]
        private string _status = "Pending";

        [ObservableProperty]
        private string? _parserEngineUsed;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _rawExtractedText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TokenBadgeText))]
        private int _tokenCount;

        [ObservableProperty]
        private int _chunkCount;

        [ObservableProperty]
        private DateTime _attachedAt;

        public bool IsParsing => Status == "Parsing" || Status == "Pending";
        public bool IsReady => Status == "Ready";
        public bool IsFailed => Status == "Failed";

        public string TokenBadgeText
        {
            get
            {
                if (IsParsing) return "Parsing...";
                if (IsFailed) return "Failed";
                if (TokenCount >= 1000)
                {
                    return $"{TokenCount / 1000.0:0.#}k tokens";
                }
                return $"{TokenCount} tokens";
            }
        }

        public string CategoryGlyph => DocumentCategory.ToLowerInvariant() switch
        {
            "word" => "\uE8A5",       // Document
            "excel" => "\uF0E3",      // Spreadsheet / Table
            "powerpoint" => "\uE8B8", // Presentation
            "pdf" => "\uEA90",        // PDF / Document
            "image" => "\uEB9F",      // Image / Picture
            _ => "\uE8A5"             // Default Document
        };

        public string CategoryColorHex => DocumentCategory.ToLowerInvariant() switch
        {
            "word" => "#2B579A",
            "excel" => "#217346",
            "powerpoint" => "#D24726",
            "pdf" => "#E81123",
            "image" => "#9B4F96",
            _ => "#0078D7"
        };

        public ChatSessionAttachmentEntity Entity { get; private set; }

        public ChatAttachmentItemViewModel(ChatSessionAttachmentEntity entity)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            UpdateFromEntity(entity);
        }

        public void UpdateFromEntity(ChatSessionAttachmentEntity entity)
        {
            Entity = entity;
            Id = entity.Id;
            SessionId = entity.SessionId;
            FileName = entity.FileName;
            FilePath = entity.FilePath;
            FileType = entity.FileType;
            DocumentCategory = entity.DocumentCategory;
            FileSize = entity.FileSize;
            Status = entity.Status;
            ParserEngineUsed = entity.ParserEngineUsed;
            ErrorMessage = entity.ErrorMessage;
            RawExtractedText = entity.RawExtractedText;
            TokenCount = entity.TokenCount;
            ChunkCount = entity.ChunkCount;
            AttachedAt = entity.AttachedAt;

            OnPropertyChanged(nameof(CategoryGlyph));
            OnPropertyChanged(nameof(CategoryColorHex));
            OnPropertyChanged(nameof(TokenBadgeText));
        }
    }
}


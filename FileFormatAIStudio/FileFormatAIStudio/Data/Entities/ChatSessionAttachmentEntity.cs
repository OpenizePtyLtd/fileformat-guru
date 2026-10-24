using System;

namespace FileFormatAIStudio.Data.Entities
{
    /// <summary>
    /// Represents a document or file attached directly to a chat session for grounded/ad-hoc RAG and Q&A.
    /// </summary>
    public class ChatSessionAttachmentEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string DocumentCategory { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending", "Parsing", "Ready", "Failed"
        public string? ParserEngineUsed { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawExtractedText { get; set; }
        public int TokenCount { get; set; }
        public int ChunkCount { get; set; }
        public DateTime AttachedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ChatSessionEntity Session { get; set; } = null!;
    }
}


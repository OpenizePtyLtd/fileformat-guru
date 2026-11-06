using System;

namespace FileFormatAIStudio.Data.Entities
{
    /// <summary>
    /// Persisted entity representing a file format from the FileFormat.com taxonomy,
    /// tracking in-app support status, engine compatibility, and user preferences.
    /// </summary>
    public class FileFormatEntity
    {
        public int Id { get; set; }
        public string Extension { get; set; } = string.Empty; // e.g. ".docx", normalized lower with dot
        public string Name { get; set; } = string.Empty;      // e.g. "DOCX"
        public string Category { get; set; } = string.Empty;  // e.g. "word-processing"
        public string CategoryDisplayName { get; set; } = string.Empty; // e.g. "Word Processing"
        public string DocsUrl { get; set; } = string.Empty;   // e.g. "https://docs.fileformat.com/word-processing/docx/"
        public bool IsSupported { get; set; }
        public string Status { get; set; } = "Planned";      // "Supported" or "Planned"
        public string AsposeApi { get; set; } = string.Empty;
        public string DefaultOssEngine { get; set; } = string.Empty;
        public string? PreferredEngineId { get; set; }        // Optional user engine preference override
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}


using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FileFormatAIStudio.Models
{
    /// <summary>
    /// Represents an indexed file format from FileFormat.com taxonomy with compatibility and engine mappings.
    /// </summary>
    public partial class FileFormatItem : ObservableObject
    {
        [ObservableProperty]
        private string _extension = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _categoryDisplayName = string.Empty;

        [ObservableProperty]
        private string _docsUrl = string.Empty;

        [ObservableProperty]
        private string _status = "Planned"; // "Supported" or "Planned"

        [ObservableProperty]
        private bool _isSupported;

        [ObservableProperty]
        private string _asposeApi = string.Empty;

        [ObservableProperty]
        private string _defaultOssEngine = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        public string StatusBadgeText => IsSupported ? "Supported" : "Roadmap";
        public string StatusBadgeGlyph => IsSupported ? "\uE73E" : "\uE823"; // Checkmark vs Clock
    }

    /// <summary>
    /// Category summary containing format count and default engines.
    /// </summary>
    public class FileFormatCategorySummary
    {
        public string CategoryKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int SupportedCount { get; set; }
        public string AsposeApi { get; set; } = string.Empty;
        public string DefaultOssEngine { get; set; } = string.Empty;
    }
}


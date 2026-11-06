using System.Collections.Generic;
using FileFormatAIStudio.Models;

namespace FileFormatAIStudio.Services.Parsing
{
    /// <summary>
    /// Service for indexing and querying the 1,464+ file formats cataloged from FileFormat.com,
    /// tracking their in-app support status and library/engine bindings.
    /// </summary>
    public interface IFileFormatRegistryService
    {
        int TotalFormatsCount { get; }
        int SupportedFormatsCount { get; }
        int PlannedFormatsCount { get; }

        IReadOnlyList<FileFormatItem> GetAllFormats();
        IReadOnlyList<FileFormatCategorySummary> GetCategories();
        FileFormatItem? FindByExtension(string extension);
        bool IsSupported(string extension);
        IEnumerable<FileFormatItem> Search(string? query, string? categoryKey, bool? supportedOnly);
    }
}


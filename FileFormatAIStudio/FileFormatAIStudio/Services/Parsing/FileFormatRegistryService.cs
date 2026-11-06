using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FileFormatAIStudio.Models;

namespace FileFormatAIStudio.Services.Parsing
{
    /// <summary>
    /// Loads and indexes FileFormat.com format catalog and resolves support dynamically
    /// against active IDocumentParserFactory registrations.
    /// </summary>
    public class FileFormatRegistryService : IFileFormatRegistryService
    {
        private readonly IDocumentParserFactory? _parserFactory;
        private readonly List<FileFormatItem> _formats = new();
        private readonly List<FileFormatCategorySummary> _categories = new();
        private readonly Dictionary<string, FileFormatItem> _extensionMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public int TotalFormatsCount => _formats.Count;
        public int SupportedFormatsCount => _formats.Count(f => f.IsSupported);
        public int PlannedFormatsCount => _formats.Count(f => !f.IsSupported);

        public FileFormatRegistryService(IDocumentParserFactory? parserFactory = null)
        {
            _parserFactory = parserFactory;
            InitializeCatalog();
        }

        public IReadOnlyList<FileFormatItem> GetAllFormats()
        {
            lock (_lock)
            {
                return _formats.ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<FileFormatCategorySummary> GetCategories()
        {
            lock (_lock)
            {
                return _categories.ToList().AsReadOnly();
            }
        }

        public FileFormatItem? FindByExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return null;

            string normalized = extension.StartsWith('.') ? extension : "." + extension;
            lock (_lock)
            {
                return _extensionMap.TryGetValue(normalized, out var item) ? item : null;
            }
        }

        public bool IsSupported(string extension)
        {
            var item = FindByExtension(extension);
            return item?.IsSupported ?? false;
        }

        public IEnumerable<FileFormatItem> Search(string? query, string? categoryKey, bool? supportedOnly)
        {
            lock (_lock)
            {
                IEnumerable<FileFormatItem> results = _formats;

                if (!string.IsNullOrWhiteSpace(categoryKey) && !categoryKey.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    results = results.Where(f => string.Equals(f.Category, categoryKey, StringComparison.OrdinalIgnoreCase));
                }

                if (supportedOnly.HasValue)
                {
                    results = results.Where(f => f.IsSupported == supportedOnly.Value);
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    string q = query.Trim().ToLowerInvariant();
                    string qWithDot = q.StartsWith('.') ? q : "." + q;
                    string qWithoutDot = q.StartsWith('.') ? q.Substring(1) : q;

                    results = results.Where(f =>
                        f.Extension.Contains(qWithDot, StringComparison.OrdinalIgnoreCase) ||
                        f.Name.Contains(qWithoutDot, StringComparison.OrdinalIgnoreCase) ||
                        f.CategoryDisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        f.AsposeApi.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        f.DefaultOssEngine.Contains(q, StringComparison.OrdinalIgnoreCase));
                }

                return results.ToList();
            }
        }

        private void InitializeCatalog()
        {
            lock (_lock)
            {
                _formats.Clear();
                _categories.Clear();
                _extensionMap.Clear();

                HashSet<string> supportedByParsers = new(StringComparer.OrdinalIgnoreCase);
                if (_parserFactory != null)
                {
                    foreach (var parser in _parserFactory.GetAllParsers())
                    {
                        foreach (var ext in parser.SupportedExtensions)
                        {
                            supportedByParsers.Add(ext.StartsWith('.') ? ext : "." + ext);
                        }
                    }
                }

                string catalogPath = ResolveCatalogFilePath();
                if (File.Exists(catalogPath))
                {
                    try
                    {
                        string json = File.ReadAllText(catalogPath);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("categories", out var categoriesElem) &&
                            categoriesElem.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var catProp in categoriesElem.EnumerateObject())
                            {
                                string catKey = catProp.Name;
                                var catObj = catProp.Value;

                                string asposeApi = catObj.TryGetProperty("aspose_api", out var aspElem)
                                    ? aspElem.GetString() ?? "" : "";
                                string ossEngine = catObj.TryGetProperty("default_oss_engine", out var ossElem)
                                    ? ossElem.GetString() ?? "" : "";
                                string catDisplayName = FormatCategoryDisplayName(catKey);

                                int catTotal = 0;
                                int catSupported = 0;

                                if (catObj.TryGetProperty("formats", out var formatsElem) &&
                                    formatsElem.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var fmtElem in formatsElem.EnumerateArray())
                                    {
                                        string ext = fmtElem.TryGetProperty("extension", out var extElem)
                                            ? extElem.GetString() ?? "" : "";
                                        string name = fmtElem.TryGetProperty("name", out var nameElem)
                                            ? nameElem.GetString() ?? "" : "";
                                        string docsUrl = fmtElem.TryGetProperty("docs_url", out var urlElem)
                                            ? urlElem.GetString() ?? "" : "";
                                        string catalogStatus = fmtElem.TryGetProperty("status", out var statElem)
                                            ? statElem.GetString() ?? "Planned" : "Planned";

                                        // Cross check with live parser engines
                                        bool isSupported = supportedByParsers.Contains(ext) ||
                                                           string.Equals(catalogStatus, "Implemented", StringComparison.OrdinalIgnoreCase);

                                        catTotal++;
                                        if (isSupported)
                                            catSupported++;

                                        var item = new FileFormatItem
                                        {
                                            Extension = ext,
                                            Name = name,
                                            Category = catKey,
                                            CategoryDisplayName = catDisplayName,
                                            DocsUrl = docsUrl,
                                            Status = isSupported ? "Supported" : "Planned",
                                            IsSupported = isSupported,
                                            AsposeApi = asposeApi,
                                            DefaultOssEngine = ossEngine,
                                            Description = $"{name} ({ext}) specification in {catDisplayName} category."
                                        };

                                        _formats.Add(item);
                                        _extensionMap[ext] = item;
                                    }
                                }

                                _categories.Add(new FileFormatCategorySummary
                                {
                                    CategoryKey = catKey,
                                    DisplayName = catDisplayName,
                                    TotalCount = catTotal,
                                    SupportedCount = catSupported,
                                    AsposeApi = asposeApi,
                                    DefaultOssEngine = ossEngine
                                });
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Fallback safely if JSON cannot be parsed
                    }
                }

                // If catalog file wasn't found or had errors, seed known baseline formats
                if (_formats.Count == 0 && supportedByParsers.Count > 0)
                {
                    foreach (var ext in supportedByParsers)
                    {
                        var item = new FileFormatItem
                        {
                            Extension = ext,
                            Name = ext.TrimStart('.').ToUpperInvariant(),
                            Category = "document",
                            CategoryDisplayName = "Document",
                            DocsUrl = $"https://docs.fileformat.com/",
                            Status = "Supported",
                            IsSupported = true,
                            AsposeApi = "Aspose Total Suite",
                            DefaultOssEngine = "Integrated Parser",
                            Description = $"Supported {ext} document format."
                        };
                        _formats.Add(item);
                        _extensionMap[ext] = item;
                    }
                }
            }
        }

        private static string ResolveCatalogFilePath()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", "fileformat_com_catalog.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "fileformat_com_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "fileformat_com_catalog.json"),
                Path.Combine(AppContext.BaseDirectory, "fileformat_com_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "docs", "fileformat_com_catalog.json")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return candidates[0];
        }

        private static string FormatCategoryDisplayName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key;

            // e.g. "word-processing" -> "Word Processing"
            var parts = key.Split('-', '_');
            return string.Join(" ", parts.Select(p =>
            {
                if (string.Equals(p, "3d", StringComparison.OrdinalIgnoreCase)) return "3D";
                if (string.Equals(p, "cad", StringComparison.OrdinalIgnoreCase)) return "CAD";
                if (string.Equals(p, "gis", StringComparison.OrdinalIgnoreCase)) return "GIS";
                if (string.Equals(p, "pdf", StringComparison.OrdinalIgnoreCase)) return "PDF";
                if (p.Length <= 1) return p.ToUpperInvariant();
                return char.ToUpperInvariant(p[0]) + p.Substring(1);
            }));
        }
    }
}


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileFormatAIStudio.Models;
using FileFormatAIStudio.Services.Parsing;
using Windows.System;

namespace FileFormatAIStudio.ViewModels
{
    public partial class FileFormatsViewModel : ObservableObject
    {
        private readonly IFileFormatRegistryService _registryService;
        private List<FileFormatItem> _allFormats = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoResults))]
        private ObservableCollection<FileFormatItem> _filteredFormats = new();

        public bool HasNoResults => FilteredFormats.Count == 0;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private string _selectedCategory = "All Categories";

        [ObservableProperty]
        private int _statusFilterIndex = 0; // 0 = All (1,464), 1 = Supported (36), 2 = Roadmap (1,428)

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilterSummaryText))]
        private int _totalCount;

        [ObservableProperty]
        private int _supportedCount;

        [ObservableProperty]
        private int _plannedCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilterSummaryText))]
        private int _filteredCount;

        public string FilterSummaryText => $"Showing {FilteredCount:N0} of {TotalCount:N0} formats";

        public FileFormatsViewModel(IFileFormatRegistryService registryService)
        {
            _registryService = registryService;
            LoadData();
        }

        public void LoadData()
        {
            _allFormats = _registryService.GetAllFormats().ToList();
            TotalCount = _registryService.TotalFormatsCount;
            SupportedCount = _registryService.SupportedFormatsCount;
            PlannedCount = _registryService.PlannedFormatsCount;

            Categories.Clear();
            Categories.Add("All Categories");
            foreach (var cat in _registryService.GetCategories().OrderBy(c => c.DisplayName))
            {
                Categories.Add(cat.DisplayName);
            }

            ApplyFilter();
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilter();
        partial void OnSelectedCategoryChanged(string value) => ApplyFilter();
        partial void OnStatusFilterIndexChanged(int value) => ApplyFilter();

        [RelayCommand]
        public void ApplyFilter()
        {
            bool? supportedOnly = StatusFilterIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            };

            string? catKey = null;
            if (!string.IsNullOrWhiteSpace(SelectedCategory) && !SelectedCategory.Equals("All Categories", StringComparison.OrdinalIgnoreCase))
            {
                var match = _registryService.GetCategories().FirstOrDefault(c =>
                    string.Equals(c.DisplayName, SelectedCategory, StringComparison.OrdinalIgnoreCase));
                catKey = match?.CategoryKey;
            }

            var results = _registryService.Search(SearchQuery, catKey, supportedOnly).ToList();

            // Sort: Supported first, then alphabetical by Extension
            results = results
                .OrderByDescending(f => f.IsSupported)
                .ThenBy(f => f.Extension)
                .ToList();

            FilteredFormats = new ObservableCollection<FileFormatItem>(results);
            FilteredCount = FilteredFormats.Count;
        }

        [RelayCommand]
        public async Task OpenDocsUrlAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    await Launcher.LaunchUriAsync(uri);
                }
            }
            catch
            {
                // Fallback silently if default browser launch fails
            }
        }
    }
}


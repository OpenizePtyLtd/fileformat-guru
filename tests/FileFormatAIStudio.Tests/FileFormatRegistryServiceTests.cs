using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileFormatAIStudio.Services.Parsing;
using FileFormatAIStudio.ViewModels;
using FluentAssertions;
using Xunit;

namespace FileFormatAIStudio.Tests
{
    public class FileFormatRegistryServiceTests
    {
        private readonly FileFormatRegistryService _registryService = new();

        [Fact]
        public void RegistryService_LoadsCatalog_WithComprehensiveCoverage()
        {
            _registryService.TotalFormatsCount.Should().BeGreaterThan(1400);
            _registryService.GetCategories().Count.Should().BeGreaterThanOrEqualTo(30);
            _registryService.SupportedFormatsCount.Should().BeGreaterThan(20);
            _registryService.PlannedFormatsCount.Should().BeGreaterThan(1000);
        }

        [Theory]
        [InlineData(".docx", true)]
        [InlineData(".pdf", true)]
        [InlineData(".xlsx", true)]
        [InlineData(".pptx", true)]
        [InlineData(".txt", true)]
        [InlineData(".png", true)]
        [InlineData(".3ds", false)]
        [InlineData(".mpp", false)]
        [InlineData(".epub", false)]
        [InlineData(".dwg", false)]
        public void IsSupported_ReturnsAccurateStatus(string extension, bool expectedSupported)
        {
            _registryService.IsSupported(extension).Should().Be(expectedSupported);
        }

        [Fact]
        public void FindByExtension_HandlesLeadingDotAndCaseInsensitivity()
        {
            var itemWithDot = _registryService.FindByExtension(".docx");
            var itemWithoutDot = _registryService.FindByExtension("docx");
            var itemUpper = _registryService.FindByExtension(".DOCX");

            itemWithDot.Should().NotBeNull();
            itemWithoutDot.Should().NotBeNull();
            itemUpper.Should().NotBeNull();

            itemWithDot!.Extension.Should().Be(".docx");
            itemWithDot.IsSupported.Should().BeTrue();
            itemWithDot.AsposeApi.Should().Contain("Aspose.Words");
            itemWithDot.DefaultOssEngine.Should().Contain("OpenXml");
        }

        [Fact]
        public void Search_FiltersByQueryAndCategory()
        {
            // Search by extension
            var pdfResults = _registryService.Search("pdf", null, null).ToList();
            pdfResults.Should().NotBeEmpty();
            pdfResults.Any(f => f.Extension == ".pdf").Should().BeTrue();

            // Search by category
            var spreadsheetFormats = _registryService.Search(null, "spreadsheet", null).ToList();
            spreadsheetFormats.Should().NotBeEmpty();
            spreadsheetFormats.All(f => f.Category == "spreadsheet").Should().BeTrue();
            spreadsheetFormats.Any(f => f.Extension == ".xlsx").Should().BeTrue();

            // Filter supported only
            var supportedSpreadsheets = _registryService.Search(null, "spreadsheet", true).ToList();
            supportedSpreadsheets.Should().NotBeEmpty();
            supportedSpreadsheets.All(f => f.IsSupported).Should().BeTrue();

            // Filter roadmap only
            var plannedSpreadsheets = _registryService.Search(null, "spreadsheet", false).ToList();
            plannedSpreadsheets.Should().NotBeEmpty();
            plannedSpreadsheets.All(f => !f.IsSupported).Should().BeTrue();
        }

        [Fact]
        public void ViewModel_LoadsData_AndAppliesFilters()
        {
            var vm = new FileFormatsViewModel(_registryService);

            vm.TotalCount.Should().BeGreaterThan(1400);
            vm.Categories.Should().Contain("All Categories");
            vm.Categories.Should().Contain("Word Processing");
            vm.Categories.Should().Contain("Spreadsheet");
            vm.Categories.Should().Contain("3D");

            // Default filter shows all formats
            vm.FilteredFormats.Count.Should().Be(vm.TotalCount);
            vm.FilterSummaryText.Should().Contain(vm.TotalCount.ToString("N0"));

            // Filter by Supported Only
            vm.StatusFilterIndex = 1;
            vm.FilteredFormats.Count.Should().Be(vm.SupportedCount);
            vm.FilteredFormats.All(f => f.IsSupported).Should().BeTrue();

            // Filter by Roadmap Only
            vm.StatusFilterIndex = 2;
            vm.FilteredFormats.Count.Should().Be(vm.PlannedCount);
            vm.FilteredFormats.All(f => !f.IsSupported).Should().BeTrue();

            // Reset status filter and search query
            vm.StatusFilterIndex = 0;
            vm.SearchQuery = "epub";
            vm.FilteredFormats.Should().Contain(f => f.Extension == ".epub");
        }
    }
}


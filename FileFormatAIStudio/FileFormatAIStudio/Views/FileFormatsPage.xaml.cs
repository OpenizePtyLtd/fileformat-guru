using System;
using FileFormatAIStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI;

namespace FileFormatAIStudio.Views
{
    public sealed partial class FileFormatsPage : Page
    {
        public FileFormatsViewModel ViewModel { get; }

        public FileFormatsPage()
        {
            InitializeComponent();
            ViewModel = ((App)Application.Current).Services.GetRequiredService<FileFormatsViewModel>();
        }

        private async void OnOpenFormatDocsClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    await Launcher.LaunchUriAsync(uri);
                }
            }
        }

        private async void OnOpenDocsHomepageClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri("https://docs.fileformat.com/"));
            }
            catch
            {
                // Fallback silently if browser cannot be launched
            }
        }

        public static Brush StatusBadgeBackground(bool isSupported)
        {
            return isSupported
                ? new SolidColorBrush(Color.FromArgb(32, 16, 124, 65))  // subtle green tint
                : new SolidColorBrush(Color.FromArgb(16, 128, 128, 128)); // subtle gray tint
        }

        public static Brush StatusBadgeBorder(bool isSupported)
        {
            return isSupported
                ? new SolidColorBrush(Color.FromArgb(90, 16, 124, 65))
                : new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));
        }

        public static Brush StatusBadgeForeground(bool isSupported)
        {
            return isSupported
                ? new SolidColorBrush(Color.FromArgb(255, 16, 124, 65))
                : (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        }
    }
}


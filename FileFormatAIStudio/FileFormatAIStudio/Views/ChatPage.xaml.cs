using System;
using FileFormatAIStudio.Data.Entities;
using FileFormatAIStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using System.Linq;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using FileFormatAIStudio.Views.Dialogs;

namespace FileFormatAIStudio.Views
{
    public sealed partial class ChatPage : Page
    {
        public static Visibility ToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
        public static Visibility ToInvertedVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

        public ChatViewModel ViewModel { get; }

        public ChatPage()
        {
            InitializeComponent();
            ViewModel = ((App)Application.Current).Services.GetRequiredService<ChatViewModel>();
            ViewModel.MessageAdded += OnMessageAdded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Guid sessionId)
            {
                await ViewModel.InitializeAsync(sessionId);
            }
            else
            {
                await ViewModel.InitializeAsync();
            }
        }

        private void OnMessageAdded()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                MessagesScrollViewer?.ChangeView(null, MessagesScrollViewer.ScrollableHeight, null, false);
            });
        }

        private async void OnPromptPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
                bool isShiftDown = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

                if (!isShiftDown)
                {
                    e.Handled = true;
                    if (ViewModel.SendMessageCommand.CanExecute(null))
                    {
                        await ViewModel.SendMessageCommand.ExecuteAsync(null);
                    }
                }
            }
        }

        private async void OnAvailableKbItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is KnowledgebaseEntity kb)
            {
                AttachKbFlyout?.Hide();
                await ViewModel.AttachKnowledgebaseCommand.ExecuteAsync(kb);
            }
        }

        private void OnPageDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Attach document to chat";
                e.DragUIOverride.IsCaptionVisible = true;
                DragDropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void OnPageDragLeave(object sender, DragEventArgs e)
        {
            DragDropOverlay.Visibility = Visibility.Collapsed;
        }

        private async void OnPageDrop(object sender, DragEventArgs e)
        {
            DragDropOverlay.Visibility = Visibility.Collapsed;
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var paths = items.OfType<Windows.Storage.IStorageFile>()
                    .Select(f => f.Path)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                if (paths.Count > 0)
                {
                    await ViewModel.AttachFilesCommand.ExecuteAsync(paths);
                }
            }
        }

        private async void OnAttachButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                var hwnd = App.MainWindowInstance != null
                    ? WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance)
                    : IntPtr.Zero;

                if (hwnd != IntPtr.Zero)
                {
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }

                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

                picker.FileTypeFilter.Add(".docx");
                picker.FileTypeFilter.Add(".doc");
                picker.FileTypeFilter.Add(".xlsx");
                picker.FileTypeFilter.Add(".xls");
                picker.FileTypeFilter.Add(".pptx");
                picker.FileTypeFilter.Add(".ppt");
                picker.FileTypeFilter.Add(".pdf");
                picker.FileTypeFilter.Add(".txt");
                picker.FileTypeFilter.Add(".csv");
                picker.FileTypeFilter.Add(".md");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");

                var files = await picker.PickMultipleFilesAsync();
                if (files != null && files.Count > 0)
                {
                    var paths = files.Select(f => f.Path).ToList();
                    await ViewModel.AttachFilesCommand.ExecuteAsync(paths);
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusText = $"Attachment error: {ex.Message}";
            }
        }

        private async void OnInspectAttachmentClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChatAttachmentItemViewModel att)
            {
                var services = ((App)Application.Current).Services;
                var vm = services.GetRequiredService<ViewExtractedTextViewModel>();
                var dialog = new ViewExtractedTextDialog(vm)
                {
                    XamlRoot = this.Content.XamlRoot
                };
                dialog.InitializeForAttachment(att.Entity);
                await dialog.ShowAsync();
            }
        }

        private async void OnDetachAttachmentClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChatAttachmentItemViewModel att)
            {
                await ViewModel.DetachDocumentCommand.ExecuteAsync(att);
            }
        }

        private async void OnDetachKbClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is KnowledgebaseEntity kb)
            {
                await ViewModel.DetachKnowledgebaseCommand.ExecuteAsync(kb);
            }
        }
    }
}


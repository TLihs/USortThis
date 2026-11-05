using System.Collections.Generic;
using System.Linq;
using System.Windows;
using USortThis.Core.Models;

namespace USortThis.UI.Views
{
    public partial class TransferPreviewWindow : Window
    {
        public TransferPreviewWindow(ProfileItemModel profile, IReadOnlyList<TransferPreviewEntry> entries, IReadOnlyList<FileEnumerationError> errors)
        {
            InitializeComponent();
            PreviewDataGrid.ItemsSource = entries;
            SummaryTextBlock.Text = $"{profile.Name}: {entries.Count} planned file route(s), {errors.Count} enumeration warning(s).";
            if (errors.Count > 0)
            {
                WarningsListBox.ItemsSource = errors.Select(error => error.ToString()).ToList();
                WarningsExpander.Visibility = Visibility.Visible;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using USortThis.Core.Models;

namespace USortThis.UI.Views
{
    /// <summary>
    /// Interaction logic for TransferPreviewWindow.xaml
    /// </summary>
    public partial class TransferPreviewWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransferPreviewWindow"/> class.
        /// </summary>
        /// <param name="profile">The profile associated with the transfer.</param>
        /// <param name="entries">The collection of transfer preview entries.</param>
        /// <param name="errors">The collection of file enumeration errors.</param>
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

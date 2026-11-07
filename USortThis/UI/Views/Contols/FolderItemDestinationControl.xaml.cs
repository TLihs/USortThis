using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using USortThis.Core.Data;
using USortThis.Core.ViewModels;

namespace USortThis.UI.Views.Controls
{
    /// <summary>
    /// Interaction logic for FolderItemDestinationControl.xaml
    /// </summary>
    public partial class FolderItemDestinationControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FolderItemDestinationControl"/> class.
        /// </summary>
        public FolderItemDestinationControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the DragOver event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DragEventArgs"/> instance containing the event data.</param>
        private void UserControl_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else
                e.Effects = System.Windows.DragDropEffects.None;

            e.Handled = true;
        }

        /// <summary>
        /// Handles the Drop event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DragEventArgs"/> instance containing the event data.</param>
        private void UserControl_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;

            // If this control's DataContext is a destination viewmodel, create copy items targeted to that destination
            //if (this.DataContext is FolderItemViewModel destVm)
            //{
            //    foreach (var file in files)
            //    {
            //        var item = destVm.CreateCopyItemForFile(file);
            //        Application.Current?.Dispatcher?.Invoke(() => CopyQueueFactory.FileItems.Add(item));
            //    }
            //}
            //else
            //{
            //    foreach (var file in files)
            //    {
            //        // Create a view model for the dropped file and add to the global queue
            //        var vm = new FileItemViewModel(file);
            //        Application.Current?.Dispatcher?.Invoke(() => CopyQueueManager.FileItems.Add(vm));
            //    }
            //}

            e.Handled = true;
        }
    }
}

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
using USortThis.Core.Interfaces;
using USortThis.Core.ViewModels;

namespace USortThis.UI.Views.Controls
{
    /// <summary>
    /// Interaction logic for ProfileItemControl.xaml
    /// </summary>
    public partial class ProfileItemControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileItemControl"/> class.
        /// </summary>
        public ProfileItemControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Drop event on the RootBorder.
        /// </summary>
        /// <param name="sender">The source of the event, in this case, the RootBorder.</param>
        /// <param name="e">The data of the event, containing the drop information.</param>
        private void RootBorder_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var viewModel = DataContext as ProfileItemViewModel;
            if (viewModel?.Profile == null) return;

            if (!e.Data.GetDataPresent("FileDrop")) return;

            if (e.Data.GetData("FileDrop") is not string[] paths || paths.Length == 0) return;

            var serviceProvider = App.ServiceProvider;
            if (serviceProvider == null) return;

            if (serviceProvider.GetService(typeof(IFolderItemModelFactory)) is not IFolderItemModelFactory folderFactory ||
                serviceProvider.GetService(typeof(IFolderBridgeModelFactory)) is not IFolderBridgeModelFactory bridgeFactory)
                return;

            foreach (var p in paths)
            {
                try
                {
                    if (System.IO.Directory.Exists(p))
                    {
                        // Prompt user for destination folder
                        using FolderBrowserDialog dlg = new();
                        dlg.Description = "Select destination folder for the new bridge";
                        dlg.ShowNewFolderButton = true;
                        DialogResult result = dlg.ShowDialog();
                        if (result != DialogResult.OK)
                            continue;

                        var source = folderFactory.Create(p, string.Empty, string.Empty);
                        var dest = folderFactory.Create(dlg.SelectedPath, string.Empty, string.Empty);
                        var bridge = bridgeFactory.Create(string.Empty, source, dest);
                        viewModel.Profile.AddFolderBridge(bridge);
                    }
                }
                catch
                {
                    // Swallow exceptions from drag/drop handlers to avoid breaking drag experience
                }
            }
        }
    }
}

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
using USortThis.Core.Models;

namespace USortThis.UI.Views.Controls
{
    /// <summary>
    /// Interaction logic for FolderItemBridgeControl.xaml
    /// </summary>
    public partial class FolderItemBridgeControl : System.Windows.Controls.UserControl
    {
        public FolderItemBridgeControl()
        {
            InitializeComponent();
        }

        private void RootBorder_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else
                e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void RootBorder_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else
                e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void RootBorder_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

            var paths = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length == 0)
            {
                e.Handled = true;
                return;
            }

            // Try to operate on the model directly if the DataContext is a model
            var bridge = (DataContext as USortThis.Core.ViewModels.FolderBridgeViewModel)?.Model
                ?? DataContext as USortThis.Core.Models.FolderBridgeModel;
            if (bridge != null)
            {
                bool setDestination = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

                foreach (var p in paths)
                {
                    try
                    {
                        if (System.IO.Directory.Exists(p))
                        {
                            if (setDestination && bridge.DestinationFolder != null)
                            {
                                bridge.DestinationFolder.FolderPath = p;
                            }
                            else
                            {
                                if (!bridge.DroppedPaths.Contains(p, StringComparer.OrdinalIgnoreCase))
                                    bridge.DroppedPaths.Add(p);
                            }
                        }
                        else if (System.IO.File.Exists(p))
                        {
                            if (!bridge.DroppedPaths.Contains(p, StringComparer.OrdinalIgnoreCase))
                                bridge.DroppedPaths.Add(p);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            else
            {
                // If DataContext isn't the model, nothing else to do in this simplified implementation.
            }

            e.Handled = true;
        }
    }
}

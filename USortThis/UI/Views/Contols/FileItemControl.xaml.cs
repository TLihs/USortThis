using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace USortThis.UI.Views.Controls
{
    /// <summary>
    /// Interaction logic for FileItemControl.xaml
    /// </summary>
    public partial class FileItemControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileItemControl"/> class.
        /// </summary>
        public FileItemControl()
        {
            InitializeComponent();
            this.MouseLeftButtonUp += FileItemControl_MouseLeftButtonUp;
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the FileItemControl control.
        /// Currently performs no action.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The mouse button event data.</param>
        private void FileItemControl_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            //if (this.DataContext is FileItemViewModel vm)
            //{
            //    var wnd = Window.GetWindow(this) as MainWindow;
            //    if (wnd != null && wnd.DataContext is MainViewModel mwvm)
            //    {
            //        if (mwvm.SelectItemCommand.CanExecute(vm)) mwvm.SelectItemCommand.Execute(vm);
            //    }
            //}
        }
    }
}

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
        public FileItemControl()
        {
            InitializeComponent();
            this.MouseLeftButtonUp += FileItemControl_MouseLeftButtonUp;
        }

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

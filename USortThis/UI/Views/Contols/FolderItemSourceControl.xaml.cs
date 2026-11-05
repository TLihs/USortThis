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

namespace USortThis.UI.Views.Controls
{
    /// <summary>
    /// Interaction logic for FolderItemControl.xaml
    /// </summary>
    public partial class FolderItemSourceControl : System.Windows.Controls.UserControl
    {
        public FolderItemSourceControl()
        {
            InitializeComponent();
        }

        public object ViewModel => DataContext;
    }
}

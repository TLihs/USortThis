using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IMainWindowModelFactory
    {
        public MainWindowViewModel Create(MainWindowModel model);
    }
}

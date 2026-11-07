using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IMainWindowModelFactory
    {
        /// <summary>Creates the main-window view model from its application model.</summary>
        /// <param name="model">The model used to initialize the view model.</param>
        public MainWindowViewModel Create(MainWindowModel model);
    }
}

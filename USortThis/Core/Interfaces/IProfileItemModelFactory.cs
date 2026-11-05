using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IProfileItemModelFactory
    {
        public ProfileItemModel Create(string profileName);
        public ProfileItemModel LoadFromDatabase(long id, string profileName);
    }
}

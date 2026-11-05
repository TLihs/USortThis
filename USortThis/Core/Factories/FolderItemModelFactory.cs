using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Factories
{
    public class FolderItemModelFactory : BaseIdModelFactory, IFolderItemModelFactory
    {
        public FolderItemModelFactory(IIdFactory idFactory) : base(idFactory)
        {
        }

        public FolderItemModel Create(string folderPath, string customName, string customDescription)
        {
            return new FolderItemModel(_idFactory.CreateId(), customName, customDescription, folderPath);
        }

        public FolderItemModel LoadFromDatabase(long id, string folderPath, string customName, string customDescription)
        {
            return new FolderItemModel(id, customName, customDescription, folderPath);
        }
    }
}

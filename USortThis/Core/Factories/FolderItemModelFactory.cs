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
        /// <summary>Initializes the factory with the identifier generator used for new folders.</summary>
        /// <param name="idFactory">The source of folder identifiers.</param>
        public FolderItemModelFactory(IIdFactory idFactory) : base(idFactory)
        {
        }

        /// <summary>Creates a folder model with a new identifier and the supplied details.</summary>
        /// <param name="folderPath">The folder's path.</param>
        /// <param name="customName">The user-defined folder name.</param>
        /// <param name="customDescription">The user-defined folder description.</param>
        public FolderItemModel Create(string folderPath, string customName, string customDescription)
        {
            return new FolderItemModel(_idFactory.CreateId(), customName, customDescription, folderPath);
        }

        /// <summary>Reconstructs a folder model from persisted values.</summary>
        /// <param name="id">The persisted folder identifier.</param>
        /// <param name="folderPath">The folder's path.</param>
        /// <param name="customName">The user-defined folder name.</param>
        /// <param name="customDescription">The user-defined folder description.</param>
        public FolderItemModel LoadFromDatabase(long id, string folderPath, string customName, string customDescription)
        {
            return new FolderItemModel(id, customName, customDescription, folderPath);
        }
    }
}

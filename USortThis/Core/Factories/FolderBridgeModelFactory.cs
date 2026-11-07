using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Factories
{
    public class FolderBridgeModelFactory : BaseIdModelFactory, IFolderBridgeModelFactory
    {
        private readonly IFileSystemIOService _fileInfoService;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>Initializes the factory with ID, file-system, and dependency-resolution services.</summary>
        /// <param name="idFactory">The source of bridge identifiers.</param>
        /// <param name="fileInfoService">The service used by bridges to inspect folders.</param>
        /// <param name="serviceProvider">The provider used to resolve the database service.</param>
        public FolderBridgeModelFactory(IIdFactory idFactory, IFileSystemIOService fileInfoService, IServiceProvider serviceProvider) : base(idFactory)
        {
            _fileInfoService = fileInfoService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>Creates a bridge connecting the supplied source and destination folders.</summary>
        /// <param name="customName">The user-defined bridge name.</param>
        /// <param name="sourceFolder">The source folder, if configured.</param>
        /// <param name="destinationFolder">The destination folder, if configured.</param>
        public FolderBridgeModel Create(string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder)
        {
            var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();
            return new(_idFactory.CreateId(), customName, sourceFolder, destinationFolder, _fileInfoService, databaseService);
        }

        /// <summary>Reconstructs a bridge with its persisted identifier and folder endpoints.</summary>
        /// <param name="id">The persisted bridge identifier.</param>
        /// <param name="customName">The user-defined bridge name.</param>
        /// <param name="sourceFolder">The source folder, if configured.</param>
        /// <param name="destinationFolder">The destination folder, if configured.</param>
        public FolderBridgeModel LoadFromDatabase(long id, string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder)
        {
            var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();
            return new(id, customName, sourceFolder, destinationFolder, _fileInfoService, databaseService);
        }
    }
}

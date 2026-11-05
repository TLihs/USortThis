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

        public FolderBridgeModelFactory(IIdFactory idFactory, IFileSystemIOService fileInfoService, IServiceProvider serviceProvider) : base(idFactory)
        {
            _fileInfoService = fileInfoService;
            _serviceProvider = serviceProvider;
        }

        public FolderBridgeModel Create(string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder)
        {
            var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();
            return new(_idFactory.CreateId(), customName, sourceFolder, destinationFolder, _fileInfoService, databaseService);
        }

        public FolderBridgeModel LoadFromDatabase(long id, string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder)
        {
            var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();
            return new(id, customName, sourceFolder, destinationFolder, _fileInfoService, databaseService);
        }
    }
}

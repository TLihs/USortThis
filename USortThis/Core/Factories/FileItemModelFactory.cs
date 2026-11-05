using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Factories
{
    public class FileItemModelFactory : BaseIdModelFactory, IFileItemModelFactory
    {
        private readonly IFileSystemIOService _fileInfoService;
        
        public FileItemModelFactory(IIdFactory idFactory, IFileSystemIOService fileInfoService) : base(idFactory)
        {
            _fileInfoService = fileInfoService;
        }

        public FileItemModel Create(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var fileSize = _fileInfoService.GetFileSize(filePath) ?? throw new ArgumentException("File size could not be read");
            var creationTime = _fileInfoService.GetCreationTime(filePath) ?? throw new ArgumentException("Creation time could not be read");
            var lastWriteTime = _fileInfoService.GetLastWriteTime(filePath) ?? throw new ArgumentException("Last write time could not be read");
            return new(_idFactory.CreateId(), filePath, fileSize, creationTime, lastWriteTime);
        }

        public FileItemModel LoadFromDatabase(long id, string filePath, long fileSize, DateTime creationTime, DateTime lastWriteTime, int status, int failedReason)
        {
            var parsedStatus = (FileItemModel.CopyStatus)status;
            var parsedFailedReason = (FileItemModel.FailedReason)failedReason;
            return new(id, filePath, fileSize, creationTime, lastWriteTime, parsedStatus, parsedFailedReason);
        }
    }
}

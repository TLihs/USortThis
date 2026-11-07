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
        
        /// <summary>Initializes the factory with identifier and file-system services.</summary>
        /// <param name="idFactory">The source of file-model identifiers.</param>
        /// <param name="fileInfoService">The service used to read file metadata.</param>
        public FileItemModelFactory(IIdFactory idFactory, IFileSystemIOService fileInfoService) : base(idFactory)
        {
            _fileInfoService = fileInfoService;
        }

        /// <summary>Creates a file model using the path and metadata read from the file system.</summary>
        /// <param name="filePath">The path of the file to represent.</param>
        /// <returns>A model populated with a new identifier and current file metadata.</returns>
        public FileItemModel Create(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var fileSize = _fileInfoService.GetFileSize(filePath) ?? throw new ArgumentException("File size could not be read");
            var creationTime = _fileInfoService.GetCreationTime(filePath) ?? throw new ArgumentException("Creation time could not be read");
            var lastWriteTime = _fileInfoService.GetLastWriteTime(filePath) ?? throw new ArgumentException("Last write time could not be read");
            return new(_idFactory.CreateId(), filePath, fileSize, creationTime, lastWriteTime);
        }

        /// <summary>Reconstructs a file model from persisted values.</summary>
        /// <param name="id">The persisted file identifier.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="creationTime">The file creation time.</param>
        /// <param name="lastWriteTime">The file's last-write time.</param>
        /// <param name="status">The persisted transfer status value.</param>
        /// <param name="failedReason">The persisted failure reason value.</param>
        public FileItemModel LoadFromDatabase(long id, string filePath, long fileSize, DateTime creationTime, DateTime lastWriteTime, int status, int failedReason)
        {
            var parsedStatus = (FileItemModel.CopyStatus)status;
            var parsedFailedReason = (FileItemModel.FailedReason)failedReason;
            return new(id, filePath, fileSize, creationTime, lastWriteTime, parsedStatus, parsedFailedReason);
        }
    }
}

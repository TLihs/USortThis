using System;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents an error that occurs during file enumeration.
    /// </summary>
    public sealed class FileEnumerationError
    {
        /// <summary>
        /// Gets the timestamp (in UTC) when the error occurred.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// Gets the path of the file that caused the error.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the type of the exception that was thrown.
        /// </summary>
        public string ExceptionType { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEnumerationError"/> class.
        /// </summary>
        /// <param name="timestampUtc">The timestamp (in UTC) when the error occurred.</param>
        /// <param name="path">The path of the file that caused the error.</param>
        /// <param name="ex">The exception that was thrown.</param>
        public FileEnumerationError(DateTime timestampUtc, string path, Exception ex)
        {
            TimestampUtc = timestampUtc;
            Path = path ?? string.Empty;
            ExceptionType = ex?.GetType().FullName ?? "Unknown";
            Message = ex?.Message ?? string.Empty;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"[{TimestampUtc:yyyy-MM-dd HH:mm:ss}] {Path} - {ExceptionType}: {Message}";
        }
    }
}

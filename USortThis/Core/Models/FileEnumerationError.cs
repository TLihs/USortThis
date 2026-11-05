using System;

namespace USortThis.Core.Models
{
    public sealed class FileEnumerationError
    {
        public DateTime TimestampUtc { get; }
        public string Path { get; }
        public string ExceptionType { get; }
        public string Message { get; }

        public FileEnumerationError(DateTime timestampUtc, string path, Exception ex)
        {
            TimestampUtc = timestampUtc;
            Path = path ?? string.Empty;
            ExceptionType = ex?.GetType().FullName ?? "Unknown";
            Message = ex?.Message ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[{TimestampUtc:yyyy-MM-dd HH:mm:ss}] {Path} - {ExceptionType}: {Message}";
        }
    }
}

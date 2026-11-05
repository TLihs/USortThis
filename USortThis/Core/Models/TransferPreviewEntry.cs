namespace USortThis.Core.Models
{
    public sealed class TransferPreviewEntry
    {
        public string BridgeName { get; }
        public string RouteName { get; }
        public string SourcePath { get; }
        public string DestinationPath { get; }
        public string PlannedAction { get; }

        public TransferPreviewEntry(string bridgeName, string routeName, string sourcePath, string destinationPath, string plannedAction)
        {
            BridgeName = bridgeName;
            RouteName = routeName;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            PlannedAction = plannedAction;
        }
    }
}

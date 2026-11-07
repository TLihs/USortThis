namespace USortThis.Core.Models
{
    public sealed class TransferPreviewEntry
    {
        public string BridgeName { get; }
        public string RouteName { get; }
        public string SourcePath { get; }
        public string DestinationPath { get; }
        public string PlannedAction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransferPreviewEntry"/> class.
        /// </summary>
        /// <param name="bridgeName">The name of the bridge.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="plannedAction">The planned action for the transfer.</param>
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

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents the result of a rule test.
    /// </summary>
    public sealed class RuleTestResult
    {
        /// <summary>
        /// Gets the path of the sample that was tested.
        /// </summary>
        public string SamplePath { get; }

        /// <summary>
        /// Gets the status of the test.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the routes that matched the test.
        /// </summary>
        public string MatchingRoutes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleTestResult"/> class.
        /// </summary>
        /// <param name="samplePath">The path of the sample that was tested.</param>
        /// <param name="status">The status of the test.</param>
        /// <param name="matchingRoutes">The routes that matched the test.</param>
        public RuleTestResult(string samplePath, string status, string matchingRoutes)
        {
            SamplePath = samplePath;
            Status = status;
            MatchingRoutes = matchingRoutes;
        }
    }
}

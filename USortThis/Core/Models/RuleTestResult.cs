namespace USortThis.Core.Models
{
    public sealed class RuleTestResult
    {
        public string SamplePath { get; }
        public string Status { get; }
        public string MatchingRoutes { get; }

        public RuleTestResult(string samplePath, string status, string matchingRoutes)
        {
            SamplePath = samplePath;
            Status = status;
            MatchingRoutes = matchingRoutes;
        }
    }
}

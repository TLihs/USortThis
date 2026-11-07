using System.Collections.Generic;
using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IRuleTestInteractionService
    {
        /// <summary>Prompts the user to select sample files for testing profile routes.</summary>
        /// <returns>The selected file paths, or null when the user cancels.</returns>
        IReadOnlyList<string>? SelectSampleFiles();
        /// <summary>Displays the results of testing routing rules against sample files.</summary>
        /// <param name="results">The rule-test results to present.</param>
        void ShowResults(IReadOnlyList<RuleTestResult> results);
    }
}

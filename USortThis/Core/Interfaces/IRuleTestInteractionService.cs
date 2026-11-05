using System.Collections.Generic;
using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IRuleTestInteractionService
    {
        IReadOnlyList<string>? SelectSampleFiles();
        void ShowResults(IReadOnlyList<RuleTestResult> results);
    }
}

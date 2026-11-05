using System.Collections.Generic;
using System.Linq;
using System.Windows;
using USortThis.Core.Models;

namespace USortThis.UI.Views
{
    public partial class RuleTestResultsWindow : Window
    {
        public RuleTestResultsWindow(IReadOnlyList<RuleTestResult> results)
        {
            InitializeComponent();
            ResultsDataGrid.ItemsSource = results;
            var matched = results.Count(result => result.Status == "Matched");
            var noMatch = results.Count(result => result.Status == "No match");
            var errors = results.Count(result => result.Status == "Error");
            SummaryTextBlock.Text = $"{results.Count} sample file(s): {matched} matched, {noMatch} with no match, {errors} error(s).";
        }
    }
}

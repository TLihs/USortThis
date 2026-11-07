using System.Collections.Generic;
using System.Linq;
using System.Windows;
using USortThis.Core.Models;

namespace USortThis.UI.Views
{
    /// <summary>
    /// Interaction logic for RuleTestResultsWindow.xaml
    /// </summary>
    public partial class RuleTestResultsWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuleTestResultsWindow"/> class.
        /// </summary>
        /// <param name="results">The rule test results to display.</param>
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

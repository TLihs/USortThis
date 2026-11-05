using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using USortThis.UI.Views;

namespace USortThis.UI.Services
{
    public sealed class RuleTestInteractionService : IRuleTestInteractionService
    {
        public IReadOnlyList<string>? SelectSampleFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select sample files to test against profile routes",
                Multiselect = true,
                CheckFileExists = true
            };

            var owner = GetActiveWindow();
            var selected = owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
            return selected == true ? dialog.FileNames : null;
        }

        public void ShowResults(IReadOnlyList<RuleTestResult> results)
        {
            var window = new RuleTestResultsWindow(results);
            var owner = GetActiveWindow();
            if (owner != null)
                window.Owner = owner;
            window.ShowDialog();
        }

        private static Window? GetActiveWindow()
        {
            return Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsActive)
                ?? Application.Current?.MainWindow;
        }
    }
}

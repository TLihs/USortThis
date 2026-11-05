using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace USortThis.UI.Views
{
    public partial class TestFileGeneratorWindow : Window
    {
        private const int BufferSize = 1024 * 1024;
        private const string PrintableCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,;:!?-_+=()[]{}";
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public TestFileGeneratorWindow()
        {
            InitializeComponent();
            FolderTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select where to create the test file",
                SelectedPath = FolderTextBox.Text,
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                FolderTextBox.Text = dialog.SelectedPath;
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetFileSize(out var fileSize))
            {
                System.Windows.MessageBox.Show(this, "Enter a valid positive file size.", "Invalid Size", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fileName = FileNameTextBox.Text.Trim();
            var folder = FolderTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName || string.IsNullOrWhiteSpace(folder))
            {
                System.Windows.MessageBox.Show(this, "Enter a file name and valid folder.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var outputPath = Path.Combine(folder, fileName);
            if (File.Exists(outputPath))
            {
                var overwrite = System.Windows.MessageBox.Show(this, $"'{outputPath}' already exists. Replace it?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (overwrite != MessageBoxResult.Yes)
                    return;
            }

            GenerateButton.IsEnabled = false;
            CancelButton.Content = "Cancel";
            GenerationProgressBar.Value = 0;
            StatusTextBlock.Text = "Generating...";

            try
            {
                await GenerateFileAsync(outputPath, fileSize, _cancellationTokenSource.Token);
                StatusTextBlock.Text = $"Created {outputPath}";
                GenerationProgressBar.Value = 100;
            }
            catch (OperationCanceledException)
            {
                StatusTextBlock.Text = "Generation cancelled.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Generation failed.";
                System.Windows.MessageBox.Show(this, ex.Message, "File Generation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GenerateButton.IsEnabled = true;
                CancelButton.Content = "Close";
            }
        }

        private bool TryGetFileSize(out long size)
        {
            size = 0;
            if (!long.TryParse(SizeTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)
                return false;

            var multiplier = SizeUnitComboBox.SelectedIndex switch
            {
                0 => 1L,
                1 => 1024L * 1024,
                2 => 1024L * 1024 * 1024,
                _ => 0L
            };
            if (multiplier == 0 || value > long.MaxValue / multiplier)
                return false;

            size = value * multiplier;
            return true;
        }

        private async Task GenerateFileAsync(string path, long fileSize, CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            var chars = PrintableCharacters.ToCharArray();
            var bytesPerCharacter = Encoding.ASCII.GetBytes(chars);
            var written = 0L;

            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            while (written < fileSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunkSize = (int)Math.Min(buffer.Length, fileSize - written);
                RandomNumberGenerator.Fill(buffer.AsSpan(0, chunkSize));
                for (var index = 0; index < chunkSize; index++)
                    buffer[index] = bytesPerCharacter[buffer[index] % bytesPerCharacter.Length];

                await stream.WriteAsync(buffer.AsMemory(0, chunkSize), cancellationToken);
                written += chunkSize;
                GenerationProgressBar.Value = written * 100d / fileSize;
                StatusTextBlock.Text = $"Generated {written:N0} of {fileSize:N0} bytes";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            base.OnClosed(e);
        }
    }
}

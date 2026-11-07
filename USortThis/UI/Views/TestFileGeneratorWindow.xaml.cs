using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace USortThis.UI.Views
{
    /// <summary>
    /// Interaction logic for TestFileGeneratorWindow.xaml
    /// </summary>
    public partial class TestFileGeneratorWindow : Window
    {
        private const int BufferSize = 1024 * 1024;
        private const string PrintableCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,;:!?-_+=()[]{}";
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFileGeneratorWindow"/> class.
        /// </summary>
        public TestFileGeneratorWindow()
        {
            InitializeComponent();
            FolderTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Handles the Click event of the Browse button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Handles the Click event of the Generate button.
        /// Validates inputs, shows overwrite confirmation, and initiates async file generation.
        /// Updates UI with progress, success, or error messages.
        /// Cancels ongoing operations on subsequent clicks.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Tries to parse the file size from the SizeTextBox input and the selected unit from the SizeUnitComboBox.
        /// </summary>
        /// <param name="size">The parsed file size in bytes.</param>
        /// <returns>True if the file size is successfully parsed; otherwise, false.</returns>
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

        /// <summary>
        /// Asynchronously generates a test file with random content at the specified path.
        /// Updates the progress bar and status text during the operation.
        /// </summary>
        /// <param name="path">The file path where the test file will be created.</param>
        /// <param name="fileSize">The desired file size in bytes.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during file generation.</exception>
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

        /// <summary>
        /// Cancels the file generation operation if it is running and releases resources.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            base.OnClosed(e);
        }
    }
}

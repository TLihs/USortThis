using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using USortThis.Core.Models;
using USortThis.Core.Services;

namespace USortThis.UI.Views
{
    /// <summary>
    /// Interaction logic for ConfiguredLocationDialog.
    /// </summary>
    public partial class ConfiguredLocationDialog : Window
    {
        private readonly ConfiguredLocationService _locationService;
        private readonly ConfiguredLocationModel _location;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredLocationDialog"/> class.
        /// </summary>
        /// <param name="locationService">The location service.</param>
        /// <param name="location">The optional location model.</param>
        public ConfiguredLocationDialog(ConfiguredLocationService locationService, ConfiguredLocationModel? location = null)
        {
            InitializeComponent();
            CheckBox_CurrentCredentials.Checked += Credentials_Changed;
            CheckBox_CurrentCredentials.Unchecked += Credentials_Changed;
            _locationService = locationService;
            _location = location ?? new ConfiguredLocationModel(App.ServiceProvider!.GetRequiredService<USortThis.Core.Interfaces.IIdFactory>().CreateId(), string.Empty, string.Empty);
            TextBox_Label.Text = _location.Label;
            TextBox_Path.Text = _location.Path;
            TextBox_Username.Text = _location.Username;
            TextBox_Domain.Text = _location.Domain;
            CheckBox_CurrentCredentials.IsChecked = _location.UseCurrentWindowsCredentials;
            UpdateCredentialFields();
        }

        /// <summary>
        /// Handles the Checked and Unchecked events of the CheckBox_CurrentCredentials control.
        /// Updates the state of the credential fields based on the CheckBox_CurrentCredentials value.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The RoutedEventArgs instance containing the event data.</param>
        private void Credentials_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;

            UpdateCredentialFields();
        }

        /// <summary>
        /// Updates the enabled state of the credential fields (domain, username, password)
        /// based on the value of CheckBox_CurrentCredentials.
        /// </summary>
        private void UpdateCredentialFields()
        {
            bool useCurrent = CheckBox_CurrentCredentials.IsChecked == true;
            TextBox_Domain.IsEnabled = !useCurrent;
            TextBox_Username.IsEnabled = !useCurrent;
            PasswordBox_Password.IsEnabled = !useCurrent;
        }

        /// <summary>
        /// Copies the input values from the text boxes and password box
        /// to the corresponding properties of the _location model.
        /// </summary>
        private void CopyInputs()
        {
            _location.Label = TextBox_Label.Text.Trim();
            _location.Path = TextBox_Path.Text.Trim();
            _location.Username = TextBox_Username.Text.Trim();
            _location.Domain = TextBox_Domain.Text.Trim();
            _location.UseCurrentWindowsCredentials = CheckBox_CurrentCredentials.IsChecked == true;
        }

        /// <summary>
        /// Tests the connection using the values from the input fields.
        /// Displays the result message in the TextBlock_Status.
        /// Handles any exceptions by displaying the exception message in TextBlock_Status.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The RoutedEventArgs instance containing the event data.</param>
        private void Test_Click(object sender, RoutedEventArgs e)
        {
            CopyInputs();
            try
            {
                if (!string.IsNullOrEmpty(PasswordBox_Password.Password) && !_location.UseCurrentWindowsCredentials)
                    App.ServiceProvider!.GetRequiredService<WindowsCredentialStore>().Write(_location.CredentialTarget,
                        string.IsNullOrWhiteSpace(_location.Domain) ? _location.Username : $"{_location.Domain}\\{_location.Username}", PasswordBox_Password.Password);
                var result = _locationService.TestConnection(_location);
                TextBlock_Status.Text = result.Message;
            }
            catch (Exception exception)
            {
                TextBlock_Status.Text = exception.Message;
            }
        }

        /// <summary>
        /// Saves the configuration using the values from the input fields.
        /// Validates the input, ensuring that both display name and path are provided.
        /// Handles any exceptions that occur during the save operation by displaying the exception message in TextBlock_Status.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The RoutedEventArgs instance containing the event data.</param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            CopyInputs();
            if (string.IsNullOrWhiteSpace(_location.Label) || string.IsNullOrWhiteSpace(_location.Path))
            {
                TextBlock_Status.Text = "Enter both a display name and a path.";
                return;
            }
            try
            {
                _locationService.Save(_location, PasswordBox_Password.Password);
                DialogResult = true;
            }
            catch (Exception exception)
            {
                TextBlock_Status.Text = exception.Message;
            }
        }
    }
}

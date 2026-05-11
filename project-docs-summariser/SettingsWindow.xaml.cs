using System;
using System.Windows;

namespace project_docs_summariser
{
    /// <summary>
    /// Logika interakcji dla klasy SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            try
            {
                // Load existing preferences and API key
                DisableAnimationsCheckBox.IsChecked = Properties.Settings.Default.DisableAnimations;
                ApiKeyInput.Password = Properties.Settings.Default.ApiKey;
            }
            catch
            {
                // Ignored if settings are not fully initialized yet
            }
        }

        private void DisableAnimationsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DisableAnimations = DisableAnimationsCheckBox.IsChecked == true;
            Properties.Settings.Default.Save();
        }

        private void SaveKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.ApiKey = ApiKeyInput.Password;
                Properties.Settings.Default.Save();

                MessageBox.Show("API Key saved successfully!", "Settings Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
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
            DisableAnimationsCheckBox.IsChecked = Properties.Settings.Default.DisableAnimations;
        }

        private void DisableAnimationsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DisableAnimations = DisableAnimationsCheckBox.IsChecked == true;
            Properties.Settings.Default.Save();
        }
    }
}

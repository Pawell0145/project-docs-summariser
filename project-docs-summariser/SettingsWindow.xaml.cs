using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace project_docs_summariser
{
    public partial class SettingsWindow : Window
    {
        private readonly string[] availableColors = new string[]
        {
            "#DCA550", "#4EC9B0", "#28A745", "#007ACC",
            "#9D88FF", "#FF7EB3", "#FF4C4C", "#FFD700",
            "#00FA9A", "#1E90FF", "#FF8C00", "#FFFFFF"
        };

        public SettingsWindow()
        {
            InitializeComponent();

            try
            {
                DisableAnimationsCheckBox.IsChecked = Properties.Settings.Default.DisableAnimations;
                ApiKeyInput.Password = Properties.Settings.Default.ApiKey;

                int savedSpeed = Properties.Settings.Default.AnimationSpeed;
                if (savedSpeed <= 0) savedSpeed = 5;
                AnimationSpeedSlider.Value = savedSpeed;
                AnimationSpeedValueText.Text = $"{savedSpeed} ms";

                string savedKeywordColor = Properties.Settings.Default.KeywordColor;
                string savedSentenceColor = Properties.Settings.Default.SentenceColor;

                if (string.IsNullOrEmpty(savedKeywordColor)) savedKeywordColor = "#DCA550";
                if (string.IsNullOrEmpty(savedSentenceColor)) savedSentenceColor = "#4EC9B0";

                PopulatePalette(KeywordColorPalette, savedKeywordColor, KeywordSwatch_Click);
                PopulatePalette(SentenceColorPalette, savedSentenceColor, SentenceSwatch_Click);

                string savedLang = Properties.Settings.Default.AppLanguage;
                if (string.IsNullOrEmpty(savedLang)) savedLang = "English";

                foreach (ComboBoxItem item in LanguageCombo.Items)
                {
                    if (item.Tag.ToString() == savedLang) LanguageCombo.SelectedItem = item;
                }
            }
            catch 
            { 
            }
        }

        private void DisableAnimationsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DisableAnimations = DisableAnimationsCheckBox.IsChecked == true;
            Properties.Settings.Default.Save();
        }

        private void AnimationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AnimationSpeedValueText != null)
            {
                int speed = (int)e.NewValue;
                AnimationSpeedValueText.Text = $"{speed} ms";

                Properties.Settings.Default.AnimationSpeed = speed;
                Properties.Settings.Default.Save();
            }
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

        private void PopulatePalette(WrapPanel panel, string selectedColor, MouseButtonEventHandler clickHandler)
        {
            panel.Children.Clear();

            foreach (var hex in availableColors)
            {
                Border swatch = new Border
                {
                    Width = 26,
                    Height = 26,
                    Margin = new Thickness(3),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom(hex),
                    Tag = hex,
                    Cursor = Cursors.Hand,
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = (hex == selectedColor) ? new Thickness(2) : new Thickness(0),
                    BorderBrush = Brushes.White
                };

                swatch.MouseLeftButtonDown += clickHandler;
                panel.Children.Add(swatch);
            }
        }

        private void KeywordSwatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedSwatch)
            {
                string hex = clickedSwatch.Tag.ToString();
                Properties.Settings.Default.KeywordColor = hex;
                Properties.Settings.Default.Save();
                UpdatePaletteSelection(KeywordColorPalette, hex);
            }
        }

        private void SentenceSwatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedSwatch)
            {
                string hex = clickedSwatch.Tag.ToString();
                Properties.Settings.Default.SentenceColor = hex;
                Properties.Settings.Default.Save();
                UpdatePaletteSelection(SentenceColorPalette, hex);
            }
        }

        private void UpdatePaletteSelection(WrapPanel panel, string selectedHex)
        {
            foreach (Border swatch in panel.Children)
            {
                swatch.BorderThickness = (swatch.Tag.ToString() == selectedHex) ? new Thickness(2) : new Thickness(0);
            }
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (LanguageCombo.SelectedItem is ComboBoxItem langItem)
            {
                Properties.Settings.Default.AppLanguage = langItem.Tag.ToString();
                Properties.Settings.Default.Save();
            }
        }
    }
}
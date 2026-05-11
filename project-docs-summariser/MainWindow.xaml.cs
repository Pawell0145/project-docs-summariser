using project_docs_summariser;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfAiIntegration
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshProjectList();
        }

        private void RefreshProjectList()
        {
            List<ProjectModel> history = ProjectManager.ListSavedProjects();
            ProjectListBox.ItemsSource = history;
        }

        private void OpenCreateDialog_Click(object sender, RoutedEventArgs e)
        {
            CreatePlanWindow createWindow = new CreatePlanWindow();
            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();

            if (result == true)
            {
                RefreshProjectList();

                if (ProjectListBox.Items.Count > 0)
                {
                    ProjectListBox.SelectedIndex = 0;
                    LoadSelectedProject();
                }
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedProject();
        }

        private void ProjectListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoadSelectedProject();
        }

        private void LoadSelectedProject()
        {
            if (ProjectListBox.SelectedItem is ProjectModel selectedItem)
            {
                ProjectModel project = ProjectManager.LoadProject(selectedItem.FilePath);

                if (project != null && !string.IsNullOrEmpty(project.RawPlan))
                {
                    project.FilePath = selectedItem.FilePath;
                    StudyWindow studyWindow = new StudyWindow();
                    studyWindow.LoadPlan(project);
                    Hide();

                    studyWindow.Closed += (s, args) =>
                    {
                        this.Show();
                        RefreshProjectList();
                    };

                    studyWindow.Show();
                }
                else
                {
                    MessageBox.Show("Could not load the selected study plan. The file may be corrupted or missing.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a saved study plan from the left menu first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectListBox.SelectedItem is ProjectModel selectedItem)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to permanently delete '{selectedItem.ProjectName}'?",
                    "Delete Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    ProjectManager.DeleteProject(selectedItem.FilePath);
                    RefreshProjectList();
                }
            }
            else
            {
                MessageBox.Show("Please select a study plan from the list to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectListBox.SelectedItem is ProjectModel selectedProject)
            {
                WelcomePanel.Visibility = Visibility.Collapsed;
                DetailsPanel.Visibility = Visibility.Visible;
                DetailTitle.Text = selectedProject.ProjectName.ToUpper();
                DetailTotalDays.Text = selectedProject.Days.ToString();
                DetailHours.Text = $"{selectedProject.Hours}h";
                DetailDate.Text = selectedProject.CreatedAt.ToString("yyyy-MM-dd");

                DetailNotes.Text = string.IsNullOrWhiteSpace(selectedProject.UserNotes)
                    ? "No specific teaching preferences provided."
                    : selectedProject.UserNotes;

                int completedCount = selectedProject.CompletedDays?.Count ?? 0;
                double progressPercent = selectedProject.Days > 0
                    ? (double)completedCount / selectedProject.Days * 100
                    : 0;

                DetailProgressCountText.Text = $"{completedCount} / {selectedProject.Days}";
                double targetWidth = (progressPercent / 100) * 440;
                if (targetWidth < 15 && completedCount > 0) targetWidth = 15;

                System.Windows.Media.Animation.DoubleAnimation anim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new System.Windows.Media.Animation.QuarticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                ProgressBarFill.BeginAnimation(WidthProperty, anim);
            }
            else
            {
                WelcomePanel.Visibility = Visibility.Visible;
                DetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while (dep != null)
            {
                if (dep is ListBoxItem ||
                    dep is Button ||
                    dep is System.Windows.Controls.Primitives.ScrollBar ||
                    (dep is Border border && border.Name == "DetailsPanel"))
                {
                    return;
                }
                dep = VisualTreeHelper.GetParent(dep);
            }

            ProjectListBox.SelectedItem = null;
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.IsSelected)
            {
                if (e.ClickCount == 1)
                {
                    ProjectListBox.SelectedItem = null;
                    e.Handled = true;
                }
            }
        }
    }

}
using project_docs_summariser;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
    }
}
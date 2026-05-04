using project_docs_summariser;
using System.Windows;
using System.Windows.Controls;

namespace WpfAiIntegration
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenCreateDialog_Click(object sender, RoutedEventArgs e)
        {
            CreatePlanWindow createWindow = new CreatePlanWindow();
            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(createWindow.GeneratedPlan))
            {
                StudyWindow studyWindow = new StudyWindow();
                studyWindow.LoadPlan(createWindow.GeneratedPlan);
                studyWindow.Show();
            }

        }
    }
}
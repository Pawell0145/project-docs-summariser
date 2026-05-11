using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace project_docs_summariser
{
    public static class ProjectManager
    {
        private static readonly string StorageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIAcademicSync",
            "Projects"
        );

        public static void SaveProject(ProjectModel project)
        {
            if (!Directory.Exists(StorageDirectory))
                Directory.CreateDirectory(StorageDirectory);

            string safeName = string.Join("_", project.ProjectName.Split(Path.GetInvalidFileNameChars()));

            if (string.IsNullOrEmpty(project.FilePath))
            {
                string fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                project.FilePath = Path.Combine(StorageDirectory, fileName);
            }

            string jsonString = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(project.FilePath, jsonString);
        }

        public static void UpdateProjectProgress(string filePath, List<int> completedDays)
        {
            var project = LoadProject(filePath);
            if (project != null)
            {
                project.CompletedDays = completedDays;
                project.FilePath = filePath;
                SaveProject(project);
            }
        }

        public static void UpdateProjectPlan(string filePath, string newRawPlan)
        {
            var project = LoadProject(filePath);
            if (project != null)
            {
                project.RawPlan = newRawPlan;
                project.FilePath = filePath;
                SaveProject(project);
            }
        }

        public static List<ProjectModel> ListSavedProjects()
        {
            List<ProjectModel> projects = new List<ProjectModel>();
            if (!Directory.Exists(StorageDirectory)) return projects;

            string[] files = Directory.GetFiles(StorageDirectory, "*.json");
            foreach (string file in files)
            {
                try
                {
                    string jsonString = File.ReadAllText(file);
                    ProjectModel project = JsonSerializer.Deserialize<ProjectModel>(jsonString);
                    if (project != null)
                    {
                        project.FilePath = file;
                        projects.Add(project);
                    }
                }
                catch { }
            }
            projects.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return projects;
        }

        public static ProjectModel LoadProject(string filePath)
        {
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<ProjectModel>(jsonString);
            }
            return null;
        }

        public static void DeleteProject(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace project_docs_summariser
{
    public class ProjectModel
    {
        public string ProjectName { get; set; }
        public string RawPlan { get; set; }
        public int Days { get; set; }
        public int Hours { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserNotes { get; set; }
        public List<int> CompletedDays { get; set; } = new List<int>();

        [JsonIgnore]
        public string FilePath { get; set; }

        [JsonIgnore]
        public string DisplayTitle => $"{ProjectName} ({Days} Days)";
    }
}
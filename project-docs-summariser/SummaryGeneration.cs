using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace project_docs_summariser
{
    public class SummaryGeneration
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TaskType
        {
            MultipleChoice,
            ShortAnswer,
            Essay,
            Calculation,
            CaseStudy,
            FillInTheBlanks,
            CodeSnippet
        }

        public class ExamTask
        {
            public TaskType Type { get; set; }
            public string Instruction { get; set; }
            public List<string> Options { get; set; } = new List<string>();
        }

        public class SummaryExam
        {
            public string DetectedSubject { get; set; }
            public List<ExamTask> Tasks { get; set; } = new List<ExamTask>();
        }
    }
}
using System.Collections.Generic;
using Portal.Models;

namespace Portal.ViewModel
{
    // Модель простого журнала (Journals.IsSystem == true):
    // журнал не подчиняется модульно-рейтинговой системе, отметки выставляются сквозные,
    // без правил редактирования, СО-зачётов и недельных окон.
    public class SimpleJournalViewModel
    {
        public Group Group { get; set; }
        public Subject Subject { get; set; }
        public Department Department { get; set; }

        public bool HasAccess { get; set; }
        public bool JournalExists { get; set; }

        public string Subgroup { get; set; }
        public bool HasSubgroups { get; set; }
        public List<string> SubgroupNames { get; set; } = new List<string>();

        public List<Student> Students { get; set; } = new List<Student>();
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();

        // Средний балл студента по предмету: среднее арифметическое числовых отметок (1-10)
        public Dictionary<int, double?> StudentAverages { get; set; } = new Dictionary<int, double?>();

        // MarksByLesson[lessonID][studentID] -> отметка
        public Dictionary<int, Dictionary<int, Mark>> MarksByLesson { get; set; }
            = new Dictionary<int, Dictionary<int, Mark>>();
    }
}

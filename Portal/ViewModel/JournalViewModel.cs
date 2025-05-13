using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class JournalViewModel
    {
        public List<JournalLessons> JournalLessons { get; set; }
        public List<JournalMarks> JournalMarks { get; set; }
        public List<Student> Students { get; set; }
        public List<Lesson> StatementLessons { get; set; }
        public Subject Subject { get; set; }
        public Group Group { get; set; }
        public Department Department { get; set; }
    }
}

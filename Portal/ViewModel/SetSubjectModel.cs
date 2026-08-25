using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class SetSubjectModel
    {
        public List<Department> Departments { get; set; }
        public List<Subject> Subjects { get; set; }
        public List<int> CompletedSubjectIds { get; set; } = new List<int>();
        public bool ShowCompleted { get; set; }
        public int AvailableSubjectsCount { get; set; }
        public int CompletedSubjectsCount { get; set; }
    }
}

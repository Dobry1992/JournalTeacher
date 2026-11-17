using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class SubgroupEditViewModel
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        public string CurrentName { get; set; }   // исходное имя — для поиска записей
        public string NewName { get; set; }

        public List<Student> CurrentStudents { get; set; } = new();
        public List<Student> AvailableStudents { get; set; } = new();
        public List<int> SelectedStudentIDs { get; set; } = new();
    }
}
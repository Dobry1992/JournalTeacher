using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class CourseStudentViewModel
    {
        public int StudentId { get; set; }
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string LastName { get; set; }

        public string FullName => $"{LastName} {Name} {Surname}".Trim();

        public List<SubjectAverageViewModel> SubjectAverages { get; set; } = new();
    }
}

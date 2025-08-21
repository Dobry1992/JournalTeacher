using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class StudentViewModel
    {
        public int StudentId { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }
        public string LastName { get; set; }

        public string FullName => $"{Surname} {Name} {LastName}".Trim();

        public List<SubjectAverageViewModel> SubjectAverages { get; set; } = new();
    }
}

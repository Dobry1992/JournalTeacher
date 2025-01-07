using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Final
{
    public class FinalGroup
    {
        public Group Group { get; set; }
        public List<Subject> Subjects { get; set; }
        public List<FinalStudent> FinalStudents { get; set; }
    }
}

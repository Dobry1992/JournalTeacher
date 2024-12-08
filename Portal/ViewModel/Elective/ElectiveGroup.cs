using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Elective
{
    public class ElectiveGroup
    {
        public Group Group { get; set; }
        public List<ElectiveStudent> ElectiveStudents { get; set; }
    }
}

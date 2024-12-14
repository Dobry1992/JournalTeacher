using Portal.Models;
using Portal.Models.Elective;

namespace Portal.ViewModel.Elective
{
    public class ElectiveStudentMark
    {
        public Student Student { get; set; }
        public ElectiveMark ElectiveMark { get; set; }
        public Group Group { get; set; }
    }
}

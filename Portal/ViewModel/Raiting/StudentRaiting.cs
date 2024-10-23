using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Raiting
{
    public class StudentRaiting
    {
        public Student Student { get; set; }
        public Group Group { get; set; }
        public double Raiting { get; set; }
        public double CommonRaiting { get; set; }
        public List<SubjectRaiting> SubjectRaitings { get; set; }
    }
}

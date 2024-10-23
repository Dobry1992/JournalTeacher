using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class MarkSubjectFinal
    {
        public Subject Subject { get; set; }
        public double Value { get; set; }
        public List<Mark> ValueK { get; set; }
        public List<Mark> FinalMarks { get; set; }
        public List<Mark> ControlMarks { get; set; }
        
    }
}

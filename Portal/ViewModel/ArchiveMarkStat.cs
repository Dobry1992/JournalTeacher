using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class ArchiveMarkStat
    {
        public Subject Subject { get; set; }
        public double Value { get; set; }
        public List<MarkArhive> ValueK { get; set; }
        public List<MarkArhive> FinalMarks { get; set; }
        public List<MarkArhive> ControlMarks { get; set; }
    }
}

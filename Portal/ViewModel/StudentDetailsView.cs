using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class StudentDetailsView
    {
        public Student Student { get; set; }
        public Dictionary<string, double> AttendancePercent { get; set; }
        public Dictionary<string, int> AttendanceNumber {  get; set; }
        public List<MarkSubjectFinal> MarkSubjectFinals { get; set; }
        public List<FinalMark> FinalMarks { get; set; }
        public List<Mark> NegativeMarks { get; set; }
        public List<object> Radar { get; set; }
        public double Raiting {  get; set; }
        public Dictionary<int,int> MarksNumber {  get; set; }
        public Dictionary<int, decimal> MarksPercent { get; set; }
        public string YearsStudy {  get; set; }
        public Dictionary<string, string> RaitingTimeSubject { get; set; }
        public Subject? Subject { get; set; }
        public Dictionary<string, string> RaitingTime {  get; set; }
    }
}

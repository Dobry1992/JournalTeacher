using Portal.ViewModel.Raiting;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class SpecialityAnalyticViewModel
    {
        public int SpecialityID { get; set; }
        public string SpecialityName { get; set; }
        public string Term { get; set; }
        public double Raiting { get; set; }
        public int GroupNumber { get; set; }
        public int StudentNumber {  get; set; }
        public int EnterYear { get; set; }
        public List<StudentRaiting> StudentsRaiting { get; set; }
        public List<InstGroupRaiting> GroupsRaiting { get; set; }
        public Dictionary<int, int> MarksNumber { get; set; }
        public Dictionary<int, decimal> MarksPercent { get; set; }
        public Dictionary<string, string> TimeRaiting { get; set; }
        public string Year { get; set; }
    }
}

using System;

namespace Portal.ViewModel
{
    public class AcademicYearInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Name { get; set; }

        public bool ContainsDate(DateTime date)
        {
            return date >= StartDate && date <= EndDate;
        }
    }
}

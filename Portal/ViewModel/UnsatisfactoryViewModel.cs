using System;

namespace Portal.ViewModel
{
    public class UnsatisfactoryViewModel
    {
        public int StudentID { get; set; }
        public int GroupID { get; set; }
        public string StudentFullName { get; set; }
        public int SubjectID { get; set; }
        public string SubjectFullName { get; set; }
        public string UnsatisfactoryMark { get; set; }
        public DateTime UsatisfactoryDate { get; set; }
        public string? CorrectedValue { get; set; }
        public DateTime? CorrectedDate { get; set; }
    }
}

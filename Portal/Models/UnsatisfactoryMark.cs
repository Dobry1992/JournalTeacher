using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class UnsatisfactoryMark
    {
        [Key]
        public int UnsatisfactoryMarkID { get; set; }
        public int MarkID { get; set; }
        public int GroupID { get; set; }
        public int StudentID { get; set; }
        public int SubjectID { get; set; }
        public string UnsatisfactoryValue { get; set; }
        public DateTime UnsatisfactoryDate { get; set; }
        public string? CorrectedValue { get; set; }
        public DateTime? CorrectedDate { get; set; }
        public bool Status { get; set; }
    }
}

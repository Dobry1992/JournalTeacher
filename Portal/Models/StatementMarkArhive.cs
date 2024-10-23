using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class StatementMarkArhive
    {
        [Key]
        public int StatementMarkArhiveID { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public string SignatureOfTeacher { get; set; }
        public string HistoryOfMark { get; set; }
        public int InstituteID { get; set; }
        public int SpecialityID { get; set; }
        public int GroupID { get; set; }
        public int TypeOfExerciseID { get; set; }
        public int StatementLessonID { get; set; }
        public int StudentArhiveID { get; set; }
        public StudentArhive StudentArhive { get; set; }
    }
}

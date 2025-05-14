using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    [Index("Date", "FlagF", "SubjectID", "GroupID")]
    public class Mark
    {
        [Key]
        public int MarkID { get; set; } 
        public string Value { get; set; } 
        public DateTime Date { get; set; } 
        public string Comment { get; set; } 
        public string SignatureOfTeacher { get; set; } 
        public string HistoryOfMark { get; set; } 
        public int FlagF { get; set; }

        public int InstituteID { get; set; }
        public int SubjectID { get; set; }
        public int GroupID { get; set; }
        public int LessonID { get; set; }
        public int TypeOfExerciseID { get; set; }
        public int DepartmentID { get; set; }
        public int SpecialityID { get; set; }
        public int ChangeCounter { get; set; }

        public int ThemeID { get; set; } 
        public Theme Theme { get; set; } 
        public int StudentID { get; set; } 
        public Student Student { get; set; } 
    }
}

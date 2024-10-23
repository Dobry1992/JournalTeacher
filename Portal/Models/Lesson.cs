using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    [Index("SubjectID", "Date", "FlagF")]
    public class Lesson
    {
        [Key]
        public int LessonID { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public string Signature { get; set; }
        public int FlagF { get; set; }
        public int SubjectID { get; set; }
        public int ThemeID { get; set; }
        public Theme Theme { get; set; }
        public int TypeOfExerciseID { get; set; }
        public TypeOfExercise TypeOfExercise { get; set; }
        public int GroupID { get; set; }
        public Group Group { get; set; } 
    }
}
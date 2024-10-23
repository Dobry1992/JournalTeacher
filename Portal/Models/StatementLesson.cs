using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class StatementLesson
    {
        [Key]
        public int StatementLessonID { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public string Signature { get; set; }
        public int TypeOfExerciseID { get; set; }
        public TypeOfExercise TypeOfExercise { get; set; }
        public int GroupID { get; set; }
        public Group Group { get; set; }
    }
}

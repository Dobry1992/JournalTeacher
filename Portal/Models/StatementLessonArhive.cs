using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class StatementLessonArhive
    {
        [Key]
        public int StatementLessonArhiveID { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public string Signature { get; set; }
        public int TypeOfExerciseID { get; set; }
        public TypeOfExercise TypeOfExercise { get; set; }
        public int GroupArhiveID { get; set; }
        public GroupArhive GroupArhive { get; set; }
    }
}

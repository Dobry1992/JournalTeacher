using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class TypeOfExercise
    {
        [Key]
        public int TypeOfExerciseID { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public bool Arch { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<LessonArhive> LessonArhives { get; set; }
        public ICollection<StatementLesson> StatementLessons { get; set; }
        public ICollection<StatementLessonArhive> StatementLessonArhives { get; set; }
    }
}

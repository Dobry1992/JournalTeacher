using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class GroupArhive
    {
        [Key]
        public int GroupArhiveID { get; set; }
        public string Name { get; set; }
        public DateTime DateEnter { get; set; }
        public DateTime DateExit { get; set; }
        public int InstituteID { get; set; }
        public int SpecialityID { get; set; }
        public Speciality Speciality { get; set; }
        public ICollection<StudentArhive> Students { get; set; }
        public ICollection<LessonArhive> Lessons { get; set; }
        public ICollection<StatementLessonArhive> StatementLessons { get; set; }
        public ICollection<JournalArhive> Journals { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Portal.Models
{
    [Index("DateExit")]
    public class Group
    {
        [Key]
        public int GroupID { get; set; } 
        public string Name { get; set; } 
        public DateTime DateEnter { get; set; } 
        public DateTime DateExit { get; set; }
        public int InstituteID { get; set; }
        public int SpecialityID { get; set; } 
        public Speciality Speciality { get; set; } 
        public ICollection<Student> Students { get; set; } 
        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<StatementLesson> StatementLessons { get; set; }
        public ICollection<Journal> Journals { get; set; }
    }
}

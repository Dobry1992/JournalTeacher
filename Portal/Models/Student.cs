using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Student
    {
        [Key]
        public int StudentID { get; set; } 
        public string Name { get; set; } 
        public string Surname { get; set; }
        public string LastName { get; set; } 
        public string PlaceOfBirth { get; set; } 
        public DateTime DateOfBirth { get; set; } 
        public bool Status { get; set; }
        public int InstituteID { get; set; }
        public int GroupID { get; set; } 
        public Group Group { get; set; }
        public ICollection<Mark> Marks { get; set; }
        public ICollection<StatementMark> StatementMarks { get; set; }
    }
}

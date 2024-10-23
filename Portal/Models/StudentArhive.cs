using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class StudentArhive
    {
        [Key]
        public int StudentArhiveID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string LastName { get; set; }
        public string PlaceOfBirth { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool Status { get; set; }
        public int InstituteID { get; set; }
        public int GroupArhiveID { get; set; }
        public GroupArhive GroupArhive { get; set; }
        public ICollection<MarkArhive> Marks { get; set; }
        public ICollection<StatementMarkArhive> StatementMarks { get; set; }
    }
}

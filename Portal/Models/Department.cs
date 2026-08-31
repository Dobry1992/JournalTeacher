using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }
        public bool Arch { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public int InstituteID { get; set; }
        public ICollection<Subject> Subjects { get; set; }
    }
}
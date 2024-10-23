using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Subject
    {
        [Key]
        public int SubjectID { get; set; } 
        public string Name { get; set; } 
        [Required]
        public string ShortName { get; set; }
        public bool Arch { get; set; }
        public int DepartmentID { get; set; } 
        public Department Department { get; set; }
        public ICollection<Theme> Themes { get; set; } 
        public ICollection<Journal> Journals { get; set; }
        public ICollection<JournalArhive> JournalArhives { get; set; }
    }
}

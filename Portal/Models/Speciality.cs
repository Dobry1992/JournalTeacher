using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Speciality
    {
        [Key]
        public int SpecialityID { get; set; }
        public string Name { get; set; }
        public int TimeOFStudy { get; set; }
        public bool Arch { get; set; }
        public int InstituteID { get; set; }
        public Institute Institute { get; set; }
        public ICollection<Group> Groups { get; set; }
        public ICollection<GroupArhive> GroupArhives { get; set; }
    }
}
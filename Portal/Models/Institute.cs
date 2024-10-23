using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Institute
    {
        [Key]
        public int InstituteID { get; set; }
        public bool Arch { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public ICollection<Speciality> Specialities { get; set; }
    }
}

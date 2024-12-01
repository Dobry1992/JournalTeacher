using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Elective
{
    public class ElectiveType
    {
        [Key]
        public int ElectiveTypeID { get; set; }
        [Required]
        public string Name { get; set; }
        public bool Archive { get; set; }
        public ICollection<ElectiveLesson> Lessons { get; set; }
    }
}

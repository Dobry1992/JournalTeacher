using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Elective
{
    public class ElectiveTheme
    {
        [Key]
        public int ElectiveThemeID { get; set; }
        [Required]
        public string Name { get; set; }
        public string ShortName { get; set; }
        public bool Archive { get; set; }
        public int ElectiveID { get; set; }
        public Elective Elective { get; set; }
        public ICollection<ElectiveLesson> Lessons { get; set; }
    }
}

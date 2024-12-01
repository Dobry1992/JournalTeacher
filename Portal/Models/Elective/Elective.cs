using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Elective
{
    public class Elective
    {
        [Key]
        public int ElectiveID {  get; set; }
        [Required]
        public string Name {  get; set; }
        public int DepartmentID { get; set; }
        public ICollection<ElectiveTheme> Themes { get; set; }
        public ICollection<El_Stud_Link> StudLinks { get; set; }
    }
}

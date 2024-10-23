using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Sub_SpecLink
    {
        [Key]
        public int LinkID { get; set; }
        public string SubjectID { get; set; }
        public string SpecialityID { get; set; }
    }
}

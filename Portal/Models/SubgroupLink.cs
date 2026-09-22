using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class SubgroupLink
    {
        [Key]
        public int SubgroupLinkID { get; set; }
        public int GroupID { get; set; }
        public int SubjectID { get; set; }
        public int StudentID { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
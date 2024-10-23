using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class TeacherNoPC
    {
        [Key]
        public int TeacherNoPCID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Surname { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Role { get; set; }
    }
}

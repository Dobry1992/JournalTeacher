using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleID { get; set; }
        [Required]
        public string Name { get; set; }
        public byte[] File { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Elective
{
    public class El_Stud_Link
    {
        [Key]
        public int Id { get; set; }
        public Elective Elective { get; set; }
        public int ElectiveID { get; set; }
        public int StudentID {  get; set; }
    }
}

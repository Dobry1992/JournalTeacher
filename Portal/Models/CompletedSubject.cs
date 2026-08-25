using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class CompletedSubject
    {
        [Key]
        public int Id { get; set; }
        public int SubjectID { get; set; }
        public int GroupID { get; set; }
    }
}
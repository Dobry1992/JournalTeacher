using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class JournalArhive
    {
        [Key]
        public int JournalID { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public int GroupArhiveID { get; set; }
        public GroupArhive GroupArhive { get; set; }
        public int SubjectID { get; set; }
        public Subject Subject { get; set; }
    }
}

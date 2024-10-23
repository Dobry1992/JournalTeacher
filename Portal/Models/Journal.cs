using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    [Index("GroupID", "SubjectID")]
    public class Journal
    {
        [Key]
        public int JournalID { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public int GroupID { get; set; }
        public Group Group { get; set; }
        public int SubjectID { get; set; }
        public Subject Subject { get; set; }
    }
}

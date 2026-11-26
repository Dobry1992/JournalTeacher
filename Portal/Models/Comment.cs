using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    [Index("StudentID", "Date")]
    public class Comment
    {
        [Key]
        public int CommentID { get; set; }
        public int StudentID { get; set; }
        public string CommentString { get; set; }
        public DateTime Date { get; set; }
        public string Author { get; set; }
    }
}

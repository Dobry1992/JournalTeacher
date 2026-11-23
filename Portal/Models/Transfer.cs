using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Transfer
    {
        [Key]
        public int TransferID { get; set; }
        public int StudentID { get; set; }
        public int OldGroupID { get; set; }
        public int NewGroupID { get; set; }
        public DateTime Date { get; set; }
        public string? Comment { get; set; }
        public ICollection<HistoryLesson> HistoryLessons { get; set; } = new List<HistoryLesson>();
    }
}

using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    [Index("Date", "FlagF")]
    public class HistoryMark
    {
        [Key]
        public int HistoryMarkID { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }
        public string? Comment { get; set; }
        public string SignatureOfTeacher { get; set; }
        public string HistoryOfMark { get; set; }
        public int FlagF { get; set; }
        public int HistoryLessonID { get; set; }
        public HistoryLesson HistoryLesson { get; set; }
    }
}

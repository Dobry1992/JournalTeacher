using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Elective
{
    [Index("Date", "FlagF")]
    public class ElectiveMark
    {
        [Key]
        public int ElectiveMarkID { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public string SignatureOfTeacher { get; set; }
        public string HistoryOfMark { get; set; }
        public int FlagF { get; set; }
        public int ElectiveLessonID { get; set; }
        public ElectiveLesson ElectiveLesson { get; set; }
        public int StudentID { get; set; }
    }
}

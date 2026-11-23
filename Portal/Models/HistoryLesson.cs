using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models
{
    [Index("ThemeID", "Date", "FlagF")]
    public class HistoryLesson
    {
        public int HistoryLessonID { get; set; }
        public DateTime Date { get; set; }
        public string? Comment { get; set; }
        public string Signature { get; set; }
        public int FlagF { get; set; }
        public int ThemeID { get; set; }
        public int TypeOfExerciseID { get; set; }
        public int TransferID { get; set; }
        public Transfer Transfer { get; set; }
        public HistoryMark HistoryMark { get; set; }
    }
}
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Elective
{
    [Index("SubjectID", "Date", "FlagF")]
    public class ElectiveLesson
    {
        [Key]
        public int ElectiveLessonID { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public string Signature { get; set; }
        public int FlagF { get; set; }
        public int DepartmentID { get; set; }
        public int ElectiveThemeID { get; set; }
        public ElectiveTheme Theme { get; set; }
        public int ElectiveTypeID { get; set; }
        public ElectiveType Type { get; set; }
        public ICollection<ElectiveMark> Marks { get; set; }
        
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Theme
    {
        [Key]
        public int ThemeID { get; set; } 
        public string Name { get; set; } 
        public string Time { get; set; } 
        public string ShortName { get; set; } 
        public bool Arch { get; set; } 
        public int SubjectID { get; set; } 
        public Subject Subject { get; set; } 
        public ICollection<Mark> Marks { get; set; } 
        public ICollection<MarkArhive> MarkArhives { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<LessonArhive> LessonArhives { get; set; }
    }
}

using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class ControlsViewModel
    {
        public List<Lesson> Lessons { get; set; }
        public List<Mark> Marks { get; set; }
        public List<Student> Students { get; set; }
        public Subject Subject { get; set; }
        public Group Group { get; set; }
        public Department Department { get; set; }
    }
}

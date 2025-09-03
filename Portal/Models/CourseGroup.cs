using System.Collections.Generic;

namespace Portal.Models
{
    public class CourseGroup
    {
        public Group Group { get; set; }
        public List<CourseDiscipline> CourseDisciplines { get; set; }
    }
}

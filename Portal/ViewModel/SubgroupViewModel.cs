using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class SubgroupViewModel
    {
        public string Name { get; set; }
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        public List<Student> Students { get; set; } = new();
    }
}

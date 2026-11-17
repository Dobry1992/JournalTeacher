using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class SubgroupDeleteViewModel
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        public string SubgroupName { get; set; }
        public List<Student> Students { get; set; } = new();
    }
}
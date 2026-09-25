using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class SubgroupSelectSubjectViewModel
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public List<Subject> Subjects { get; set; } = new List<Subject>();
    }
}
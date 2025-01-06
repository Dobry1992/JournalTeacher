using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Statement
{
    public class GroupRaiting
    {
        public Group Group { get; set; }
        public List<Subject> Subjects { get; set; }
        public List<StudRaiting> Raitings { get; set; }
    }
}

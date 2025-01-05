using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Statement
{
    public class StudRaiting
    {
        public Student Student { get; set; }
        public List<SubRaiting> SubRaitings { get; set; }
    }
}

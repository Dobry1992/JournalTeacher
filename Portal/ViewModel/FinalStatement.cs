using Portal.ViewModel.Final;
using Portal.ViewModel.Statement;
using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class FinalStatement
    {
        public List<FinalGroup> FinalGroups { get; set; }
        public List<Portal.ViewModel.Statement.GroupRaiting> GroupRaitings { get; set; }
    }
}

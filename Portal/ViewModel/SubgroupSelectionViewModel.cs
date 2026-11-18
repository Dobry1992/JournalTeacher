using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class SubgroupSelectionViewModel
    {
        public int GroupID { get; set; }
        public int SubjectID { get; set; }
        public string GroupName { get; set; }
        public string SubjectName { get; set; }

        /// <summary>"Journal" или "AdjustedJournal"</summary>
        public string ReturnAction { get; set; }

        public List<SubgroupOption> Subgroups { get; set; } = new();
    }

    public class SubgroupOption
    {
        public string Name { get; set; }
        public int StudentCount { get; set; }
    }
}
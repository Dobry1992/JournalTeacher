using System.Collections.Generic;

namespace Portal.ViewModel
{
    public class GroupSummaryStatementViewModel
    {
        public int StudentID { get; set; }
        public int GroupID { get; set; }
        public string StudentName { get; set; }
        public string StudentLastName { get; set; }
        public string StudentSurname { get; set; }
        public string StudentFullName => $"{StudentLastName} {StudentName} {StudentSurname}".Trim();
        public List<MarkGroupSummaryStatement> Marks { get; set; }
    }
}
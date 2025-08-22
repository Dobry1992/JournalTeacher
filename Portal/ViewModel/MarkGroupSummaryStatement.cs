using System;

namespace Portal.ViewModel
{
    public class MarkGroupSummaryStatement
    {
        public string Value { get; set; }
        public DateTime Date { get; set; }
        public int? SubjectID { get; set; }
        public string? SubjectName { get; set; }
        public string? ShortSubjectName { get; set; }
        public int TypeID { get; set; }
        public string TypeName { get; set; }
        public string ShortTypeName { get; set; }
    }
}
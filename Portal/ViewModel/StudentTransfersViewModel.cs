using Portal.Models;
using System.Collections.Generic;
using System.Linq;

namespace Portal.ViewModel
{
    public class StudentTransfersViewModel
    {
        public Student Student { get; set; }

        public List<StudentTransferView> Transfers { get; set; } = new List<StudentTransferView>();

        public int LessonsCount => Transfers.Sum(t => t.Lessons.Count);

        public int SubjectsCount => Transfers
            .SelectMany(t => t.Lessons)
            .Select(l => l.SubjectName)
            .Distinct()
            .Count();
    }
}

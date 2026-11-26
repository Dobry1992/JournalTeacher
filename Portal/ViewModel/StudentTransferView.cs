using System;
using System.Collections.Generic;
using System.Linq;

namespace Portal.ViewModel
{
    public class StudentTransferView
    {
        public DateTime Date { get; set; }

        public string OldGroupName { get; set; }

        public string NewGroupName { get; set; }

        public string Comment { get; set; }

        public List<StudentTransferLessonView> Lessons { get; set; } = new List<StudentTransferLessonView>();

        public int SubjectsCount => Lessons.Select(l => l.SubjectName).Distinct().Count();

        // Сводка по пройденным предметам: невыставленные (пустые) отметки не выводятся
        public List<StudentTransferSubjectView> Subjects => Lessons
            .GroupBy(l => l.SubjectName)
            .Select(g =>
            {
                var passedMarks = g
                    .Select(l => l.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                return new StudentTransferSubjectView
                {
                    SubjectName = g.Key,
                    LessonsCount = g.Count(),
                    Marks = passedMarks.Any() ? string.Join(", ", passedMarks) : "—"
                };
            })
            .OrderBy(s => s.SubjectName)
            .ToList();
    }
}

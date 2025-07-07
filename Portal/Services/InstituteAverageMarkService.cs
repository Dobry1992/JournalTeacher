using Portal.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Services
{
    public class InstituteAverageMarkService
    {
        public Task<double> GetInstituteAverageMarkAsync(List<Mark>? marks)
        {
            return Task.Run(() =>
            {
                if (marks == null || marks.Count == 0)
                    return 0.000;

                var studentSubjectAverages = marks
                    .GroupBy(m => new { m.StudentID, m.SubjectID })
                    .Select(g =>
                    {
                        var validValues = g
                            .Select(m => TryParseMark(m.Value))
                            .Where(v => v.HasValue)
                            .Select(v => v.Value)
                            .ToList();

                        double subjectAvg = validValues.Any() ? validValues.Average() : 0.000;

                        return new
                        {
                            g.Key.StudentID,
                            SubjectAverage = subjectAvg
                        };
                    })
                    .GroupBy(x => x.StudentID)
                    .Select(g => new
                    {
                        StudentID = g.Key,
                        StudentAverage = g.Select(x => x.SubjectAverage).Average()
                    })
                    .ToList();

                var studentGroups = marks
                    .Select(m => new { m.StudentID, m.GroupID })
                    .Distinct()
                    .ToList();

                var groupAverages = studentGroups
                   .GroupJoin(
                       studentSubjectAverages,
                       sg => sg.StudentID,
                       sa => sa.StudentID,
                       (sg, saGroup) => new { sg.GroupID, saGroup }
                   )
                   .GroupBy(x => x.GroupID)
                   .Select(g =>
                   {
                       var studentAvgs = g.SelectMany(x => x.saGroup.Select(a => a.StudentAverage)).ToList();
                       return studentAvgs.Any() ? studentAvgs.Average() : 0.000;
                   })
                   .ToList();

                return groupAverages.Any() ? groupAverages.Average() : 0.000;
            });
        }

        private double? TryParseMark(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (double?)null;
        }
    }
}

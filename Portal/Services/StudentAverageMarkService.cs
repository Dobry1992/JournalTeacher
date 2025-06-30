using Portal.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Portal.Services
{
    public class StudentAverageMarkService
    {
        public double? GetStudentAverageMark(Student student, List<Mark>? marks)
        {
            var subjectAverages = marks
                .Where(m => m.StudentID == student.StudentID)
                .GroupBy(m => m.SubjectID)
                .Select(g =>
                {
                    var validValues = g
                        .Select(m => TryParseMarkValue(m.Value))
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .ToList();

                    return validValues.Count > 0 ? (double?)validValues.Average() : null;
                })
                .Where(avg => avg.HasValue)
                .Select(avg => avg.Value)
                .ToList();

            if (subjectAverages.Count == 0)
                return null;

            return Math.Round(subjectAverages.Average(), 1);
        }

        private double? TryParseMarkValue(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (double?)null;
        }
    }
}

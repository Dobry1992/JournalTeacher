using Portal.Data;
using System.Linq;
using System;
using Portal.Models;
using System.Collections.Generic;

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
            if (double.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return null;
        }
    }
}

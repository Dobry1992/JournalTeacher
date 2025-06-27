using Portal.Data;
using System.Linq;
using System.Threading.Tasks;
using System;
using Portal.Models;
using Portal.ViewModel.Raiting;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Portal.Services
{
    public class StudentAverageMarkService
    {
        private readonly AcademyContext _context;

        public StudentAverageMarkService(AcademyContext context)
        {
            _context = context;
        }

        public async Task<double?> GetStudentAverageMarkAsync(Student student)
        {
            // Загружаем оценки студента
            var marks = await _context.Marks
                .Where(m => m.StudentID == student.StudentID)
                .ToListAsync();

            // Группируем по предмету и рассчитываем среднее по каждому предмету
            var subjectAverages = marks
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

            return Math.Round(subjectAverages.Average(), 2);
        }

        /// <summary>
        /// Безопасно преобразует строковую оценку в double.
        /// </summary>
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

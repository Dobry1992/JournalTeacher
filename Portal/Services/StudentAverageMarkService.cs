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
            return GetStudentAverageMark(student, marks, null);
        }

        // typeIOId - id типа "Итоговая отметка". Когда задан, действует правило сессии:
        // если по предмету выставлена итоговая отметка за текущий период и новых учебных
        // отметок после неё нет, оценкой предмета считается итоговая отметка, а не среднее.
        public double? GetStudentAverageMark(Student student, List<Mark>? marks, int? typeIOId)
        {
            var subjectAverages = marks
                .Where(m => m.StudentID == student.StudentID)
                .GroupBy(m => m.SubjectID)
                .Select(g => GetSubjectAverage(g, typeIOId))
                .Where(avg => avg.HasValue)
                .Select(avg => avg.Value)
                .ToList();

            if (subjectAverages.Count == 0)
                return null;

            return Math.Round(subjectAverages.Average(), 3, MidpointRounding.AwayFromZero);
        }

        // Оценка предмета для расчёта среднего балла:
        // - итоговой нет -> среднее арифметическое числовых отметок (прежнее поведение);
        // - итоговая выставлена и после неё нет числовых отметок (период сессии) -> итоговая;
        // - после итоговой появились новые отметки (дисциплина продолжается, новый семестр)
        //   -> среднее только по отметкам нового периода.
        public double? GetSubjectAverage(IEnumerable<Mark> subjectMarks, int? typeIOId)
        {
            var list = subjectMarks as IList<Mark> ?? subjectMarks.ToList();

            var otherNumeric = list
                .Where(m => !typeIOId.HasValue || m.TypeOfExerciseID != typeIOId.Value)
                .Select(m => new { m.Date, Value = TryParseMarkValue(m.Value) })
                .Where(x => x.Value.HasValue)
                .ToList();

            if (!typeIOId.HasValue)
                return otherNumeric.Count > 0
                    ? otherNumeric.Average(x => x.Value!.Value)
                    : (double?)null;

            var final = list
                .Where(m => m.TypeOfExerciseID == typeIOId.Value)
                .Select(m => new { m.Date, Value = TryParseMarkValue(m.Value) })
                .Where(x => x.Value.HasValue)
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            if (final == null)
                return otherNumeric.Count > 0
                    ? otherNumeric.Average(x => x.Value!.Value)
                    : (double?)null;

            var newer = otherNumeric.Where(x => x.Date > final.Date).ToList();
            if (newer.Any())
                return newer.Average(x => x.Value!.Value);

            return final.Value;
        }

        private double? TryParseMarkValue(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (double?)null;
        }
    }
}

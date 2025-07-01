using Portal.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Services
{
    public class GroupRatingService
    {
        public async Task<double> CalculateGroupRatingAsync(List<Mark> marks)
        {
            if (marks == null || marks.Count == 0)
                return 0.0;

            var studentAverages = marks
                .GroupBy(m => m.StudentID)
                .Select(group =>
                {
                    var validMarks = group
                        .Select(m => TryParseMarkValue(m.Value))
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .ToList();

                    return validMarks.Any() ? validMarks.Average() : 0.0;
                })
                .ToList();

            if (studentAverages.Count == 0)
                return 0.0;

            double groupRating = studentAverages.Average();
            return await Task.FromResult(groupRating);
        }

        private double? TryParseMarkValue(string value)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                if (result >= 1.0 && result <= 10.0)
                    return result;
            }

            return null;
        }
    }
}

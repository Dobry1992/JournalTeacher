using Portal.Services.Interfaces;

namespace Portal.Services
{
    public class ShortNameParser : IShortNameParser
    {
        public int[] GetNumericParts(string shortName)
        {
            if (string.IsNullOrWhiteSpace(shortName))
                return new[] { 0 };

            shortName = shortName.Trim();

            int firstDigitIndex = shortName.IndexOfAny("0123456789".ToCharArray());
            if (firstDigitIndex == -1)
                return new[] { 0 };

            string numericPart = shortName.Substring(firstDigitIndex);

            var parts = numericPart.Split('.');

            var normalized = new int[3];

            for (int i = 0; i < 3; i++)
            {
                if (i < parts.Length && int.TryParse(parts[i], out int val))
                    normalized[i] = val;
                else
                    normalized[i] = 0;
            }

            return normalized;
        }
    }
}

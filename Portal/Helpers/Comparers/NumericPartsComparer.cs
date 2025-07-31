using System;
using System.Collections.Generic;

namespace Portal.Helpers.Comparers
{
    public class NumericPartsComparer : IComparer<int[]>
    {
        public int Compare(int[] x, int[] y)
        {
            int maxLength = Math.Max(x.Length, y.Length);

            for (int i = 0; i < maxLength; i++)
            {
                int a = i < x.Length ? x[i] : 0;
                int b = i < y.Length ? y[i] : 0;
                int cmp = a.CompareTo(b);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace ShiftSearch.Code
{
    public static class StringExtensions
    {
        public static double ParseOptionAmount(this string s)
        {
            double value = double.Parse(
                s.Substring(0, s.Length - 1), 
                NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint );

            char lastChar = s[^1];

            if (lastChar == 'K')
            {
                return value * 1000;
            }

            if (lastChar == 'M')
            {
                return value * 1e6;
            }

            // TODO Throw
            return -1;
        }
    }
}

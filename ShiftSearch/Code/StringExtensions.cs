using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Serilog;

namespace ShiftSearch.Code
{
    public static class StringExtensions
    {
        public static double ParseOptionAmount(this string s)
        {
            try
            {
                double value = double.Parse(
                    s.Substring(0, s.Length - 1),
                    NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint);

                char lastChar = s[^1];

                if (lastChar == 'K')
                {
                    return value * 1000;
                }

                if (lastChar == 'M')
                {
                    return value * 1e6;
                }

                throw new Exception($"Last character did not match K or M");
            }
            catch (Exception ex)
            {
                Log.Debug($"{nameof(ParseOptionAmount)} failed to parse string {s}. Exception message: {ex.Message}");

                return 0;
            }
        }
    }
}

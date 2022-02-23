using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSearch
{
    public class SymbolConfig
    {
        public string Symbol { get; init; }
        public string Url { get; init; }
        public List<UserThresholdConfig> UserThresholds { get; init; }
    }

    public class UserThresholdConfig
    {
        public List<string> PhoneNumbers { get; init; }
        public List<double> PutThresholds { get; init; }
        public List<double> CallThresholds { get; init; }
    }
}

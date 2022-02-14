using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSearch.ViewModels
{
    public class BlockOrdersViewModel
    {
        public BlockOrdersViewModel( string callAmount, string putAmount )
        {
            CallAmount = callAmount;
            PutAmount = putAmount;
        }

        public string CallAmount { get; set; }
        public string PutAmount { get; set; }
    }
}

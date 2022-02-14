using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plivo;
using Serilog;
using ShiftSearch.Code;
using ShiftSearch.Configs;
using ShiftSearch.ViewModels;

namespace ShiftSearch
{
    public class SymbolTracker
    {
        public SymbolTracker(SymbolConfig symbolConfig, PlivoConfig plivoConfig, string chromePath)
        {
            Symbol = symbolConfig.Symbol;
            PutTracker = new ThresholdTracker(symbolConfig.PutThresholds, $"{symbolConfig.Symbol} Puts", plivoConfig);
            CallTracker = new ThresholdTracker(symbolConfig.CallThresholds, $"{symbolConfig.Symbol} Calls", plivoConfig);
            ShiftSearchClient = new ShiftSearchClient(symbolConfig.Url, chromePath);
        }

        public string Symbol { get; init; }
        public ThresholdTracker PutTracker { get; init; }
        public ThresholdTracker CallTracker { get; init; }
        public ShiftSearchClient ShiftSearchClient { get; init; }

        public async Task UpdateAndNotify()
        {
            var success = await ShiftSearchClient.GoToPage();
            if (!success)
            {
                // TODO Log
                // TODO Exponential back off?
                return;
            }

            BlockOrdersViewModel vm = await ShiftSearchClient.RecognizeBlockOrders();

            //BlockOrdersViewModel vm = new BlockOrdersViewModel(callAmount: "1M", putAmount: "400K");
            PutTracker.UpdateAndNotify(vm.PutAmount);
            CallTracker.UpdateAndNotify(vm.CallAmount);
        }
    }

    public class ThresholdTracker
    {
        public ThresholdTracker(List<double> thresholdValues, 
                                string description, 
                                PlivoConfig plivoConfig)
        {
            _plivoConfig = plivoConfig;
            _plivoClient = new PlivoApi(plivoConfig.AuthId, plivoConfig.AuthToken);
            _description = description;
            _thresholdNotifications = thresholdValues
                .OrderByDescending(t => t)
                .ToDictionary(k => k, k => false);
        }

        private readonly PlivoApi _plivoClient;
        private readonly Dictionary<double, bool> _thresholdNotifications;
        private readonly string _description;
        private readonly PlivoConfig _plivoConfig;

        public void UpdateAndNotify(string amountStr)
        {
            double amount = amountStr.ParseOptionAmount();

            foreach (var (threshold, passedAndNotified) in _thresholdNotifications)
            {
                if (passedAndNotified)
                {
                    break;
                }

                if (amount >= threshold)
                {
                    // TODO Notify, only set to true if success
                    Notify(amountStr);
                    _thresholdNotifications[threshold] = true;
                    break;
                }
            }
        }

        public void Notify(string amountStr)
        {
            var msg = $"{_description} at {amountStr} at {DateTime.Now.ToString("hh:mm:ss")}";
            Log.Information(msg);
            Console.WriteLine(msg);
            //var response = _plivoClient.Message.Create(src: _plivoConfig.FromNumber, dst: _plivoConfig.ToNumbers, text: msg);
            //Console.WriteLine(response);
        }
    }
}

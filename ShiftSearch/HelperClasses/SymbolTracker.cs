using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plivo;
using Plivo.Resource.Call;
using Serilog;
using ShiftSearch.Code;
using ShiftSearch.Configs;
using ShiftSearch.ViewModels;

namespace ShiftSearch
{
    public class SymbolTracker
    {
        public SymbolTracker(SymbolConfig symbolConfig, PlivoConfig plivoConfig, ShiftSearchClient client)
        {
            Url = symbolConfig.Url;
            Symbol = symbolConfig.Symbol;
            ShiftSearchClient = client;

            PutTrackers = new List<ThresholdTracker>();
            CallTrackers = new List<ThresholdTracker>();
            foreach (var userThresholdConfig in symbolConfig.UserThresholds)
            {
                var putTracker = new ThresholdTracker(userThresholdConfig.PhoneNumbers, userThresholdConfig.PutThresholds, $"{symbolConfig.Symbol} Puts", plivoConfig);
                var callTracker = new ThresholdTracker(userThresholdConfig.PhoneNumbers, userThresholdConfig.CallThresholds, $"{symbolConfig.Symbol} Calls", plivoConfig);

                PutTrackers.Add(putTracker);
                CallTrackers.Add(callTracker);
            }
        }

        public List<ThresholdTracker> PutTrackers { get; init; }
        public List<ThresholdTracker> CallTrackers { get; init; }
        public string Symbol { get; init; }
        public ShiftSearchClient ShiftSearchClient { get; init; }
        public string Url { get; init; }
        public bool DoneNotifyingAllThresholds => PutTrackers.All(tracker => tracker.DoneNotifyingAllThresholds) && 
                                                  CallTrackers.All(tracker => tracker.DoneNotifyingAllThresholds);

        public async Task UpdateAndNotify()
        {
            if (DoneNotifyingAllThresholds)
            {
                Console.WriteLine($"Done notifying all thresholds for symbol {Symbol}");
                return;
            }

            var success = await ShiftSearchClient.GoToPage(Url);
            if (!success)
            {
                Log.Warning($"Couldn't navigate to page {Url} ");
                // TODO Exponential back off?
                return;
            }

            BlockOrdersViewModel vm = await ShiftSearchClient.RecognizeBlockOrders();

            //BlockOrdersViewModel vm = new BlockOrdersViewModel(callAmount: "1M", putAmount: "400K");
            foreach (var putTracker in PutTrackers)
            {
                putTracker.UpdateAndNotify(vm.PutAmount);
            }

            foreach (var callTracker in CallTrackers)
            {
                callTracker.UpdateAndNotify(vm.CallAmount);
            }
        }
    }

    public class ThresholdTracker
    {
        public ThresholdTracker(List<string> phoneNumbers,
                                List<double> thresholdValues, 
                                string description, 
                                PlivoConfig plivoConfig)
        {
            _phoneNumbers = phoneNumbers;
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
        private readonly List<string> _phoneNumbers;
        private readonly PlivoConfig _plivoConfig;

        public bool DoneNotifyingAllThresholds => _thresholdNotifications.Values.All(v => v);

        public void UpdateAndNotify(string amountStr)
        {
            if (string.IsNullOrEmpty(amountStr))
            {
                return;
            }

            double amount = amountStr.ParseOptionAmount();

            foreach (var (threshold, passedAndNotified) in _thresholdNotifications)
            {
                if (passedAndNotified)
                {
                    break;
                }

                if (amount >= threshold)
                {
                    bool success = Notify(amountStr);
                    _thresholdNotifications[threshold] = success;
                    break;
                }
            }
        }

        public bool Notify(string amountStr)
        {
            var msg = $"{_description} at {amountStr} at {DateTime.Now.ToString("hh:mm:ss")}\n\tNotifying {PhoneNumbersList()}";
            Log.Information(msg);

            if (Program.ONLY_LOG)
            {
                return true;
            }

            try
            {

                var response = _plivoClient.Message.Create(src: _plivoConfig.FromNumber, dst: _phoneNumbers,
                    text: msg);

                if (response.StatusCode == 202)
                {
                    return true;
                }

                Log.Warning($"Failed to send text notification to {PhoneNumbersList()} with msg {msg}. Response: {response}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception while notifying: {ex}");
            }

            return false;
        }

        private string PhoneNumbersList() => string.Join(",", _phoneNumbers);
    }
}

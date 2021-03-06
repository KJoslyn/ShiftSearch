using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Plivo;
using ShiftSearch.Code;
using ShiftSearch.Configs;
using ShiftSearch.ViewModels;

namespace ShiftSearch
{
    public class Worker : BackgroundService, IAsyncDisposable
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        private readonly List<SymbolTracker> _symbolTrackers;

        public Worker(
            IHostApplicationLifetime hostApplicationLifetime,
            IConfiguration configuration )
        {
            var plivoConfig = new PlivoConfig(
                authId: configuration["PlivoAuthId"],
                authToken: configuration["PlivoAuthToken"],
                fromNumber: configuration["Plivo:From"]);

            _client = new ShiftSearchClient(configuration["ChromePath"]);

            _symbolTrackers = configuration.GetSection("Symbols")
                .Get<List<SymbolConfig>>()
                .Select(config => new SymbolTracker(config, plivoConfig, _client))
                .ToList();

            _hostApplicationLifetime = hostApplicationLifetime;
        }

        private int intervalSeconds = 30;
        private readonly ShiftSearchClient _client;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan marketOpenTime = new TimeSpan(9, 30, 0);
            TimeSpan marketCloseTime = new TimeSpan(16, 0, 0);

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                if (now >= marketCloseTime && !Program.IGNORE_MARKET_HOURS)
                {
                    Log.Information("Market now closed!");
                    break;
                }
                else if (now <= marketOpenTime.Add(TimeSpan.FromMinutes(15)) && !Program.IGNORE_MARKET_HOURS)
                {
                    Log.Information("Market not open yet!");
                    // Or, wait until 9:30am
                    await Task.Delay(30 * 1000, stoppingToken);

                    continue;
                }

                try
                {
                    foreach (var symbolTracker in _symbolTrackers)
                    {
                        await symbolTracker.UpdateAndNotify();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in Worker: {ex}");
                }

                await Task.Delay(intervalSeconds*1000, stoppingToken);
            }

            _hostApplicationLifetime.StopApplication();
        }

        public override async Task StopAsync(CancellationToken token)
        {
            await base.StopAsync(token);
            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_client != null)
            {
                await _client.DisposeAsync();
            }
        }
    }
}

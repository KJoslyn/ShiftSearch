using PuppeteerSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ShiftSearch.Code;
using ShiftSearch.ViewModels;

#nullable enable

namespace ShiftSearch
{
    public class ShiftSearchClient
    {
        public ShiftSearchClient(string url, string chromePath)
        {
            Url = url;
            ChromePath = chromePath;
        }

        public string Url { get; }
        protected string ChromePath { get; }
        protected Browser? Browser { get; private set; }

        private async Task<string> GetCallAmountString()
        {
            string xPath = "//div[@class=\"---ui-lib-Grid-Grid-module__Area\"][contains(@style,'--area-name:callMain')]";
            return await GetContentOfXpathElement(xPath);
        }

        private async Task<string> GetPutAmountString()
        {
            string xPath = "//div[@class=\"---ui-lib-Grid-Grid-module__Area\"][contains(@style,'--area-name:putMain')]";
            return await GetContentOfXpathElement(xPath);
        }

        private async Task<string> GetContentOfXpathElement(string xPath)
        {
            var page = await GetPage();

            var handle = await GetElement( page, xPath);

            if (handle == null)
            {
                Log.Error( "Could not find call div!");
                // TODO Throw exception
            }

            return await page.EvaluateFunctionAsync<string>("e => e.textContent", handle);
        }

        protected async Task<ElementHandle?> GetElementWithContent(Page page, string elementType, string content)
        {
            string xPathMatch = string.Format("//{0}[contains(., '{1}')]", elementType, content);
            return await GetElement(page, xPathMatch);
        }

        public async Task<bool> GoToPage()
        {
            if (Browser == null)
            {
                Browser = await StartBrowser();
            }

            Page page = await GetPage();
            if (page.Url == Url)
            {
                return true;
            }

            try
            {
                await page.GoToAsync(Url);
                if (page.Url == Url)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        protected async Task<ElementHandle?> GetElement(Page page, string xPathMatch)
        {
            ElementHandle[] elementHandles = await page.XPathAsync(xPathMatch);

            List<ElementHandle> visibleHandles = new List<ElementHandle>();
            foreach (ElementHandle handle in elementHandles)
            {
                if (await handle.IsIntersectingViewportAsync())
                {
                    visibleHandles.Add(handle);
                }
            }
            if (visibleHandles.Count == 0)
            {
                return null;
            }
            if (visibleHandles.Count > 1)
            {
                Log.Warning(string.Format("Multiple elements found with xPath {0}", xPathMatch));
            }
            return visibleHandles[0];
        }

        protected async Task<Page> GetPage()
        {
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            Page[] pages = await Browser.PagesAsync();
            #pragma warning restore CS8602
            return pages[0];
        }

        private async Task<Browser> StartBrowser()
        {
            Log.Information("Starting headless browser");
            // This only downloads the browser version if it is has not been downloaded already
            //await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = ChromePath,
                DefaultViewport = new ViewPortOptions { Width = 2560, Height = 1440 }
            });
            return browser;
        }

        public async Task<BlockOrdersViewModel> RecognizeBlockOrders()
        {
            var callAmount = await GetCallAmountString( );
            var putAmount = await GetPutAmountString( );

            return new BlockOrdersViewModel(callAmount: callAmount, putAmount: putAmount);
        }

        private async Task TakePieScreenshot(string filePath)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync(filePath,
                new ScreenshotOptions { Clip = new PuppeteerSharp.Media.Clip { Width = 1000, Height = 1440 } });
        }

        private static string GetNextScreenshotFilepath()
        {
            int current = GetCurrentPieScreenshotNumber();
            return "C:/Users/Admin/WindowsServices/ShiftSearch/ShiftSearch/screenshots/piecharts/pie-" + current + ".png";
        }

        private static int GetCurrentPieScreenshotNumber()
        {
            Regex reg = new Regex(@"pie-(\d+).png");
            string[] files = Directory.GetFiles("C:/Users/Admin/WindowsServices/ShiftSearch/ShiftSearch/screenshots/piecharts", "*.png");
            IEnumerable<string> matches = files.Where(path => reg.IsMatch(path));
            if (matches.Count() == 0)
            {
                return 0;
            }
            IEnumerable<int> numbers = matches
                .Select(path => int.Parse(reg.Match(path).Groups[1].Value));
            return numbers.Max();
        }

    }
}

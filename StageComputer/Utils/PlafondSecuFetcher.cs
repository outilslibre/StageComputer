using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace StageComputer.Utils
{
    public class PlafondSecuFetcher
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMemoryCache memoryCache;

        public PlafondSecuFetcher(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.memoryCache = memoryCache;
        }

        public async Task<float> FetchLegalPayRateForYearAsync(int year)
        {
            if (year < 2015)
                return 24;

            return await memoryCache.GetOrCreateAsync($"phss_{year}", async (entry) =>
            {
                var client = null == httpClientFactory ? new HttpClient() : httpClientFactory.CreateClient("fetcher");

                var url = $"https://phss.outils-libre.org/phss/{year}";
                var responseMessage = await client.GetAsync(url);
                if (HttpStatusCode.NotFound == responseMessage.StatusCode)
                {
                    entry.SetSlidingExpiration(TimeSpan.FromDays(1)); //retry later
                    return await FetchLegalPayRateForYearAsync(year - 1);
                }
                
                entry.SetSlidingExpiration(TimeSpan.FromDays(90));

                var phssAmount = await responseMessage.Content.ReadAsStringAsync();
                return float.Parse(
					phssAmount,
                    CultureInfo.InvariantCulture);
            });
        }
        public async Task<float> GetLegalStagePayRateAsync(DateTime startStageDate)
        {
            return (await FetchLegalPayRateForYearAsync(startStageDate.Year)) * StageLegislation.LEGAL_PAYRATE_PLAFOND_RATIO;
        }
    }
}

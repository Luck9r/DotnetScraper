using Newtonsoft.Json.Linq;

public interface IHttpClientFactory
{
    HttpClient CreateClient();
}

public class DefaultHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
        };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:141.0) Gecko/20100101 Firefox/141.0");
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        client.DefaultRequestHeaders.Add("brand", "TNF");
        client.DefaultRequestHeaders.Add("channel", "ECOMM");
        client.DefaultRequestHeaders.Add("siteid", "TNF-US");
        client.DefaultRequestHeaders.Add("source", "ECOM15");
        client.DefaultRequestHeaders.Add("locale", "en_US");
        client.DefaultRequestHeaders.Add("region", "NORA");
        return client;
    }
}
public class Scraper
{
    private readonly HttpClient _httpClient;
    public Scraper(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public virtual async Task<string> ScrapeEndpointAsync(string url)
    {
        _httpClient.DefaultRequestHeaders.Remove("x-transaction-id");
        _httpClient.DefaultRequestHeaders.Add("x-transaction-id", Guid.NewGuid().ToString());
        try
        {
            var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error scraping {url}: {(int)response.StatusCode} {response.ReasonPhrase}");
                return string.Empty;
            }
            return body;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scraping {url}: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<JArray> ScrapeAllAsync(InputsFile inputsFile, FieldsConfig fieldsConfig)
    {
        var outputItems = new JArray();

        foreach (var input in inputsFile.Inputs)
        {
            var parsedItem = new JObject();
            foreach (var endpoint in fieldsConfig.Endpoints)
            {
                var url = endpoint.Url;

                foreach (var kvp in input)
                {
                    var placeholder = $"<{kvp.Key}>";
                    url = url.Replace(placeholder, kvp.Value?.ToString() ?? "");
                }

                var response = await ScrapeEndpointAsync(url);

                if (!string.IsNullOrEmpty(response))
                {
                    var doc = JObject.Parse(response);

                    foreach (var field in endpoint.Fields)
                    {
                        JToken? fieldValue = null as JToken;
                        try
                        {
                            if (!string.IsNullOrEmpty(field.ConstantValue))
                            {
                                fieldValue = JToken.FromObject(field.ConstantValue);
                            }
                            else if (!string.IsNullOrEmpty(field.Path))
                            {
                                var tokens = doc.SelectTokens(field.Path);
                                if (tokens.Count() == 1)
                                {
                                    fieldValue = JToken.FromObject(tokens.First());
                                }
                                else
                                {
                                    fieldValue = JToken.FromObject(tokens);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error extracting field '{field.Name}', {e}");
                        }
                        parsedItem.Add(field.Name, fieldValue);
                    }
                }
            }
            if (parsedItem.Properties().Any())
                outputItems.Add(parsedItem as JToken);
        }

        return outputItems;
    }

}
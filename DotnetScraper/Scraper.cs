using System;
using System.Net.Http;

using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json.Linq;

public class Scraper
{
    private readonly HttpClient _httpClient;
    
    public Scraper()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
        };
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:141.0) Gecko/20100101 Firefox/141.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _httpClient.DefaultRequestHeaders.Add("brand", "TNF");
        _httpClient.DefaultRequestHeaders.Add("channel", "ECOMM");
        // these will need to be adjusted if the site country is different
        _httpClient.DefaultRequestHeaders.Add("siteid", "TNF-US");
        _httpClient.DefaultRequestHeaders.Add("source", "ECOM15");
        _httpClient.DefaultRequestHeaders.Add("locale", "en_US");
        _httpClient.DefaultRequestHeaders.Add("region", "NORA");

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

        foreach (var input in inputsFile.inputs)
        {
            var parsedItem = new JObject();
            foreach (var endpoint in fieldsConfig.endpoints)
            {
                var url = endpoint.url;

                foreach (var kvp in input)
                {
                    var placeholder = $"<{kvp.Key}>";
                    url = url.Replace(placeholder, kvp.Value?.ToString() ?? "");
                }

                var response = await ScrapeEndpointAsync(url);

                if (!string.IsNullOrEmpty(response))
                {
                    var doc = JObject.Parse(response);

                    foreach (var field in endpoint.fields)
                    {
                        JToken? fieldValue = null as JToken;
                        try
                        {
                            if (!string.IsNullOrEmpty(field.constantValue))
                            {
                                fieldValue = JToken.FromObject(field.constantValue); 
                            }
                            else if (!string.IsNullOrEmpty(field.path))
                            {
                                var tokens = doc.SelectTokens(field.path);
                                if (tokens.Count() == 1) { 
                                    fieldValue = JToken.FromObject(tokens.First());
                                } else {
                                    fieldValue = JToken.FromObject(tokens);
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"Error extracting field '{field.name}', {e}");
                        } 
                        parsedItem.Add(field.name, fieldValue);   
                    }
                }
            }
            outputItems.Add(parsedItem as JToken);
        }

        return outputItems;
    }
    
}
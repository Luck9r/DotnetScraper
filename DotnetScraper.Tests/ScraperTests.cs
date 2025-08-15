using Xunit;
using Moq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class ScraperTests
{
    [Fact]
    public async Task ScrapeAllAsync_ReturnsExpectedFields()
    {
        var scraper = new Scraper(new DefaultHttpClientFactory());

        var inputsFile = new InputsFile
        {
            Inputs = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "productID", "NF0A3C8D" } }
            }
        };

        var fieldsConfig = new FieldsConfig
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "productData",
                    Url = "https://example.com/api/products/<productID>",
                    Fields = new List<FieldConfig>
                    {
                        new FieldConfig { Name = "sku", Path = "id" },
                        new FieldConfig { Name = "brand", Path = "brand" },
                        new FieldConfig { Name = "constant", ConstantValue = "test" }
                    }
                }
            }
        };

        var sampleJson = @"{ ""id"": ""NF0A3C8D"", ""brand"": ""The North Face"" }";
        var scraperMock = new Mock<Scraper>(new DefaultHttpClientFactory());
        scraperMock.CallBase = true;
        scraperMock.Setup(s => s.ScrapeEndpointAsync(It.IsAny<string>()))
            .ReturnsAsync(sampleJson);

        var result = await scraperMock.Object.ScrapeAllAsync(inputsFile, fieldsConfig);
        Assert.Single(result);
        var item = (JObject)result[0];
        Assert.Equal("NF0A3C8D", item["sku"].ToString());
        Assert.Equal("The North Face", item["brand"].ToString());
        Assert.Equal("test", item["constant"].ToString());
    }

    [Fact]
    public async Task ScrapeAllAsync_HandlesEmptyResponse()
    {
        var scraperMock = new Mock<Scraper>(new DefaultHttpClientFactory());
        scraperMock.CallBase = true;
        scraperMock.Setup(s => s.ScrapeEndpointAsync(It.IsAny<string>()))
            .ReturnsAsync("");

        var inputsFile = new InputsFile
        {
            Inputs = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            }
        };

        var fieldsConfig = new FieldsConfig
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "productData",
                    Url = "https://example.com",
                    Fields = new List<FieldConfig>
                    {
                        new FieldConfig { Name = "sku", Path = "id" }
                    }
                }
            }
        };
        var result = await scraperMock.Object.ScrapeAllAsync(inputsFile, fieldsConfig);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ScrapeAllAsync_HandlesJsonParseError()
    {
        var scraperMock = new Mock<Scraper>(new DefaultHttpClientFactory());
        scraperMock.CallBase = true;

        scraperMock.Setup(s => s.ScrapeEndpointAsync(It.IsAny<string>()))
            .ReturnsAsync("{ this is not valid JSON }");

        var inputsFile = new InputsFile
        {
            Inputs = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            }
        };

        var fieldsConfig = new FieldsConfig
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "productData",
                    Url = "https://example.com",
                    Fields = new List<FieldConfig>
                    {
                        new FieldConfig { Name = "sku", Path = "id" }
                    }
                }
            }
        };

        await Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(async () =>
        {
            await scraperMock.Object.ScrapeAllAsync(inputsFile, fieldsConfig);
        });
    }
}
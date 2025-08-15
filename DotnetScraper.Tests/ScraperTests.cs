using Xunit;
using Moq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class ScraperTests
{
    [Fact]
    public async Task ScrapeAllAsync_ReturnsExpectedFields()
    {
        var scraper = new Scraper();

        var inputsFile = new InputsFile
        {
            inputs = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "productID", "NF0A3C8D" } }
            }
        };

        var fieldsConfig = new FieldsConfig
        {
            endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    name = "productData",
                    url = "https://example.com/api/products/<productID>",
                    fields = new List<FieldConfig>
                    {
                        new FieldConfig { name = "sku", path = "id" },
                        new FieldConfig { name = "brand", path = "brand" },
                        new FieldConfig { name = "constant", constantValue = "test" }
                    }
                }
            }
        };

        var sampleJson = @"{ ""id"": ""NF0A3C8D"", ""brand"": ""The North Face"" }";
        var scraperMock = new Mock<Scraper>();
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
        var scraperMock = new Mock<Scraper>();
        scraperMock.CallBase = true;
        scraperMock.Setup(s => s.ScrapeEndpointAsync(It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        var inputsFile = new InputsFile
        {
            inputs = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            }
        };

        var fieldsConfig = new FieldsConfig
        {
            endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    name = "productData",
                    url = "https://example.com",
                    fields = new List<FieldConfig>
                    {
                        new FieldConfig { name = "sku", path = "id" }
                    }
                }
            }
        };

        var result = await scraperMock.Object.ScrapeAllAsync(inputsFile, fieldsConfig);
        Assert.Single(result);
        Assert.Empty(((JObject)result[0]).Properties());
    }

    [Fact]
    public async Task ScrapeAllAsync_HandlesJsonParseError()
    {
        var scraperMock = new Mock<Scraper>();
        scraperMock.CallBase = true;
        scraperMock.Setup(s => s.ScrapeEndpointAsync(It.IsAny<string>()))
            .ReturnsAsync("not a json");

        var inputsFile = new InputsFile
        {
            inputs = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            }
        };

        var fieldsConfig = new FieldsConfig
        {
            endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    name = "productData",
                    url = "https://example.com",
                    fields = new List<FieldConfig>
                    {
                        new FieldConfig { name = "sku", path = "id" }
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
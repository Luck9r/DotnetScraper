using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class InputsFile
{
    [YamlMember(Alias = "inputs")]
    public required List<Dictionary<string, object>> Inputs { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        var fieldsYaml = File.ReadAllText("fields.yaml");
        if (string.IsNullOrEmpty(fieldsYaml))
        {
            Console.WriteLine("No fields found in fields.yaml");
            return;
        }
        var fieldsConfig = LoadYamlConfig<FieldsConfig>("fields.yaml");
        var inputsFile = LoadYamlConfig<InputsFile>("inputs.yaml");

        var scraper = new Scraper(new DefaultHttpClientFactory());

        var outputItems = await scraper.ScrapeAllAsync(inputsFile, fieldsConfig);
        if (outputItems.Count == 0)
        {
            Console.WriteLine("No data scraped.");
            return;
        }


        var transform = new Transform();
        var transformedItems = transform.TransformData(outputItems);
        var json = transformedItems.ToString();
        File.WriteAllText("output.json", json);
    }

    private static T LoadYamlConfig<T>(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        if (string.IsNullOrEmpty(yaml))
            throw new FileNotFoundException($"No content found in {filePath}");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<T>(yaml);
    }
}
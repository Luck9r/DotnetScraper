using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class InputsFile
{
    public required List<Dictionary<string, object>> inputs { get; set; }
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
        var yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var fieldsConfig = yamlDeserializer.Deserialize<FieldsConfig>(fieldsYaml);

        var inputsYAML = File.ReadAllText("inputs.yaml");
        if (string.IsNullOrEmpty(inputsYAML))
        {
            Console.WriteLine("No inputs found in inputs.yaml");
            return;
        }
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var inputsFile = deserializer.Deserialize<InputsFile>(inputsYAML);

        var scraper = new Scraper();

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
}
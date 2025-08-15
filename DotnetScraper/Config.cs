using YamlDotNet.Serialization;

public class FieldsConfig
{
    [YamlMember(Alias = "endpoints")]
    public required List<EndpointConfig> Endpoints { get; set; }
}

public class EndpointConfig
{
    [YamlMember(Alias = "name")]
    public required string Name { get; set; }
    [YamlMember(Alias = "url")]
    public required string Url { get; set; }
    [YamlMember(Alias = "fields")]
    public required List<FieldConfig> Fields { get; set; }
}

public class FieldConfig
{
    [YamlMember(Alias = "name")]
    public required string Name { get; set; }
    [YamlMember(Alias = "path")]
    public string? Path { get; set; }
    [YamlMember(Alias = "constantValue")]
    public string? ConstantValue { get; set; }
}
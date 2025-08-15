using System.Collections.Generic;

public class FieldsConfig
{
    public required List<EndpointConfig> endpoints { get; set; }
}

public class EndpointConfig
{
    public required string name { get; set; }
    public required string url { get; set; }
    public required List<FieldConfig> fields { get; set; }
}

public class FieldConfig
{
    public required string name { get; set; }
    public string? path { get; set; }
    public string? constantValue { get; set; }
}
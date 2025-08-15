using Xunit;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class TransformTests
{
    [Fact]
    public void FlattenAttributes_Works()
    {
        var input = new JArray
        {
            new JObject
            {
                ["attributes"] = new JArray
                {
                    new JObject
                    {
                        ["label"] = "Color",
                        ["options"] = new JArray
                        {
                            new JObject { ["label"] = "Red", ["value"] = "R" },
                            new JObject { ["label"] = "Blue", ["value"] = "B" }
                        }
                    }
                }
            }
        };

        var transform = new Transform();
        var result = transform.TransformData(input);
        var attributes = result[0]["attributes"] as JObject;
        if (attributes == null)
        {
            Assert.Fail("Attributes should not be null");
            return;
        }
        Assert.Equal("Red, Blue", attributes!["Colors"].ToString());
    }

    [Fact]
    public void CategoryExtraction_Works()
    {
        var input = new JArray
        {
            new JObject
            {
                ["breadcrumbs"] = new JArray { "Men", "Jackets", "Insulated" }
            }
        };

        var transform = new Transform();
        var result = transform.TransformData(input);
        Assert.Equal("Home>Men>Jackets>Insulated", result[0]["category"].ToString());
        Assert.Equal("Home", result[0]["categoryLvl1"].ToString());
        Assert.Equal("Men", result[0]["categoryLvl2"].ToString());
        Assert.Equal("Jackets", result[0]["categoryLvl3"].ToString());
        Assert.Equal("Insulated", result[0]["categoryLvl4"].ToString());
    }

    [Fact]
    public void BuildSKU_Works()
    {
        var variant = new JObject
        {
            ["id"] = "NF:0A7WHK:JK3:L::1:",
            ["attributes"] = new JObject
            {
                ["color"] = "JK3",
                ["size"] = "L"
            }
        };
        var transform = new Transform();
        var sku = typeof(Transform).GetMethod("BuildSKU", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(transform, new object[] { variant }) as string;
        Assert.Contains("color=JK3", sku);
        Assert.Contains("size=L", sku);
    }

    [Fact]
    public void StarRatingDistribution_Works()
    {
        var input = new JArray
        {
            new JObject
            {
                ["starRatingDistribution"] = new JArray { 50, 23, 44, 186, 2489 },
                ["numberOfCustomerReviews"] = 2792
            }
        };

        var transform = new Transform();
        var result = transform.TransformData(input);
        var stars = result[0]["starRatingDistribution"] as JObject;
        Assert.Equal("50", stars["1 Star"]["number"].ToString());
        Assert.Equal("1.8", stars["1 Star"]["percentage"].ToString());
        Assert.Equal("2489", stars["5 Star"]["number"].ToString());
        Assert.Equal("89.1", stars["5 Star"]["percentage"].ToString());
    }

    [Fact]
    public void HandlesMissingAttributes()
    {
        var input = new JArray { new JObject() };
        var transform = new Transform();
        var result = transform.TransformData(input);
        Assert.NotNull(result[0]);
        Assert.Null(result[0]["attributes"]);
    }

    [Fact]
    public void HandlesEmptyBreadcrumbs()
    {
        var input = new JArray { new JObject { ["breadcrumbs"] = new JArray() } };
        var transform = new Transform();
        var result = transform.TransformData(input);
        Assert.Equal("Home", result[0]["category"]!.ToString());
        Assert.Equal("Home", result[0]["categoryLvl1"]!.ToString());
    }

    [Fact]
    public void HandlesNullStarRatingDistribution()
    {
        var input = new JArray { new JObject { ["numberOfCustomerReviews"] = 100 } };
        var transform = new Transform();
        var result = transform.TransformData(input);
        Assert.Null(result[0]["starRatingDistribution"]);
    }

    [Fact]
    public void VariantAttributesMapping_Works()
    {
        var input = new JArray
        {
            new JObject
            {
                ["sku"] = "NF0A3C8D",
                ["variantsData"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "NF0A3C8D:JK3:L::1:",
                        ["attributes"] = new JObject
                        {
                            ["color"] = "JK3",
                            ["size"] = "L"
                        }
                    }
                },
                ["attributes"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "color",
                        ["options"] = new JArray
                        {
                            new JObject { ["value"] = "JK3", ["label"] = "Black" }
                        }
                    },
                    new JObject
                    {
                        ["type"] = "size",
                        ["options"] = new JArray
                        {
                            new JObject { ["value"] = "L", ["label"] = "L" }
                        }
                    }
                }
            }
        };

        var transform = new Transform();
        var result = transform.TransformData(input);
        var variantAttributes = result[0]["variantAttributes"] as JObject;
        Assert.NotNull(variantAttributes);
        Assert.Equal("Black", variantAttributes["Color"].ToString());
        Assert.Equal("L", variantAttributes["Size"].ToString());
    }

    [Fact]
    public void VariantWithMissingAttributeValue_Works()
    {
        var input = new JArray
        {
            new JObject
            {
                ["sku"] = "NF0A3C8D",
                ["variantsData"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "NF0A3C8D:JK3::1:",
                        ["attributes"] = new JObject
                        {
                            ["color"] = "JK3",
                            ["size"] = null
                        }
                    }
                },
                ["attributes"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "color",
                        ["options"] = new JArray
                        {
                            new JObject { ["value"] = "JK3", ["label"] = "Black" }
                        }
                    },
                    new JObject
                    {
                        ["type"] = "size",
                        ["options"] = new JArray
                        {
                            new JObject { ["value"] = "L", ["label"] = "Large" }
                        }
                    }
                }
            }
        };

        var transform = new Transform();
        var result = transform.TransformData(input);
        var variantAttributes = result[0]["variantAttributes"] as JObject;
        Assert.NotNull(variantAttributes);
        Assert.Equal("Black", variantAttributes["Color"].ToString());
        Assert.False(variantAttributes.ContainsKey("Size"));
    }

}
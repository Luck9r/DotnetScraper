using System;
using Newtonsoft.Json.Linq;

public class Transform
{
    public JArray TransformData(JArray inputData)
    {
        var transformedData = new JArray();

        foreach (JObject item in inputData)
        {
            if (item["variantsData"] != null && item["variantsData"]!.Type == JTokenType.Object)
            {
                var singleVariant = item["variantsData"] as JObject;
                item["variantsData"] = new JArray(singleVariant!);
            }
            
            var variantsExist = item["variantsData"] != null && item["variantsData"]!.Type == JTokenType.Array;

            var attributeLabelLookup = new Dictionary<string, Dictionary<string, string>>();
            if (item["attributes"] != null && item["attributes"]!.Type == JTokenType.Array && variantsExist)
            {
                
                foreach (var attr in item["attributes"]!)
                {
                    var type = attr["type"]?.ToString();
                    if (type != null && attr["options"] != null && attr["options"]!.Type == JTokenType.Array)
                    {
                        var valueToLabel = new Dictionary<string, string>();
                        foreach (var opt in attr["options"]!)
                        {
                            var value = opt["value"]?.ToString();
                            var label = opt["label"]?.ToString();
                            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(label))
                                valueToLabel[value] = label;
                        }
                        attributeLabelLookup[type] = valueToLabel;
                    }
                }
            }

            var transformedItem = TransformItem(item);

            if (variantsExist)
            {
                var variantsAttributesArray = new JArray();

                foreach (JObject variant in item["variantsData"]!)
                {
                    var fullSku = item["sku"] + BuildSKU(variant);

                    var attributesDict = new JObject();
                    if (variant["attributes"] != null && variant["attributes"]!.Type == JTokenType.Object)
                    {
                        foreach (var prop in variant["attributes"]!)
                        {
                            var key = ((JProperty)prop).Name;
                            var tokenValue = ((JProperty)prop).Value;
                            var value = tokenValue.Type == JTokenType.Null ? null : tokenValue.ToString();

                            if (string.IsNullOrEmpty(value))
                                continue;

                            string prettyLabel = value;
                            if (attributeLabelLookup.ContainsKey(key) && value != null)
                            {
                                prettyLabel = attributeLabelLookup[key].ContainsKey(value)
                                    ? attributeLabelLookup[key][value]
                                    : value;
                            }
                            var outKey = char.ToUpper(key[0]) + key.Substring(1);
                            attributesDict[outKey] = prettyLabel;
                        }
                    }
                    variantsAttributesArray.Add(new JObject { [fullSku] = attributesDict });
                }
                transformedItem["variantsAttributes"] = variantsAttributesArray;

                if (transformedItem["sku"] != null)
                {
                    foreach (JObject variant in transformedItem["variantsData"]!)
                    {
                        variant["id"] = transformedItem["sku"]!.ToString() + BuildSKU(variant);
                        transformedData.Add(TransformVariant(variant, transformedItem, attributeLabelLookup));
                    }
                }
                
            }
            else
            {
                transformedData.Add(transformedItem);
            }
        }
        
        return transformedData;
    }

    private JObject TransformVariant(JObject variant, JObject item, Dictionary<string, Dictionary<string, string>> attributeLabelLookup)
    {
        var itemCopy = (JObject)item.DeepClone();

        if (variant["attributes"] != null && variant["attributes"]!.Type == JTokenType.Object)
        {
            var attributesDict = new JObject();
            foreach (var prop in variant["attributes"]!)
            {
                var key = ((JProperty)prop).Name;
                var tokenValue = ((JProperty)prop).Value;
                var value = tokenValue.Type == JTokenType.Null ? null : tokenValue.ToString();

                if (string.IsNullOrEmpty(value))
                    continue;

                string prettyLabel = value;
                if (attributeLabelLookup.ContainsKey(key) && value != null)
                {
                    prettyLabel = attributeLabelLookup[key].ContainsKey(value)
                        ? attributeLabelLookup[key][value]
                        : value;
                }
                var outKey = char.ToUpper(key[0]) + key.Substring(1);
                attributesDict[outKey] = prettyLabel;
            }
            itemCopy["variantAttributes"] = attributesDict;

            itemCopy["availability"] = variant["productInventoryState"]?.ToString() ?? null;
            itemCopy["price"] = (float?)variant["price"]?["current"] ?? null;
            if (itemCopy["mpn"] != null && itemCopy["variantsData"]!.Count() != 1)
            {
                var colorCode = variant["attributes"]?["color"]?.ToString();
                if (!string.IsNullOrEmpty(colorCode))
                {
                    itemCopy["mpn"] = itemCopy["mpn"]!.ToString() + colorCode;
                }
            }

        }



        itemCopy.Remove("variantsData");

        itemCopy["sku"] = variant["id"];

        return itemCopy;
    }
      
    private JObject TransformItem(JObject item)
    {
        var itemCopy = (JObject)item.DeepClone();

        itemCopy["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd")+ "T00:00:00";

        itemCopy["time"] = DateTime.UtcNow.ToString("HH:mm:ss.fffffff");

        if (itemCopy["breadcrumbs"] != null && itemCopy["breadcrumbs"]!.Type == JTokenType.Array)
        {
            var breadcrumbs = itemCopy["breadcrumbs"]!.ToObject<string[]>();

            var newBreadcrumbs = new List<string> { "Home" };
            newBreadcrumbs.AddRange(breadcrumbs!);

            if (newBreadcrumbs != null && newBreadcrumbs.Count > 0)
            {
                itemCopy["category"] = string.Join(">", newBreadcrumbs);

                for (int i=1; i<=newBreadcrumbs.Count; i++)
                {
                    itemCopy[$"categoryLvl{i}"] = newBreadcrumbs[i-1];
                }
            }
            itemCopy.Remove("breadcrumbs");
        }
        
        if (itemCopy["url"] != null && itemCopy["rootDomain"] != null)
        {
            var path = itemCopy["url"]!.ToString();
            var domain = itemCopy["rootDomain"]!.ToString();

            var skuMatch = System.Text.RegularExpressions.Regex.Match(path, @"(NF[0-9A-Z]+)");
            var sku = skuMatch.Success ? skuMatch.Value : "";
            
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(domain))
            {
                itemCopy["url"] = $"https://www.{domain}/en-us/p/-{sku}";
            }
        }

        if (itemCopy["attributes"] != null && itemCopy["attributes"]!.Type == JTokenType.Array)
        {
            var flatAttributes = new JObject();
            foreach (var attr in itemCopy["attributes"]!)
            {
                var label = attr["label"]?.ToString() + "s";
                string? value = null;

                if (attr["options"] != null && attr["options"]!.Type == JTokenType.Array)
                {
                    var optionLabels = attr["options"]!.Select(opt => opt["label"]?.ToString()).Where(l => !string.IsNullOrEmpty(l));
                    value = string.Join(", ", optionLabels);
                }

                if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(value))
                    flatAttributes[label] = value;
            }
            itemCopy["attributes"] = flatAttributes;

            if (itemCopy["productDetails"] != null && itemCopy["productDetails"]!.Type == JTokenType.Array)
            {
                var productDetails = itemCopy["productDetails"]!.ToObject<JArray>();
                foreach (var detail in productDetails!)
                {
                    if (detail["label"] != null && detail["text"] != null)
                    {
                        var label = detail["label"]!.ToString();
                        var text = detail["text"]!.ToString();
                        if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(text))
                        {
                            itemCopy["attributes"]![label] = text;
                        }
                    }
                }
                itemCopy.Remove("productDetails");
            }
        }

        var variants = new JArray();

        if (itemCopy["variantsData"] != null && itemCopy["variantsData"]!.Type == JTokenType.Array)
        {
            foreach (var variant in itemCopy["variantsData"]!)
            {
                variants.Add(item["sku"] + BuildSKU((JObject)variant)); 
            }
        }

        itemCopy["variants"] = variants;

        if (itemCopy["features"] != null)
        {
            itemCopy["features"] = itemCopy["features"]!.ToString().Replace("\n", "").Replace("\"", "'").Trim();
        }
        if (itemCopy["description"] != null)
        {
            itemCopy["description"] = itemCopy["description"]!.ToString().Replace("\n", "").Replace("\"", "'").Trim();
        }

        if(itemCopy["starRatingDistribution"] != null && itemCopy["starRatingDistribution"]!.Type == JTokenType.Array)
        {
            var distribution = itemCopy["starRatingDistribution"] as JArray;
            var total = itemCopy["numberOfCustomerReviews"]?.ToObject<int>() ?? 0;
            var transformedStars = new JObject();
            var starLabels = new[] { "1 Star", "2 Star", "3 Star", "4 Star", "5 Star" };

            for (int i = 0; i < distribution!.Count && i < starLabels.Length; i++)
            {
                var number = distribution[i].ToObject<int>();
                var percentage = total > 0 ? Math.Round((number / (double)total) * 100, 1) : 0;

                transformedStars[starLabels[i]] = new JObject
                {
                    ["number"] = number,
                    ["percentage"] = percentage
                };
            }

            itemCopy["starRatingDistribution"] = transformedStars;
        }

        return itemCopy;

    }

    private string BuildSKU(JObject variant)
    {
        var id = variant["id"]?.ToString();
        var baseSkuMatch = System.Text.RegularExpressions.Regex.Match(id ?? "", @"NF:[0-9A-Z]+");
        var baseSku = baseSkuMatch.Success ? baseSkuMatch.Value : "";

        var attributeParts = new List<string>();
        if (variant["attributes"] != null && variant["attributes"]!.Type == JTokenType.Object)
        {
            foreach (var prop in variant["attributes"]!)
            {
                var key = ((JProperty)prop).Name;
                var tokenValue = ((JProperty)prop).Value;
                var value = tokenValue.Type == JTokenType.Null ? null : tokenValue.ToString();

                if (string.IsNullOrEmpty(value))
                    continue;

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    attributeParts.Add($"{key}={value}");
                }
            }
        }

        return "$" + string.Join("&", attributeParts);
    }
}
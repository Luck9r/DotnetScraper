# DotnetScraper

A versatile web scraper built with .NET.

## Requirements

- .NET 9.0 SDK

## Installation

```bash
dotnet restore
```

## Running

```bash
dotnet run --project DotnetScraper/DotnetScraper.csproj
```

## Running Tests

```bash
dotnet test DotnetScraper.Tests
```

## Configuration
The scraper uses an `inputs.yaml` file to define the items to scrape.

```yaml
inputs:
  - productID: 'NF0A3C8D'
    domain: 'www.example.com'
  - productID: 'NF0A8CGSQZI'
    domain: 'www.example.com'
```

The fields are defined for each endpoint in `fields.yaml`. Each field contains a name and a JSONPath to the data in the response. A constant value can be set instead. Here is an example:

```yaml
endpoints:
  - name: 'productDetails'
    # Input parameters are used to construct the URL
    url: 'https://<domain>/api/products/<productID>'
    fields:
      - name: 'sku'
        path: 'id'
      - name: 'rootDomain'
        constantValue: 'example.com'
  - name: 'productReviews'
    url: 'https://<domain>/api/products/<productID>/reviews'
    fields:
      - name: 'reviewId'
        path: 'id'
      - name: 'reviewText'
        path: 'text'
```
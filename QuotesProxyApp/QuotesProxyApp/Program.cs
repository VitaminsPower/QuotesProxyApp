using System.Text.RegularExpressions;
using HtmlAgilityPack;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string baseUri = "https://quotes.toscrape.com";

app.Map("{**everything}", async ctx =>
{
    var upstreamUrl = $"{baseUri}{ctx.Request.Path}{ctx.Request.QueryString}";
    
    using var http = new HttpClient();
    var upstreamResp = await http.GetAsync(upstreamUrl);
    var contentType = upstreamResp.Content.Headers.ContentType?.ToString() ?? "text/html";
    var bytes = await upstreamResp.Content.ReadAsByteArrayAsync();
    
    if (!contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.ContentType = contentType;
        await ctx.Response.Body.WriteAsync(bytes);
        return;
    }
    
    var html = System.Text.Encoding.UTF8.GetString(bytes);
    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    var sixLetter = new Regex(@"\b([A-Za-z]{6})\b");
    var textNodes = doc.DocumentNode.SelectNodes("//text()[normalize-space(.)!='']") 
                    ?? Enumerable.Empty<HtmlNode>();
    foreach (var node in textNodes)
    {
        var rawText = node.InnerText;
        node.InnerHtml = sixLetter.Replace(rawText, "$1&trade;");
    }

    var attrs = new[] { "href", "src" };
    foreach (var attr in attrs)
    {
        var nodes = doc.DocumentNode.SelectNodes($"//*[@{attr}]") 
                    ?? Enumerable.Empty<HtmlNode>();
        foreach (var n in nodes)
        {
            var url = n.GetAttributeValue(attr, "");
            if (string.IsNullOrEmpty(url))
                continue;

            if (Uri.TryCreate(url, UriKind.Absolute, out var abs) &&
                abs.Host == new Uri(baseUri).Host)
            {
                n.SetAttributeValue(attr, abs.PathAndQuery);
            }
            else if (Uri.TryCreate(url, UriKind.Relative, out _))
            {
                n.SetAttributeValue(attr, url);
            }
        }
    }

    ctx.Response.ContentType = contentType;
    await ctx.Response.WriteAsync(doc.DocumentNode.OuterHtml);
});

app.Run();

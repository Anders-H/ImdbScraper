﻿using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace ImdbScraper;

public class Scraper
{
    private const string JsonStartTag = "<script type=\"application/ld+json\">";

    public DownloadResult Download(uint imdbId)
    {
        var url = $@"https://www.imdb.com/title/tt{imdbId:0000000}/";

        try
        {
            using var client = new WebClient();
            client.Encoding = Encoding.UTF8;
            var html = client.DownloadString(url);
            var infrastructureSuccess = true;
            
            var grabSuccess = !string.IsNullOrEmpty(html)
                && html.Length > 10000
                && html.IndexOf(JsonStartTag, StringComparison.Ordinal) > 0;

            return new DownloadResult(infrastructureSuccess, grabSuccess, html);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public ScrapeResult Scrape(string html)
    {
        var startIndex = html.IndexOf(JsonStartTag, StringComparison.Ordinal);

        html = html[(startIndex + JsonStartTag.Length)..];

        var endIndex = html.IndexOf("</script>", StringComparison.Ordinal);

        html = html.Substring(0, endIndex);

        var data = JObject.Parse(html);

        if (!data.TryGetValue("name", StringComparison.InvariantCultureIgnoreCase, out var name))
            return ScrapeResult.Fail();

        var regionalTitle = name.Value<string>() ?? "";
        var originalTitle = "";

        if (data.TryGetValue("alternateName", StringComparison.InvariantCultureIgnoreCase, out var alternateName))
            originalTitle = alternateName.Value<string>() ?? "";

        short year = 0;

        if (data.TryGetValue("datePublished", StringComparison.CurrentCultureIgnoreCase, out var datePublished))
            year = (short)datePublished.Value<DateTime>().Year;

        float? rating = null;

        try
        {
            var aggregateRating = data["aggregateRating"];
            var ratingValue = (float?)aggregateRating?["ratingValue"] ?? null;

            if (ratingValue != null)
                rating = ratingValue.Value;
        }
        catch
        {
            // ignored
        }

        var url = "";

        if (data.TryGetValue("url", StringComparison.CurrentCultureIgnoreCase, out var tempUrl) && !string.IsNullOrWhiteSpace(tempUrl.Value<string>()))
        {
            url = tempUrl.Value<string>();
        }

        return new ScrapeResult(true, regionalTitle, originalTitle, year)
        {
            ScrapeDate = DateTime.Now,
            Rating = rating,
            Url = url
        };
    }
}
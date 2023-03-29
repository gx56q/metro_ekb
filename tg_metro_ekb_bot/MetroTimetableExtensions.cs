using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace tg_metro_ekb_bot;

public static class MetroTimetableExtensions
{
    private const string Encoding = "utf-8";
    private const string StationsUrl = "https://metro-ektb.ru/podrobnye-grafiki-po-stanciyam/";
    private const string TimesXPath = "/html/body/div/div[1]/div[2]/div[2]/div/div[2]/div/div/div/ul";
    private const string HeadersXPath = "/html/body/div/div[1]/div[2]/div[2]/div/div[2]/div/div/div/p";
    
    private static string GetWebsite(string url)
    {
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var win1251 = System.Text.Encoding.GetEncoding(Encoding);
#pragma warning disable SYSLIB0014
        var client = new System.Net.WebClient();
#pragma warning restore SYSLIB0014
        client.Encoding = win1251;
        return client.DownloadString(url);
    }

    private static Station GetStationFromHtml(string html, string stationName)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var times = doc.DocumentNode.SelectNodes(TimesXPath);
        var header = doc.DocumentNode.SelectNodes(HeadersXPath);
        if (times == null || header == null) return new Station();
        var timetable1 = times.Select((x, i) => new { x, i })
            .ToDictionary(x => header[x.i]
                    .InnerText,
                x => x.x.ChildNodes.Where(y => y.Name == "li")
                    .Select(y => y.InnerText)
                    .Aggregate((a,
                        b) => a + ", " + b));
        var regex = new Regex(@"(\d{2}[:;.,]\d{2})");
        var timetable2 = timetable1.ToDictionary(x => x.Key,
            x => x.Value.Replace(" ",
                    "")
                .Replace("&nbsp",
                    "")
                .Replace("&amp",
                    "")
                .Replace("&quot",
                    "")
                .Replace("&lt",
                    "")
                .Replace("&gt",
                    "")
                .Replace("\r",
                    "")
                .Replace("\n",
                    "")
                .Replace(",",
                    "")
                .Replace(';', ':'));
        var timetable = timetable2.ToDictionary(x => x.Key, x => regex.Matches(x.Value).Select(y => y.Value).ToArray());
        var station = new Station(stationName);
        foreach (var (key, value) in timetable)
        {
            var dateType = key.Contains("Рабочие дни")
                ? TimetableExtensions.DayType.Weekday
                : TimetableExtensions.DayType.Weekend;
            var wayType = key.Contains("Ботаническая")
                ? TimetableExtensions.WayType.Down
                : TimetableExtensions.WayType.Up;
            station.AddTime(value, dateType, wayType);
        }
        return station;
    }

    private static Dictionary<string, string> GetStationsLinks(string html)
    {
        var stationLinks = new Dictionary<string, string>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var nodes = doc.DocumentNode.SelectNodes("/html/body/div/div[1]/div[2]/div[2]/div/div[2]/div/div/div/ul/li");
        if (nodes == null) return stationLinks;
        foreach (var node in nodes)
        {
            var link = node.ChildNodes[0].Attributes["href"].Value;
            var processedLink = link.EndsWith('/') ? link : link + '/';
            if (stationLinks.ContainsValue(processedLink)) continue;
            stationLinks.Add(node.InnerText, processedLink);
        }
        return stationLinks;
    }
    
    public static Station[] GetStations()
    {
        var metroTimetable = new List<Station>();
        var stationsWebsite = GetWebsite(StationsUrl);
        var stationsLinks = GetStationsLinks(stationsWebsite);
        foreach (var stationLink in stationsLinks)
        {
            var stationHtml = GetWebsite(stationLink.Value);
            var station = GetStationFromHtml(stationHtml, stationLink.Key);
            metroTimetable.Add(station);
        }
        return metroTimetable.ToArray();
    }
    
    public static List<string> GetStationTimetable(string station, Dictionary<string, Dictionary<string, string[]>> timetable)
    {
        var result = new List<string>();
        if (timetable.ContainsKey(station))
        {
            var preResult = new StringBuilder();
            foreach (var line in timetable[station])
            {
                preResult.Append(line.Key + ": " + string.Join(", ", line.Value) + "\n");
            }
            result = preResult.ToString().Split('\n').ToList(); 
        }
        else
        {
            result.Add("Станция не найдена");
        }
        return result;
    }
}
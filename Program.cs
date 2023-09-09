using HtmlAgilityPack;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using OfficeOpenXml;
using PriceComparer.Misc;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PriceComparer;

public static partial class Program
{
    private static HttpClient _client = new();
    private static readonly HtmlDocument _doc = new();

    private static List<List<Product>> _listOfLists = new() { new(), new(), new(), new() };

    private static async Task Main()
    {
        while (true)
        {
            await Console.Out.WriteLineAsync("1 - 5ka");
            await Console.Out.WriteLineAsync("2 - Okay");
            await Console.Out.WriteLineAsync("3 - Perekrestok");
            await Console.Out.WriteLineAsync("4 - Evroopt");

            var query = await Console.In.ReadLineAsync();
            switch (query)
            {
                case "1":
                    await Load5Ka();
                    break;

                case "2":
                    await LoadOkay();
                    break;

                case "3":
                    await LoadVprok();
                    break;

                case "4":
                    await LoadEvroopt();
                    break;

                case "5":
                    await LoadGreen();
                    break;

                default:
                    await Console.Out.WriteLineAsync("Incorrect Format!");
                    break;
            }

            SaveToExcel();
        }
    }

    private static Task LoadGreen()
    {

    }

    private static async Task LoadEvroopt()
    {
        string pdfPath = await DownloadFileAsync("https://evroopt.by/wp-content/uploads/redprice/04092023/list_04092023.pdf");

        using PdfDocument pdfDoc = new(new PdfReader(pdfPath));

        StringBuilder extractedText = new();
        for (int pageNum = 1; pageNum <= pdfDoc.GetNumberOfPages(); pageNum++)
        {
            LocationTextExtractionStrategy extractionStrategy = new();

            PdfCanvasProcessor parser = new(extractionStrategy);
            parser.ProcessPageContent(pdfDoc.GetPage(pageNum));

            extractedText.AppendLine(extractionStrategy.GetResultantText());
        }
        string allText = extractedText.ToString();

        MatchCollection matches = PdfRegex().Matches(allText);
        foreach (var (name, price) in from Match match in matches
                                      let name = match.Groups[1].Value.Trim()
                                      let price = match.Groups[3].Value
                                      select (name, price))
        {
            lock (_listOfLists)
            {
                _listOfLists[3].Add(new(name, double.Parse(price)));
            }

            Console.WriteLine($"Name: {name}\nPrice: {price}");
        }
    }

    private static async Task Load5Ka()
    {
        for (int i = 1; i < 1000; i++)
        {
            string apiUrl = $"http://5ka.ru/api/v2/special_offers/?page={i}";
            string json = await GetAsync(apiUrl);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(json);
            if (apiResponse == null || apiResponse.Results == null || apiResponse.Results.Count == 0)
            {
                Console.WriteLine("No products found in the JSON data.");
                return;
            }

            // Extract the relevant data and add it to _listOfLists[1].
            foreach (var result in apiResponse.Results)
            {
                if (result == null)
                {
                    return;
                }
                var product = new Product(result.Name, result.CurrentPrices.PricePromoMin);

                lock (_listOfLists)
                {
                    _listOfLists[0].Add(product);
                }
            }

            Console.WriteLine($"Added {apiResponse.Results.Count} products to _listOfLists[1].");

            if (apiResponse.Next is null)
                break;
        }
    }

    private static async Task LoadOkay()
    {
        for (int page = 1; page <= 200; page++)
        {
            string html = await GetAsync($"https://www.okmarket.ru/products/?sort=POPULAR&nav-products=page-{page}");
            if (html == "")
                return;

            HtmlNodeCollection? productNodes = await ExtractListNode(html, "//div[@class='ass-prod-card__info']");

            if (productNodes is null)
                return;

            await ExtractNameAndPrice(_listOfLists[1], productNodes, ".//div[@class='ass-prod-card__name']", ".//div[@class='ass-prod-card__prices-base']");
        }
    }

    private static async Task LoadVprok()
    {
        for (int category = 1294; category < 5000; category++)
            for (int page = 0; page < 100; page++)
            {
                string html = await GetAsync($"https://www.vprok.ru/catalog/{category}unknown?sort=popularity_desc&page={page}");
                if (html == "")
                    return;

                HtmlNodeCollection? productNodes = await ExtractListNode(html, "//div[@class='ProductTilesListing_root__g87sH']");

                if (productNodes is null)
                    return;

                await ExtractNameAndPrice(_listOfLists[2], productNodes, ".//a[@class='MainProductTile_title__AHt0H MainProductTile_tall__cD7Dz']", ".//span[@class='Price_price__B1Q8E Price_size_SM__3XOjt Price_role_discount__E0QVZ']");
            }
    }

    private static async Task<HtmlNodeCollection?> ExtractListNode(string html, string name)
    {
        HtmlNodeCollection? collection = null;
        try
        {
            _doc.LoadHtml(html);
            collection = _doc.DocumentNode.SelectNodes(name);
        }
        catch (Exception ex) { await Console.Out.WriteLineAsync(ex.Message); }

        return collection;
    }

    private static async Task ExtractNameAndPrice(List<Product> list, HtmlNodeCollection? productNodes, string name, string price)
    {
        foreach (var (productName, match) in from productNode in productNodes
                                             let productNameNode = productNode.SelectSingleNode(name)
                                             let productName = productNameNode?.InnerText.Trim()
                                             let productPriceNode = productNode.SelectSingleNode(price)
                                             let productPrice = productPriceNode?.InnerText
                                             let match = PriceRegex().Match(productPrice)
                                             where !string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(productPrice)
                                             select (productName, match))
        {
            if (!int.TryParse(match.Value, out int val))
                continue;

            AddToList(list, productName, val);
            await OutputToConsole(productName, match);
        }
    }

    private static void AddToList(List<Product> list, string? productName, int val)
    {
        lock (list)
            list.Add(new Product(productName, val));
    }

    private static async Task OutputToConsole(string? productName, Match match)
    {
        await Console.Out.WriteLineAsync($"Product Name: {productName}");
        await Console.Out.WriteLineAsync($"Product Price: {match.Value}\n");
    }

    private static async Task<string> DownloadFileAsync(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url);

        string? contentType = response.Content.Headers.ContentType?.MediaType;

        string fileExtension = GetFileExtensionFromContentType(contentType);
        string localFilePath = $"temp{fileExtension}";

        Stream pdfStream = await response.Content.ReadAsStreamAsync();

        using FileStream fileStream = File.Open(localFilePath, FileMode.Create);

        await pdfStream.CopyToAsync(fileStream);

        fileStream.Dispose();

        return localFilePath;
    }

    private static async Task<string> GetAsync(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to fetch page: {url}");
            throw new HttpRequestException();
        }

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> PostAsync(string url, string payload)
    {
        StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _client.PostAsync(url, content);


    }

    private static void SaveToExcel()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(new FileInfo("Prices.xlsx"));

        for (int j = 0; j < _listOfLists.Count; j++)
        {
            for (int i = 0; i < _listOfLists[j].Count; i++)
            {
                Product? item = _listOfLists[j][i];
                package.Workbook.Worksheets[j].Cells.SetCellValue(i, 0, item.Name);
                package.Workbook.Worksheets[j].Cells.SetCellValue(i, 1, item.Price);
            }
        }
        package.Save();
    }

    private static string GetFileExtensionFromContentType(string? contentType)
    {
        if (contentType == null)
            return ".unknown";

        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "application/zip" => ".zip",
            "audio/mpeg" => ".mp3",
            "audio/wav" => ".wav",
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/x-msvideo" => ".avi",

            _ => ".unknown"
        };
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"(.+?)\s+([\d.]+)\s+([\d.]+)")]
    private static partial Regex PdfRegex();
}
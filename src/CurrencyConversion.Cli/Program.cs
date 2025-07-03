using System.Net.Http.Json;

record ConversionRequest(string From, string To, decimal Amount);

record ConversionResult(string from, string to, decimal amount, decimal convertedAmount, decimal rate, DateTime timestamp);

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string from = "";
        string to = "";
        decimal amount = 1m;
        string api = "http://localhost:5000";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--from":
                    from = args[++i];
                    break;
                case "--to":
                    to = args[++i];
                    break;
                case "--amount":
                    if (!decimal.TryParse(args[++i], out amount))
                    {
                        Console.WriteLine("Invalid amount");
                        return 1;
                    }
                    break;
                case "--api":
                    api = args[++i].TrimEnd('/');
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;
            }
        }

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            Console.WriteLine("--from and --to are required");
            return 1;
        }

        using var client = new HttpClient { BaseAddress = new Uri(api) };
        try
        {
            var request = new ConversionRequest(from, to, amount);
            var response = await client.PostAsJsonAsync("/api/ExchangeRate/convert", request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"API error: {response.StatusCode}");
                var text = await response.Content.ReadAsStringAsync();
                Console.WriteLine(text);
                return 1;
            }
            var result = await response.Content.ReadFromJsonAsync<ConversionResult>();
            if (result == null)
            {
                Console.WriteLine("Invalid response from API");
                return 1;
            }
            Console.WriteLine($"{result.amount} {result.from} = {result.convertedAmount} {result.to} (rate {result.rate})");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet run -- --from USD --to TWD --amount 100 [--api http://localhost:5000]");
    }
}

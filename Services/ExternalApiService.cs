using System.Net;
using System.Text.Json;
using CryptoMetrics.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoMetrics.Services;

public interface IExternalApiService
{
    Task<(List<Cryptocurrency> Items, string Source)> GetCryptocurrenciesAsync();

    Task<(List<MarketData> Data, string Source)> GetHistoricalDataAsync(Cryptocurrency coin, int days);
}

public class ExternalApiService : IExternalApiService
{
    private const string SourceCoinGecko = "CoinGecko";
    private const string ListCacheKey = "cryptocurrencies";

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExternalApiService> _logger;

    public ExternalApiService(HttpClient http, IMemoryCache cache, ILogger<ExternalApiService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(List<Cryptocurrency> Items, string Source)> GetCryptocurrenciesAsync()
    {
        if (_cache.TryGetValue(ListCacheKey, out (List<Cryptocurrency> Items, string Source) cached))
            return cached;

        using var response = await _http.GetAsync(
            "coins/markets?vs_currency=usd&order=market_cap_desc&per_page=20&page=1");
        EnsureSuccess(response);

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        var items = new List<Cryptocurrency>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            items.Add(new Cryptocurrency
            {
                Id = el.GetProperty("id").GetString() ?? string.Empty,
                Name = el.GetProperty("name").GetString() ?? string.Empty,
                Symbol = (el.GetProperty("symbol").GetString() ?? string.Empty).ToUpperInvariant(),
                CurrentPrice = el.TryGetProperty("current_price", out var p) && p.ValueKind == JsonValueKind.Number
                    ? p.GetDouble()
                    : 0
            });
        }

        if (items.Count == 0)
            throw new HttpRequestException("CoinGecko вернул пустой список криптовалют");

        var result = (items, SourceCoinGecko);
        _cache.Set(ListCacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<(List<MarketData> Data, string Source)> GetHistoricalDataAsync(Cryptocurrency coin, int days)
    {
        var cacheKey = $"history:{coin.Id}:{days}";
        if (_cache.TryGetValue(cacheKey, out (List<MarketData> Data, string Source) cached))
            return cached;

        var url = $"coins/{coin.Id}/market_chart?vs_currency=usd&days={days}&interval=daily";
        using var response = await _http.GetAsync(url);
        EnsureSuccess(response);

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var prices = doc.RootElement.GetProperty("prices");
        var volumes = doc.RootElement.GetProperty("total_volumes");

        var points = new List<MarketData>();
        for (int i = 0; i < prices.GetArrayLength(); i++)
        {
            points.Add(new MarketData
            {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)prices[i][0].GetDouble()).UtcDateTime,
                Price = prices[i][1].GetDouble(),
                Volume = i < volumes.GetArrayLength() ? volumes[i][1].GetDouble() : 0
            });
        }

        var daily = points
            .GroupBy(p => p.Timestamp.Date)
            .Select(g => g.Last())
            .ToList();

        if (daily.Count == 0)
            throw new HttpRequestException($"CoinGecko вернул пустую историю цен для {coin.Symbol}");

        var result = (daily, SourceCoinGecko);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    private void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            _logger.LogWarning("CoinGecko: превышен лимит запросов (429). Укажите CoinGecko:ApiKey или подождите минуту.");

        response.EnsureSuccessStatusCode();
    }
}
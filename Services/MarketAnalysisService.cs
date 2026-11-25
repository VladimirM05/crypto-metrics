using System.Globalization;
using CryptoMetrics.Models;

namespace CryptoMetrics.Services;

public interface IMarketAnalysisService
{
    Task<MarketAnalysisResult?> CreateAnalysisAsync(AnalysisRequest request);

    IReadOnlyList<MarketAnalysisResult> GetAll();

    MarketAnalysisResult? GetById(int id);
}

public class MarketAnalysisService : IMarketAnalysisService
{
    private static readonly List<MarketAnalysisResult> Storage = new();
    private static readonly object SyncRoot = new();
    private static int _nextId = 1;

    private static readonly IndicatorRequest[] DefaultIndicators =
    {
        new IndicatorRequest { Name = "SMA", Period = 7 },
        new IndicatorRequest { Name = "EMA", Period = 7 },
        new IndicatorRequest { Name = "RSI", Period = 14 }
    };

    private readonly IExternalApiService _api;
    private readonly IIndicatorService _indicators;

    public MarketAnalysisService(IExternalApiService api, IIndicatorService indicators)
    {
        _api = api;
        _indicators = indicators;
    }

    public async Task<MarketAnalysisResult?> CreateAnalysisAsync(AnalysisRequest request)
    {
        var symbol = request.Symbol.Trim();

        var (coins, _) = await _api.GetCryptocurrenciesAsync();
        var coin = coins.FirstOrDefault(c => c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (coin is null) return null;

        var (data, source) = await _api.GetHistoricalDataAsync(coin, request.Days);

        var requested = request.Indicators.Count > 0
            ? request.Indicators
            : DefaultIndicators.Where(d => data.Count > d.Period).ToList();

        if (requested.Count == 0)
            throw new ArgumentException($"Слишком мало данных ({data.Count} точек) для расчёта индикаторов. Увеличьте days.");

        var indicators = requested
            .Select(r => _indicators.Calculate(r.Name, r.Period, data))
            .ToList();

        var result = new MarketAnalysisResult
        {
            Cryptocurrency = coin,
            Days = request.Days,
            DataSource = source,
            CreatedAt = DateTime.UtcNow,
            MarketData = data,
            Indicators = indicators,
            Summary = BuildSummary(data, indicators)
        };

        lock (SyncRoot)
        {
            result.Id = _nextId++;
            Storage.Add(result);
        }

        return result;
    }

    public IReadOnlyList<MarketAnalysisResult> GetAll()
    {
        lock (SyncRoot) return Storage.ToList();
    }

    public MarketAnalysisResult? GetById(int id)
    {
        lock (SyncRoot) return Storage.FirstOrDefault(r => r.Id == id);
    }

    private static string BuildSummary(List<MarketData> data, List<Indicator> indicators)
    {
        var first = data.First().Price;
        var last = data.Last().Price;
        var change = first == 0 ? 0 : (last - first) / first * 100;

        var parts = new List<string> { $"Изменение цены за период: {Fmt(change)}%" };

        foreach (var indicator in indicators)
        {
            if (indicator.LastValue is not double value) continue;

            if (indicator.Name == "RSI")
            {
                var state = value >= 70 ? "перекупленность" : value <= 30 ? "перепроданность" : "нейтральная зона";
                parts.Add($"RSI({indicator.Period}) = {Fmt(value)} ({state})");
            }
            else
            {
                var position = last >= value ? "выше" : "ниже";
                parts.Add($"{indicator.Name}({indicator.Period}) = {Fmt(value)}, цена {position} индикатора");
            }
        }

        return string.Join("; ", parts);
    }

    private static string Fmt(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}

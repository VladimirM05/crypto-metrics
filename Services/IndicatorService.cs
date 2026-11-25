using CryptoMetrics.Models;

namespace CryptoMetrics.Services;

public interface IIndicatorService
{
    Indicator Calculate(string name, int period, IReadOnlyList<MarketData> data);
}

public class IndicatorService : IIndicatorService
{
    private static readonly string[] Supported = { "SMA", "EMA", "RSI" };

    public Indicator Calculate(string name, int period, IReadOnlyList<MarketData> data)
    {
        var normalized = (name ?? string.Empty).Trim().ToUpperInvariant();

        if (!Supported.Contains(normalized))
            throw new ArgumentException($"Неизвестный индикатор '{name}'. Доступны: SMA, EMA, RSI.");

        var required = normalized == "RSI" ? period + 1 : period;
        if (data.Count < required)
            throw new ArgumentException(
                $"Для {normalized}({period}) нужно минимум {required} точек данных, а получено {data.Count}. " +
                "Увеличьте days или уменьшите period.");

        var prices = data.Select(d => d.Price).ToList();

        var values = normalized switch
        {
            "SMA" => Sma(prices, period),
            "EMA" => Ema(prices, period),
            _ => Rsi(prices, period)
        };

        return new Indicator
        {
            Name = normalized,
            Period = period,
            Values = values.Select(v => v.HasValue ? Math.Round(v.Value, 4) : (double?)null).ToList()
        };
    }

    private static List<double?> Sma(List<double> prices, int period)
    {
        var result = Enumerable.Repeat<double?>(null, prices.Count).ToList();
        double sum = 0;

        for (int i = 0; i < prices.Count; i++)
        {
            sum += prices[i];
            if (i >= period) sum -= prices[i - period];
            if (i >= period - 1) result[i] = sum / period;
        }

        return result;
    }

    private static List<double?> Ema(List<double> prices, int period)
    {
        var result = Enumerable.Repeat<double?>(null, prices.Count).ToList();
        if (prices.Count < period) return result;

        double k = 2.0 / (period + 1);
        double ema = prices.Take(period).Average();
        result[period - 1] = ema;

        for (int i = period; i < prices.Count; i++)
        {
            ema = prices[i] * k + ema * (1 - k);
            result[i] = ema;
        }

        return result;
    }

    private static List<double?> Rsi(List<double> prices, int period)
    {
        var result = Enumerable.Repeat<double?>(null, prices.Count).ToList();
        if (prices.Count <= period) return result;

        double gainSum = 0, lossSum = 0;
        for (int i = 1; i <= period; i++)
        {
            double change = prices[i] - prices[i - 1];
            if (change >= 0) gainSum += change; else lossSum -= change;
        }

        double avgGain = gainSum / period;
        double avgLoss = lossSum / period;
        result[period] = ToRsi(avgGain, avgLoss);

        for (int i = period + 1; i < prices.Count; i++)
        {
            double change = prices[i] - prices[i - 1];
            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;

            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
            result[i] = ToRsi(avgGain, avgLoss);
        }

        return result;
    }

    private static double ToRsi(double avgGain, double avgLoss)
    {
        if (avgLoss == 0) return avgGain == 0 ? 50 : 100;
        var rs = avgGain / avgLoss;
        return 100 - 100 / (1 + rs);
    }
}

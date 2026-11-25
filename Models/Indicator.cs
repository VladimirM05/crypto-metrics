namespace CryptoMetrics.Models;

public class Indicator
{
    public string Name { get; set; } = string.Empty;

    public int Period { get; set; }

    public List<double?> Values { get; set; } = new();

    public double? LastValue => Values.LastOrDefault();
}

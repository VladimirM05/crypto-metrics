namespace CryptoMetrics.Models;

public class Cryptocurrency
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public double CurrentPrice { get; set; }
}

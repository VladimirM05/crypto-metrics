namespace CryptoMetrics.Models;

public class MarketAnalysisResult
{
    public int Id { get; set; }

    public Cryptocurrency Cryptocurrency { get; set; } = new();
    
    public int Days { get; set; }

    public string DataSource { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string Summary { get; set; } = string.Empty;

    public List<Indicator> Indicators { get; set; } = new();

    public List<MarketData> MarketData { get; set; } = new();
}

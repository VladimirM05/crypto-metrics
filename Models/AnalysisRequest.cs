using System.ComponentModel.DataAnnotations;

namespace CryptoMetrics.Models;

public class AnalysisRequest
{
    [Required(ErrorMessage = "Укажите тикер криптовалюты")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "Тикер должен быть от 1 до 10 символов")]
    public string Symbol { get; set; } = string.Empty;

    [Range(2, 365, ErrorMessage = "Период должен быть от 2 до 365 дней")]
    public int Days { get; set; } = 30;

    public List<IndicatorRequest> Indicators { get; set; } = new();
}

public class IndicatorRequest
{
    [Required(ErrorMessage = "Укажите название индикатора")]
    public string Name { get; set; } = string.Empty;

    [Range(2, 200, ErrorMessage = "Период индикатора должен быть от 2 до 200")]
    public int Period { get; set; } = 14;
}

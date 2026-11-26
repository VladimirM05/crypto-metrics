using CryptoMetrics.Models;
using CryptoMetrics.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMetrics.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalysisController : ControllerBase
{
    private readonly IMarketAnalysisService _service;

    public AnalysisController(IMarketAnalysisService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MarketAnalysisResult>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<MarketAnalysisResult>> GetAll()
    {
        return Ok(_service.GetAll());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MarketAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<MarketAnalysisResult> GetById(int id)
    {
        var result = _service.GetById(id);
        if (result is null)
            return NotFound(new { error = $"Анализ с id={id} не найден" });

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MarketAnalysisResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MarketAnalysisResult>> Post([FromBody] AnalysisRequest request)
    {
        try
        {
            var result = await _service.CreateAnalysisAsync(request);
            if (result is null)
                return NotFound(new
                {
                    error = $"Криптовалюта '{request.Symbol}' не найдена. Список доступных: GET /api/cryptocurrencies"
                });

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Внешний API CoinGecko недоступен или превышен лимит запросов. Попробуйте позже." });
        }
        
    }
}

using CryptoMetrics.Models;
using CryptoMetrics.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMetrics.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CryptocurrenciesController : ControllerBase
{
    private readonly IExternalApiService _api;

    public CryptocurrenciesController(IExternalApiService api)
    {
        _api = api;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Cryptocurrency>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IEnumerable<Cryptocurrency>>> Get()
    {
        try
        {
            var (items, source) = await _api.GetCryptocurrenciesAsync();
            Response.Headers["X-Data-Source"] = source;
            return Ok(items);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Внешний API CoinGecko недоступен или превышен лимит запросов. Попробуйте позже." });
        }
    }
}

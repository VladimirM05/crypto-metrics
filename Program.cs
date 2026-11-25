using CryptoMetrics.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CryptoMetrics/1.0");

    var apiKey = builder.Configuration["CoinGecko:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
        client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
});

builder.Services.AddScoped<IIndicatorService, IndicatorService>();
builder.Services.AddScoped<IMarketAnalysisService, MarketAnalysisService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseAuthorization();

app.MapControllers();

app.Urls.Add("http://0.0.0.0:80");

app.Run();

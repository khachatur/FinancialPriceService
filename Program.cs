using FinancialPriceService;
using FinancialPriceService.Hubs;
using FinancialPriceService.Services;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddLogging(config =>
{
	config.AddConsole();
});

// Configure DI for application services
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<PriceStore>();
builder.Services.AddHostedService<BinanceWebSocketService>();
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "Financial Price Service API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Price Service API v1"));
}

app.UseWebSockets();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapHub<PriceHub>("/priceHub");

app.Map("/ws", async context =>
{
	if (context.WebSockets.IsWebSocketRequest)
	{
		var webSocket = await context.WebSockets.AcceptWebSocketAsync();
		var connectionManager = app.Services.GetRequiredService<WebSocketConnectionManager>();
		await connectionManager.HandleWebSocketConnection(context, webSocket);
	}
	else
	{
		context.Response.StatusCode = 400;
	}
});

app.Run();

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FinancialPriceService.Services
{
	public class BinanceWebSocketService : BackgroundService
	{
		private readonly WebSocketConnectionManager _connectionManager;
		private readonly PriceStore _priceStore;
		private readonly ILogger<BinanceWebSocketService> _logger;
		private readonly string[] _instruments = new[] { "btcusdt@aggTrade", "ethusdt@aggTrade", "eurusdt@aggTrade" }; // Add more instruments as needed

		/// <summary>
		/// Initializes a new instance of the <see cref="BinanceWebSocketService"/> class.
		/// </summary>
		/// <param name="connectionManager">The WebSocket connection manager to manage connections.</param>
		/// <param name="priceStore">The price store to store and retrieve prices.</param>
		/// <param name="logger">The logger to log information and errors.</param>
		public BinanceWebSocketService(WebSocketConnectionManager connectionManager, PriceStore priceStore, ILogger<BinanceWebSocketService> logger)
		{
			_connectionManager = connectionManager;
			_priceStore = priceStore;
			_logger = logger;
		}

		/// <summary>
		/// This method is responsible for managing the WebSocket connection to Binance and handling incoming messages.
		/// </summary>
		/// <param name="stoppingToken">A token to observe for cancellation requests.</param>
		/// <returns>A Task representing the asynchronous operation.</returns>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// Initialize a new WebSocket client
			using var client = new ClientWebSocket();

			// Connect to the Binance WebSocket endpoint
			await client.ConnectAsync(new Uri("wss://stream.binance.com:443/ws"), stoppingToken);

			// Create a subscription message for the desired instruments
			var subscriptionMessage = new
			{
				method = "SUBSCRIBE",
				@params = _instruments,
				id = 1
			};

			// Serialize the subscription message to JSON
			var message = JsonSerializer.Serialize(subscriptionMessage);

			// Convert the JSON message to bytes
			var bytes = Encoding.UTF8.GetBytes(message);

			// Send the subscription message to the Binance WebSocket
			await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, stoppingToken);

			// Log a message indicating successful connection and subscription
			_logger.LogInformation("Connected to Binance WebSocket and subscribed to instruments");

			// Create a buffer to receive incoming messages
			var buffer = new byte[1024 * 4];

			// Enter a loop to receive and process messages
			while (!stoppingToken.IsCancellationRequested)
			{
				// Receive a message from the Binance WebSocket
				var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);

				// If the message type is a close message, close the WebSocket connection
				if (result.MessageType == WebSocketMessageType.Close)
				{
					await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", stoppingToken);
					break;
				}
				// If the message type is a text message, process it
				else if (result.MessageType == WebSocketMessageType.Text)
				{
					// Convert the received bytes to a string
					var messageReceived = Encoding.UTF8.GetString(buffer, 0, result.Count);

					// Log the received message
					_logger.LogInformation($"Received message: {messageReceived}");

					// Parse the received message as JSON
					var data = JsonDocument.Parse(messageReceived).RootElement;

					// Extract the symbol and price from the JSON message
					if (data.TryGetProperty("s", out var symbolProperty) && data.TryGetProperty("p", out var priceProperty))
					{
						var symbol = symbolProperty.GetString();
						var priceString = priceProperty.GetString();

						// Try to parse the price as a decimal
						if (!string.IsNullOrEmpty(symbol) && !string.IsNullOrEmpty(priceString) && decimal.TryParse(priceString, out var price))
						{
							// Map the Binance symbol to our API instrument
							var instrument = symbol switch
							{
								"BTCUSDT" => "BTCUSD",
								"ETHUSDT" => "ETHUSD",
								"EURUSDT" => "EURUSD",
								_ => null
							};

							// If the instrument is valid, update the price in the price store
							if (instrument != null)
							{
								_priceStore.UpdatePrice(instrument, price);
							}
						}
					}

					// Broadcast the received message to all connected clients
					await _connectionManager.BroadcastMessage(messageReceived);
				}
			}
		}
	}
}
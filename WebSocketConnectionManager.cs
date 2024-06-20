using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace FinancialPriceService
{
	public class WebSocketConnectionManager
	{
		private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
		private readonly ILogger<WebSocketConnectionManager> _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="WebSocketConnectionManager"/> class.
		/// </summary>
		/// <param name="logger">An instance of <see cref="ILogger{WebSocketConnectionManager}"/> for logging.</param>
		public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// Handles a WebSocket connection and manages it within the application.
		/// </summary>
		/// <param name="context">The HTTP context of the WebSocket connection.</param>
		/// <param name="webSocket">The WebSocket instance to be managed.</param>
		/// <returns>A task that represents the asynchronous handling of the WebSocket connection.</returns>
		public async Task HandleWebSocketConnection(HttpContext context, WebSocket webSocket)
		{
			// Generate a unique connection ID
			var connectionId = Guid.NewGuid().ToString();

			// Add the new WebSocket connection to the connections dictionary
			_connections.TryAdd(connectionId, webSocket);

			// Log the establishment of the WebSocket connection
			_logger.LogInformation($"WebSocket connection established: {connectionId}");

			// Prepare a buffer for receiving data from the WebSocket
			var buffer = new byte[1024 * 4];

			// Receive the initial data from the WebSocket
			var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

			// Continue receiving data until the WebSocket connection is closed
			while (!result.CloseStatus.HasValue)
			{
				// Receive more data from the WebSocket
				result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			}

			// Remove the WebSocket connection from the connections dictionary
			_connections.TryRemove(connectionId, out _);

			// Close the WebSocket connection
			await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

			// Log the closure of the WebSocket connection
			_logger.LogInformation($"WebSocket connection closed: {connectionId}");
		}

		/// <summary>
		/// Broadcasts a message to all currently open WebSocket connections.
		/// </summary>
		/// <param name="message">The message to be broadcasted.</param>
		/// <returns>A task that represents the asynchronous broadcast operation.</returns>
		public async Task BroadcastMessage(string message)
		{
			// Convert the message to a byte array using UTF-8 encoding
			var buffer = System.Text.Encoding.UTF8.GetBytes(message);

			// Iterate through all the WebSocket connections and send them through the broadcast
			foreach (var connection in _connections.Values)
			{
				// Check if the connection is open
				if (connection.State == WebSocketState.Open)
				{
					// Send the message to the WebSocket connection
					await connection.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
				}
			}
		}
	}
}
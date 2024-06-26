using Microsoft.AspNetCore.SignalR;

namespace FinancialPriceService.Hubs
{
	public class PriceHub : Hub
	{
		private readonly PriceStore _priceStore;

		/// <summary>
		/// Initializes a new instance of the <see cref="PriceHub"/> class.
		/// </summary>
		/// <param name="priceStore">An instance of <see cref="PriceStore"/> used to retrieve prices.</param>
		public PriceHub(PriceStore priceStore)
		{
			_priceStore = priceStore;
		}

		/// <summary>
		/// Subscribes to the price updates for a specific instrument.
		/// </summary>
		/// <param name="instrument">The identifier of the instrument for which to subscribe.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task Subscribe(string instrument)
		{
			// Check if the price store has the price for the specified instrument
			if (_priceStore.TryGetPrice(instrument, out var price))
			{
				// If the price exists, send it to the caller using SignalR's SendAsync method
				await Clients.Caller.SendAsync("ReceivePriceUpdate", instrument, price);
			}
		}
	}
}

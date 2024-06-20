using Microsoft.AspNetCore.Mvc;

namespace FinancialPriceService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PricesController : ControllerBase
	{
		private readonly PriceStore _priceStore;

		/// <summary>
		/// Initializes a new instance of the <see cref="PricesController"/> class.
		/// </summary>
		/// <param name="priceStore">An instance of <see cref="PriceStore"/> used to retrieve instrument prices.</param>
		public PricesController(PriceStore priceStore)
		{
			_priceStore = priceStore;
		}       /// <summary>
				/// Retrieves a list of all available instruments.
				/// </summary>
				/// <returns>An <see cref="IActionResult"/> containing a list of instruments.</returns>
		[HttpGet("instruments")]
		public IActionResult GetInstruments()
		{
			var instruments = _priceStore.GetInstruments();
			return Ok(instruments);
		}

		/// <summary>
		/// Retrieves the current price of a specific instrument.
		/// </summary>
		/// <param name="instrument">The unique identifier of the instrument.</param>
		/// <returns>
		/// An <see cref="IActionResult"/> containing the current price of the instrument.
		/// If the instrument is not found or no data is available, a <see cref="NotFoundResult"/> is returned.
		/// </returns>
		[HttpGet("currentprice/{instrument}")]
		public IActionResult GetCurrentPrice(string instrument)
		{
			// Try to get the price of the instrument from the store
			if (_priceStore.TryGetPrice(instrument, out var price))
			{
				// If the price is found, return it as a JSON object
				return Ok(new { Instrument = instrument, Price = price });
			}

			// If the price is not found, return a NotFoundResult with a custom error message
			return NotFound(new { Message = "Instrument not found or no data available" });
		}
	}
}

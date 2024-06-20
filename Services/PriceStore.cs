using System.Collections.Concurrent;

public class PriceStore
{
	private readonly ConcurrentDictionary<string, decimal> _prices = new();

	/// <summary>
	/// Updates the price of a specific instrument in the store.
	/// </summary>
	/// <param name="instrument">The identifier of the instrument.</param>
	/// <param name="price">The new price of the instrument.</param>
	public void UpdatePrice(string instrument, decimal price)
	{
		_prices[instrument] = price;
	}

	/// <summary>
	/// Tries to get the price of a specific instrument from the store.
	/// </summary>
	/// <param name="instrument">The identifier of the instrument.</param>
	/// <param name="price">When this method returns, contains the price of the instrument if the instrument is found, or zero if not found. This parameter is passed uninitialized.</param>
	/// <returns>True if the price of the instrument was successfully retrieved; otherwise, false.</returns>
	public bool TryGetPrice(string instrument, out decimal price)
	{
		return _prices.TryGetValue(instrument, out price);
	}

	/// <summary>
	/// Retrieves a collection of instrument identifiers stored in the price store.
	/// </summary>
	/// <returns>An enumerable collection of instrument identifiers.</returns>
	public IEnumerable<string> GetInstruments()
	{
		return _prices.Keys;
	}
}

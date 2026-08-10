namespace ShippingMcp.Shipping;

public record QuoteRequest(string Origin, string Destination, double WeightKg);

public record Quote(string Carrier, decimal Price, int EstimatedDays);

// El resultado tipado: éxito o motivo. Nunca una excepción hacia el modelo.
public record QuoteResult(bool Success, Quote? Quote, string? Reason)
{
    public static QuoteResult Ok(Quote quote) => new(true, quote, null);
    public static QuoteResult Unsupported(string reason) => new(false, null, reason);
}

public interface IShipmentHandler
{
    string Carrier { get; }
    bool CanHandle(QuoteRequest request);
    QuoteResult GetQuote(QuoteRequest request);
}
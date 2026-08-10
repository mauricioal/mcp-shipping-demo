namespace ShippingMcp.Shipping;

public class ExpressHandler : IShipmentHandler
{
    public string Carrier => "express";

    public bool CanHandle(QuoteRequest r) => r.WeightKg <= 50 && !IsInternational(r);

    public QuoteResult GetQuote(QuoteRequest r)
    {
        if (r.WeightKg > 50)
            return QuoteResult.Unsupported("Express does not accept packages over 50kg.");
        if (IsInternational(r))
            return QuoteResult.Unsupported("Express operates domestically only.");

        return QuoteResult.Ok(new("express", 18.00m + (decimal)r.WeightKg * 1.20m, 2));
    }

    internal static bool IsInternational(QuoteRequest r) =>
        r.Origin.Length != r.Destination.Length;
}

public class EconomyHandler : IShipmentHandler
{
    public string Carrier => "economy";

    public bool CanHandle(QuoteRequest r) =>
        r.WeightKg <= 30 && !ExpressHandler.IsInternational(r);

    public QuoteResult GetQuote(QuoteRequest r)
    {
        if (r.WeightKg > 30)
            return QuoteResult.Unsupported("Economy does not accept packages over 30kg.");
        if (ExpressHandler.IsInternational(r))
            return QuoteResult.Unsupported("Economy operates domestically only.");

        return QuoteResult.Ok(new("economy", 6.50m + (decimal)r.WeightKg * 0.40m, 6));
    }
}

public class InternationalHandler : IShipmentHandler
{
    public string Carrier => "international";

    public bool CanHandle(QuoteRequest r) =>
    ExpressHandler.IsInternational(r) && r.WeightKg <= 200;

    public QuoteResult GetQuote(QuoteRequest r)
    {
        if (!ExpressHandler.IsInternational(r))
            return QuoteResult.Unsupported("International is only for cross-border shipments.");
        if (r.WeightKg > 200)
            return QuoteResult.Unsupported("International does not accept packages over 200kg.");

        return QuoteResult.Ok(new("international", 42.00m + (decimal)r.WeightKg * 2.10m, 12));
    }
}
using System.ComponentModel;
using ModelContextProtocol.Server;
using ShippingMcp.Shipping;

namespace ShippingMcp.Tools;

[McpServerToolType]
public class ShippingTools(IServiceProvider services, ILogger<ShippingTools> logger)
{
    private static readonly string[] Carriers = ["express", "economy", "international"];

    [McpServerTool]
    [Description("Lists the shipping carriers available on this server.")]
    public string ListCarriers() => string.Join(", ", Carriers);

    // El llamante elige el carrier.
    [McpServerTool]
    [Description("Gets a shipping quote from a specific carrier.")]
    public string GetQuote(
        [Description("Carrier code. Call list_carriers for valid values.")] string carrier,
        [Description("Origin postal code.")] string origin,
        [Description("Destination postal code.")] string destination,
        [Description("Package weight in kilograms.")] double weightKg)
    {
        var handler = services.GetKeyedService<IShipmentHandler>(carrier);
        if (handler is null)
            return $"Unknown carrier '{carrier}'. Available carriers: {string.Join(", ", Carriers)}.";

        var result = handler.GetQuote(new(origin, destination, weightKg));
        logger.LogInformation("Quote requested for carrier {Carrier}: {Outcome}",
            carrier, result.Success ? "ok" : result.Reason);

        return Render(result);
    }

    // El servidor deduce el carrier.
    [McpServerTool]
    [Description("Gets the cheapest available shipping quote. The server selects the carrier.")]
    public string GetBestQuote(
        [Description("Origin postal code.")] string origin,
        [Description("Destination postal code.")] string destination,
        [Description("Package weight in kilograms.")] double weightKg)
    {
        var request = new QuoteRequest(origin, destination, weightKg);

        var quotes = Carriers
            .Select(c => services.GetRequiredKeyedService<IShipmentHandler>(c))
            .Where(h => h.CanHandle(request))
            .Select(h => h.GetQuote(request))
            .Where(r => r.Success)
            .Select(r => r.Quote!)
            .OrderBy(q => q.Price)
            .ToList();

        if (quotes.Count == 0)
            return "No carrier can handle this shipment. Try get_quote with a specific carrier to see why.";

        var best = quotes[0];
        logger.LogInformation(
            "Best quote selected: {Carrier} at {Price} — {Considered} carriers considered, {Eligible} eligible",
            best.Carrier, best.Price, Carriers.Length, quotes.Count);

        return $"{best.Carrier}: ${best.Price:F2} USD, {best.EstimatedDays} business days.";
    }

    private static string Render(QuoteResult r) =>
        r.Success
            ? $"{r.Quote!.Carrier}: ${r.Quote.Price:F2} USD, {r.Quote.EstimatedDays} business days."
            : $"Not available: {r.Reason}";
}
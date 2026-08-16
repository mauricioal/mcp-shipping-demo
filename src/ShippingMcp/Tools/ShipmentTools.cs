using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ShippingMcp.Shipping;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

namespace ShippingMcp.Tools;

[McpServerToolType]
public class ShipmentTools(IServiceProvider services, ILogger<ShipmentTools> logger)
{
    [McpServerTool]
    [Description("Creates a shipment. For international shipments the server will ask for customs information if it is missing.")]
    public async Task<string> CreateShipment(
        McpServer server,
        [Description("Carrier code. Call list_carriers for valid values.")] string carrier,
        [Description("Origin postal code.")] string origin,
        [Description("Destination postal code.")] string destination,
        [Description("Package weight in kilograms.")] double weightKg,
        CancellationToken cancellationToken)
    {
        var request = new QuoteRequest(origin, destination, weightKg);

        var handler = services.GetKeyedService<IShipmentHandler>(carrier);
        if (handler is null)
            return $"Unknown carrier '{carrier}'. Call list_carriers for valid values.";

        var quote = handler.GetQuote(request);
        if (!quote.Success)
            return $"Cannot ship: {quote.Reason}";

        string? contents = null;
        decimal? declaredValue = null;

        if (ExpressHandler.IsInternational(request))
        {
            if (server.ClientCapabilities?.Elicitation is null)
                return "This is an international shipment and customs information is required, "
                     + "but this client does not support interactive prompts. "
                     + "Please use a client with elicitation support.";

            var schema = new RequestSchema
            {
                Properties =
                {
                    ["contents"] = new StringSchema
                    {
                        Description = "Short description of what is inside the package."
                    },
                    ["declaredValue"] = new NumberSchema
                    {
                        Description = "Declared customs value in USD."
                    }
                }
            };

            var response = await server.ElicitAsync(new ElicitRequestParams
            {
                Message = "This is an international shipment. Customs information is required.",
                RequestedSchema = schema
            }, cancellationToken);

            if (response.Action != "accept")
            {
                logger.LogInformation("Elicitation declined for international shipment");
                return "Shipment cancelled: customs information was not provided.";
            }

            contents = response.Content?["contents"].GetString();
            declaredValue = response.Content?["declaredValue"].GetDecimal();
        }

        var id = $"SHP-{Random.Shared.Next(100000, 999999)}";
        logger.LogInformation("Shipment {Id} created with {Carrier}, customs data: {HasCustoms}",
            id, carrier, contents is not null);

        return contents is null
            ? $"Shipment {id} created. {carrier}: ${quote.Quote!.Price:F2} USD, {quote.Quote.EstimatedDays} days."
            : $"Shipment {id} created. {carrier}: ${quote.Quote!.Price:F2} USD, {quote.Quote.EstimatedDays} days. "
            + $"Customs: {contents}, declared ${declaredValue:F2}.";
    }
}
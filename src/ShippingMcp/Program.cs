using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();

[McpServerToolType]
public static class ShippingTools
{
    [McpServerTool]
    [Description("Gets a shipping quote for a package.")]
    public static string GetQuote(
        [Description("Origin postal code.")] string origin,
        [Description("Destination postal code.")] string destination,
        [Description("Package weight in kilograms.")] double weightKg)
        => $"Quote from {origin} to {destination} for {weightKg}kg: $24.50 USD, 3-5 business days.";
}
using ModelContextProtocol.Server;
using System.ComponentModel;
using ShippingMcp.Shipping;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddKeyedSingleton<IShipmentHandler, ExpressHandler>("express");
builder.Services.AddKeyedSingleton<IShipmentHandler, EconomyHandler>("economy");
builder.Services.AddKeyedSingleton<IShipmentHandler, InternationalHandler>("international");

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
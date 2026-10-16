using ModelContextProtocol.Server;
using System.ComponentModel;
using ShippingMcp.Shipping;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => { options.Stateless = false; })
    .WithToolsFromAssembly();

// Única lista de carriers: las tools la leen del contenedor.
builder.Services.AddKeyedSingleton<IShipmentHandler, ExpressHandler>("express");
builder.Services.AddKeyedSingleton<IShipmentHandler, EconomyHandler>("economy");
builder.Services.AddKeyedSingleton<IShipmentHandler, InternationalHandler>("international");
builder.Services.AddKeyedSingleton<IShipmentHandler, FreightHandler>("freight");

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
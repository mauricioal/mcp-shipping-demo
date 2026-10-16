# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A teaching demo for a talk: a .NET 10 MCP server over a deliberately trivial shipping domain. The business rules are not the point; the patterns for an API whose consumer is a language model are. README.md states the thesis, and NOTES.md records each design decision and why it was made. Read NOTES.md before changing one of those decisions.

## Commands

```bash
dotnet build                            # from the repo root (ShippingMcp.slnx)
dotnet run --project src/ShippingMcp    # serves MCP at http://localhost:5103/mcp
npx @modelcontextprotocol/inspector     # connect with transport "Streamable HTTP" to http://localhost:5103/mcp
```

There is no test project and no linter. Behaviour is verified by calling tools in the MCP Inspector (Tools tab for results, Protocol tab for message direction) and reading the server's console logs.

- Restart the server after a code change. A stale process still serving the previous build has caused confusing 404s before.
- Disconnect and reconnect the Inspector after changing the tool surface: the client fetches the tool list at handshake and caches it.

## Architecture

- `Program.cs` wires everything: `AddMcpServer().WithHttpTransport(...).WithToolsFromAssembly()`, one keyed singleton registration per carrier, and `MapMcp("/mcp")`. Those registrations are the only carrier list.
- `Shipping/` is the domain. `IShipmentHandler` (`Carrier`, `CanHandle`, `GetQuote`) has one implementation per carrier in `Handlers.cs`, registered with keyed DI under its carrier code. `QuoteResult` is a typed success-or-reason result.
- `Tools/` is the MCP surface. Every `[McpServerToolType]` class is discovered automatically, and the SDK snake-cases method names (`GetBestQuote` becomes `get_best_quote`). Tools resolve handlers by key at the call site; NOTES.md explains why this differs from the production pattern it is modelled on.
    - `ShippingTools`: `list_carriers`, `get_quote` (the caller picks the carrier) and `get_best_quote` (the server filters handlers with `CanHandle` and returns the cheapest). `list_carriers` and `get_best_quote` read the carriers from the container with `GetKeyedServices<IShipmentHandler>(KeyedService.AnyKey)`, so adding a carrier means one handler class and one registration, never a second list.
    - `ShipmentTools`: `create_shipment`, which asks the client for customs data through elicitation on international shipments.
- "International" is a toy rule: origin and destination postal codes of different lengths (`ExpressHandler.IsInternational`).

## Conventions

These patterns are what the talk teaches, so new code follows them:

- **Descriptions are the contract.** Every tool and every parameter has a `[Description]`, because it is all the model sees. Elicitation schema fields carry descriptions too.
- **Nothing throws toward the model.** Tools return strings and handlers return `QuoteResult.Unsupported(reason)`. Every failure message tells the model what to do next: which tool to call, which values are valid, which limit applies. When the client can't elicit, `create_shipment` returns a sentence rather than throwing `McpException`.
- **The transport is stateful on purpose.** `options.Stateless = false` is required for elicitation; stateless mode throws `Elicitation is not supported in stateless mode`. Don't change it.
- Tools log through `ILogger<T>` with structured message templates.
- Code comments are short and in Spanish; README.md and NOTES.md are in English.

SDK notes (`ModelContextProtocol.AspNetCore` 2.1.0): a tool takes `McpServer` as a parameter (there is no `IMcpServer`), and elicitation schema types such as `StringSchema` are nested under `ElicitRequestParams` (`using static ModelContextProtocol.Protocol.ElicitRequestParams;`).

## Git and GitHub

- The numbered branches (`00-empty`, `01-first-tool`, `03-handlers`, `04-elicitation`) are teaching snapshots that each run on their own, and `main` is `04-elicitation` plus later feature work (the `freight` carrier). Don't modify them; new work branches off `main`.
- Commit messages use conventional prefixes: `feat:`, `docs:`, `chore:`.
- When behaviour or the tool surface changes, update README.md and NOTES.md in the same change.
- The repo root holds untracked `.pptx` and `.mp4` files from the talk. Never commit them: stage files by name, never with `git add -A` or `git add .`.
- The GitHub repo is `mauricioal/mcp-shipping-demo`; use `gh` for issues and pull requests.

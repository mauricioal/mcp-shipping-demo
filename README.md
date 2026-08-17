# mcp-shipping-demo

A .NET 10 Model Context Protocol server, built branch by branch over a toy
shipping domain.

The domain is deliberately trivial — the point is not shipping. The point is what
changes architecturally when the consumer of your API is a language model instead
of a frontend.

## Why this exists

A frontend developer reads your docs, learns your contract, and writes code
against it once. A language model reads your tool descriptions at runtime, decides
which tool to call, and has to recover on its own when it gets something wrong.

That difference reaches further into the design than it first appears:

- **Your tool descriptions are your API contract.** No separate documentation
  exists. What you write in `[Description]` is what the model reasons over.
- **Errors are instructions, not status codes.** A 400 is written for a human
  reading a network tab. Your error message is read by a model that will retry.
- **Who picks the provider?** The caller can name it, or the server can infer it.
  Both are defensible, and the choice changes your whole error surface.
- **The server can ask questions.** Elicitation inverts the direction of the
  conversation — and it only works if you made the right transport decision three
  branches earlier.

## Branches

Each branch is one step and runs on its own. Later branches contain everything
from earlier ones.

| Branch | What it adds |
|---|---|
| `00-empty` | Bare ASP.NET Core host — the starting point |
| `01-first-tool` | One MCP tool, Streamable HTTP transport, mapped at `/mcp` |
| `03-handlers` | Three carriers behind `IShipmentHandler`, keyed DI, typed results instead of exceptions |
| `04-elicitation` | The server asks for customs data mid-call on international shipments |
| `main` | Same as `04` |

`02-descriptions` is intentionally missing. The tool-description exercise only
works once there are several tools for a model to confuse — it is presented live
with two versions of the same description, not as versioned code.

`05-production` is discussed rather than demoed. Structured logging is already
wired into the tools in `03`; rate limiting, resilience and Aspire orchestration
are covered in the talk without code.

## Running it

Requires the .NET 10 SDK and Node (for the Inspector).

```bash
dotnet run --project src/ShippingMcp
```

In another terminal:

```bash
npx @modelcontextprotocol/inspector
```

Add a server with transport **Streamable HTTP** pointing at
`http://localhost:5103/mcp`, connect, and the tools appear under the Tools tab.

## Things worth trying

- Call `get_quote` with a carrier that does not exist. Note that you get a usable
  sentence back, not an exception — including the list of valid carriers.
- Call `get_best_quote` with a 60kg domestic package. No carrier can take it, and
  the message points you at the other tool to find out why.
- Call `create_shipment` with postal codes of different lengths (the toy rule for
  "international"). The server interrupts and asks for customs information. Watch
  the Inspector's Protocol tab: `tools/call` stays **pending** while
  `elicitation/create` travels **server → client**.
- Then call it with same-length postal codes. Nothing is asked. Same tool, same
  call shape — the server only interrupts when it actually needs something.

## One trap, documented

The SDK docs recommend `options.Stateless = true` for HTTP transport. It scales
better and there is no session state to manage. It also silently removes
elicitation, because a server-to-client request needs a session to travel back
through:

```
System.InvalidOperationException: Elicitation is not supported in stateless mode.
```

Stateless vs stateful is not a tuning knob — it decides what your API is capable
of. This repo sets it explicitly.

More design notes and reasoning in [NOTES.md](NOTES.md).

## License

MIT.
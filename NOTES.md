# NOTES.md

Design decisions and lessons learned while building this demo, branch by branch.
Written as I go so the reasoning survives until presentation day.

---

## Purpose of this repo

A teaching demo for a talk on what changes architecturally when the consumer of
your API is an LLM instead of a frontend. The domain (multi-carrier shipping
quotes) is deliberately trivial — the patterns are the content, not the business
rules.

Each branch is one pedagogical step. Every branch compiles and runs on its own.

---

## Branch order

| Branch | Content |
|---|---|
| `00-empty` | Bare ASP.NET Core host |
| `01-first-tool` | One MCP tool, HTTP transport |
| `03-handlers` | Per-carrier handlers, keyed DI, typed results |
| `02-descriptions` | Tool description exercise |
| `04-elicitation` | Server asks for missing fields |
| `05-production` | Logging, rate limiting, resilience, Aspire |

**`02` is built after `03` on purpose.** The tool-description exercise only works
when there are several tools for the model to confuse. With a single `get_quote`
there is nothing to choose wrong. Keep the numbering, explain the order in the
README.

---

## `00-empty`

Nothing but `dotnet new web` on .NET 10. Exists so the first branch of the live
demo has somewhere to start from.

---

## `01-first-tool`

**Stack:** `ModelContextProtocol.AspNetCore`, Streamable HTTP transport, endpoint
mapped at `/mcp`.

### Decision: stateful, not stateless

The SDK docs recommend `options.Stateless = true`. Left at the default (stateful)
because stateless mode disables server-to-client requests — which is exactly what
elicitation needs in `04`. Deciding this in branch `01` avoids having to undo it
later.

### Decision: hardcoded return value

The first tool returns a fixed string. The point of this branch is that the
protocol works, not that the logic does.

### Observation worth presenting

The SDK turned `GetQuote` into `get_quote` and rendered every `[Description]`
attribute as the field help in the client. No API documentation was written, yet
the full contract the model reads is there. **The description *is* the API
contract** — that is the thesis of the whole talk, visible in branch 1.

Also: the tool accepted `4568` and `4126` as postal codes without complaint. A
frontend would have blocked that with a regex. Here the only filter is how well
the parameter is described.

### Errors are instructions, not HTTP codes

Two failures while setting this up, both worth showing:

- **404 on `/mcp`** — the running process was still the previous build. The edit
  had not been saved.
- **405 on `/`** — the Inspector POSTs; the template's `MapGet("/")` only accepts
  GET.

Neither of those tells a language model anything actionable. That is the point of
the segment: a status code is written for a human reading a network tab, and the
consumer here is a model that will retry.

### Tool list caching

After adding tools, the Inspector kept showing the old surface until the server
was disconnected and reconnected. The tool list is fetched at handshake and
cached. **A client that has already connected does not know your API surface
changed.** Versioning and refreshing tools is a real production concern —
belongs in the production segment.

---

## `03-handlers`

**Shape:** `IShipmentHandler` with three implementations (express, economy,
international), registered with keyed DI, resolved by key at the tool call site.

### Decision: keyed DI resolved at the call site

The production system this demo is modelled on does it differently: the
discriminator selects a *consumer object*, and the handler is a constructor
dependency already captured inside it. Every keyed lookup happens inside a
registration factory lambda, not at any call site.

That is the better answer in production — it makes the handler choice part of
object composition rather than a runtime branch. It was rejected here because the
indirection adds a layer that cannot be explained in ten minutes and obscures the
pattern being taught.

**Say this out loud in the talk.** "I resolved at the call site for clarity; the
production pattern resolves the consumer by key and captures the handler inside,
for these reasons." That distinction is what separates a talk from a tutorial.

### Decision: typed result instead of exception

`QuoteResult` carries either a quote or a reason. Nothing throws toward the model.

This came from reading the reference implementation, where the same problem is
solved both ways in one codebase: the handler path throws a domain exception,
while a sibling interface returns a typed `Unsupported` result. For an LLM
consumer the difference is large — an exception becomes a protocol error and the
model gets nothing it can act on; a typed result it can read and correct itself
from.

Unknown carrier returns the list of valid ones. No eligible carrier returns a
message pointing at the other tool. **Every failure path tells the model what to
do next.**

### Decision: two tools, one question

- `get_quote(carrier, ...)` — the caller picks the carrier
- `get_best_quote(...)` — the server infers it

Both exist so the talk can ask the real design question: **who chooses the
provider, the model or the server?**

The reference system answers this both ways too. One path takes the discriminator
from a tool parameter. Another takes it from the output of an upstream inference
pipeline — the routing key is not supplied by anyone, it is *derived* from the
input documents.

That second case generalises into the most interesting idea available here:

> When the consumer is an LLM, the routing can be inferred too. And if your
> discriminator is inferred rather than supplied, everything downstream changes:
> you cannot default, you have to fail closed, and you need traceability of *why*
> a route was chosen.

With deterministic routing, the log says what happened. With inferred routing,
the log has to say why. That is an observability requirement that does not exist
in a conventional API — it links this segment to the production segment.

The demo's inference is trivial on purpose (weight and destination rules, no
model involved). The *mechanism* is uninteresting; the *design question* is
identical.

### Third selection style seen in the reference

A sibling pipeline injects `IEnumerable<IStrategy<TRequest>>` and filters by
predicate at runtime — a third way to solve the same structural problem in one
codebase. `CanHandle` on the handler interface is the nod to it here, and it is
the shape to adapt (not copy) for `04`.

### Deliberately not fixed

No carrier accepts over 50kg domestically. That gap is what makes the "no carrier
can handle this" path demonstrable. Raising the international ceiling would make
the domain feel more complete but produces an identical demo result — not worth
the time.

---

## Open questions for later branches

- How to version a tool surface once clients have cached it
- Whether `CanHandle` duplicating the guard clauses inside `GetQuote` is worth
  refactoring, or whether the duplication is clearer for teaching
- What to log about an inferred routing decision so it is reconstructable
- Testing: unit tests on handlers are obvious; how to test the tool surface
  itself is not — likely stays an open question in the talk

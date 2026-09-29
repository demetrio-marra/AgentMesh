## Context

See proposal.md for motivation and the stream-request-events spec for wire behavior. `RequestsController` currently calls `IAppInstance`: sync runs wait for `AppInstance.ProcessRequest`/`SummarizeAsync`; async runs configure `CallbackNotifierContext` in a new execution scope. `AgentMeshRuntime.LoadAgentMesh` binds a scoped `IWorkflowProgressNotifier` to `CallbackWorkflowProgressNotifier`. Pipeline steps already call that interface. The CLI calls async endpoints, hosts `CallbackListenerService`, and correlates results with `PendingRequestRegistry`. The configured architecture-baseline file is absent; the current source and the archived `replace-api-host-with-runtime` design establish the live Runtime/plugin boundary.

## Goals / Non-Goals

**Goals:** Deliver progress and terminal results on one response with request isolation, pre-stream HTTP validation/routing failures, and cancellation. Preserve all plugin-facing interfaces and existing sync/async behavior. Reuse existing callback payload shapes where practical and avoid extra files beyond the smallest necessary transport types.

**Non-Goals:** Streaming partial LLM tokens, making async callbacks stream through the same transport, retrying interrupted streams, or changing pipeline scheduling/optimization.

## Decisions

### Use SSE framing over an ordinary streamed POST response

The two new controller actions accept existing `ProcessRequestApiInput` and `SummarizationApiInput` bodies and return `text/event-stream; charset=utf-8`. Each frame uses `event: <name>\ndata: <single-line JSON>\n\n` with camelCase JSON matching the existing API serializer. Existing callback payload fields provide the shape for progress and terminal data; every payload includes one `requestId`. Flush after each frame, then end immediately after a single terminal event. No callback URL fields belong in these inputs. Document event types and error responses in OpenAPI; do not alter the old routes.

Alternative: JSON lines. Rejected because SSE provides explicit event names and established framing with minimal parsing code, even though the request is a POST and the CLI uses HttpClient rather than browser EventSource.

### Resolve and run in one scoped execution without extending plugin contracts

Add additive stream entry points on the concrete framework runner (`AppInstance`), keeping `IAppInstance`, `IChatRequestPipeline`, `ISummarizationPipeline`, and `IWorkflowProgressNotifier` unchanged. A stream run creates its own execution scope, resolves the pipeline synchronously before writing headers, sets a scoped event sink and generated request ID, and then invokes the same existing private chat/summarization execution methods with `HttpContext.RequestAborted`. The controller obtains the concrete runner for the stream actions; old actions keep their existing interface injection. Keep the scope alive until the run and terminal write finish; dispose it on routing failures too.

The scoped notifier implementation checks whether a stream sink is set: for this request it forwards workflow start/step notifications to the sink; otherwise it follows the existing callback/no-op behavior unchanged. Put its small scoped transport state next to the notifier instead of changing the shared plugin contract. The controller owns SSE serialization and flushing; the runner never takes an ASP.NET response dependency.

Alternative: wrap a synchronous run in the controller and resolve a second runner scope for notifications. Rejected because pipeline and notifier would be in different scopes and the progress sink would not see pipeline events. Alternative: add new methods to `IAppInstance` or `IWorkflowProgressNotifier`. Rejected because third-party implementations could break at compile time.

### Distinguish pre-stream failure from execution failure

Validate model input and resolve the pipeline before setting SSE content type or flushing response headers; use existing `PipelineRoutingException` mapping for 503 and model-validation behavior for 400/401. After stream starts, serialize an error event instead of trying to change HTTP status. On client abort, propagate cancellation and skip further writes. Terminal success includes the complete workflow result or summarization content/time, so conversation ownership stays on the client. Do not invoke callback URLs in stream mode.

Alternative: start streaming immediately and emit routing errors as events. Rejected because existing API error semantics would become inconsistent and clients could not distinguish invalid input by HTTP status.

### Move CLI to direct stream consumption

`AgentMeshApiClient` sends authenticated POST with `ResponseHeadersRead`, reads framed events incrementally with request cancellation, and deserializes known payloads using the same JSON naming policy as the server. Disable the client's default finite HTTP timeout for long-running streams; use its cancellation token for explicit cancellation. `UserConsoleInputService` calls the existing `ConsoleWorkflowProgressNotifier` when each progress event arrives and updates conversation state only on a valid terminal success. Unexpected EOF, malformed/unknown terminal data, and stream errors report failure without applying partial results. Ctrl+C cancels the outgoing request; remove callback listener, URL factory, registry, and callback configuration from CLI startup and active settings, with no changes to server-side async callbacks.

Alternative: keep the callback listener as a fallback. Rejected because it preserves the inbound firewall requirement and violates the CLI-only-stream goal.

## Risks / Trade-offs

- [Proxy buffers SSE or times out idle streams] -> Flush every event; document that deployments must permit long-lived, unbuffered responses and configure proxy idle timeouts appropriately.
- [A disconnect races with completion] -> Request cancellation is passed to pipeline execution and checked before writes; CLI applies state only when it receives a complete success event while still active.
- [Event serialization/write fails] -> Stop the run and release the scope; avoid writing a second terminal event on a broken connection.
- [No pipeline event for a prolonged step] -> A proxy may time out; operational proxy settings are required, without introducing heartbeat machinery into pipeline execution.

## Migration Plan

1. Release Runtime/framework stream support first; preserve existing routes so old clients continue to work.
2. Release CLI pointed at a stream-capable plugin web host. Remove obsolete CLI callback listener settings and update CLI/Runtime documentation with stream contracts and proxy requirements.
3. Manually verify both new routes, existing sync/async callbacks, and CLI on the built host. Roll back the CLI to the previous version if the deployed Runtime does not expose stream routes; server routes remain additive.
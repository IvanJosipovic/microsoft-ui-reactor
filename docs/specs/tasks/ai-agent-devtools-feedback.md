# AI Agent Devtools — Field Feedback

Reference: [024-ai-agent-devtools.md](../024-ai-agent-devtools.md) · [implementation tasks](ai-agent-devtools-implementation.md)

Field notes from an agent (Claude Code, Opus 4.7) exercising the MCP surface end-to-end against `samples/Reactor.TestApp` on Windows 11 ARM64. Every issue below is backed by an actual HTTP request/response against the live `--devtools run` server; suggestions are grounded in where the agent got confused, had to try multiple selectors, or had to read the framework source to recover.

Session: ~30 tool calls, exercising `version`, `components`, `switchComponent`, `windows`, `tree` (summary + full + includeReactorSource), `click`, `invoke`, `toggle`, `focus`, `type`, `waitFor`, `screenshot`, `fire`, `state`. Skipped: `reload`, `select`, `scroll` (out of scope for this pass).

## Resolution status

| Item | Status | Note |
|---|---|---|
| B1 node-id collisions | fixed | `TreeWalker` always threads the immediate parent; `NodeIdBuilder` still anchors to the nearest stable ancestor when one exists. Regression tests added. |
| B2 AggregateException swallow | already fixed | `OnDispatcher` uses `ContinueWith`+`ManualResetEventSlim`; `Devtools_UnknownSelectorStructuredError` self-host fixture pins the round-trip. |
| B3 screenshot DPI | fixed | `ScreenshotCapture` scales DIP→px via `GetDpiForWindow` before stacking the client offset. |
| B4 `Run<T>` ignored | fixed | `typeof(TRoot)` threads through `TryRunDevtools` → `RunRunSubverb` as the default component when `--component` is absent. |
| B5 `[name=]` can't match Button text | fixed | `SelectorResolver.NameMatches` falls back to the same text extraction the tree walker reports (TextBlock/TextBox/Button/ContentControl). |
| B6 `waitFor` timeout logged ok | fixed | `McpDispatcher.Invoke` inspects the return value for `ok:false` and logs as `err`. |
| B7 TestApp sample uses `preview:` | fixed | `samples/Reactor.TestApp`, `samples/apps/headtrax`, `samples/apps/validation-showcase` all migrated to `devtools:`. |
| U6 `fire` accepts `Render` | fixed | `DevtoolsFireTool.ForbiddenMethods` refuses lifecycle/hook-owned names with a `forbidden-method` structured error. |
| U7 missing MCP `initialize` | fixed | Dispatcher now handles `initialize`, `ping`, `notifications/*`, `resources/list`, `prompts/list`. Self-host `Devtools_InitializeHandshake` fixture pins the shape. |
| U1 default-component surprise | covered by B4 | |
| U2 ambiguity payloads dropped | covered by B2 | Agent UX on the candidates payload (per-match suggested selector) is still worth a follow-up. |
| U3 tree noise from templated parts | deferred | Interactive-only view is a Phase 4 DX polish; not in this pass. |
| U4 `TextBox[1]` DFS vs. sibling index | deferred | Documentation fix; will land with Phase 3's selector-grammar pass. |
| U5 stale window id after `switchComponent` | deferred | Low severity; needs window-id rotation in `WindowRegistry` + tests. |

---

## Bugs

### B1. Every non-root node gets a one-segment id — massive id collisions

**Where:** `src/Reactor/Hosting/Devtools/NodeIdBuilder.cs:37-67`, `TreeWalker.cs:131-169`.

**What I saw:** `FormDemo` renders two distinct TextBoxes (Name, Email) in two separate `FormField`-style StackPanels. The tree walker emitted both with the same id `r:preview-accentbadge/StackPanel~/TextBox[1]`. Two `Popup` nodes in `DemoApp` both got `r:preview-accentbadge/FlexPanel~/Popup[8]`.

Consequence: `tools/call type` with `{selector: "TextBox"}` returned **`Selector matched multiple elements`**. The id system is meant to be the escape hatch when type-paths are ambiguous, but the ids themselves collide. I had to fall back to `TextBox[1]` (type-path index) and hope the walker's iteration order matched what I wanted — which meant I typed "Chris" into the Email field instead of Name on my first try.

**Root cause:** `NodeIdBuilder.BuildLocal` walks `node.StableAncestor`, which is only advanced when a node has an `AutomationId` or a `ReactorSource`. Both are always null in v1 (see `TreeWalker.cs:135` — `ReactorSource: null`, and Reactor factories never set `AutomationProperties.AutomationId`). The loop at `NodeIdBuilder.cs:51-63` never enters, so `segments` stays as a single entry `[{TypeName}[{SiblingIndex}]]`, and every node's id reduces to `{Root}~/{Type}[{siblingIndexInDirectParent}]`. Two TextBoxes that are each the 2nd child of their own StackPanel → identical ids.

**Fix options:**
- In the absence of a stable anchor, still walk the full parent chain (use `parent` as `StableAncestor` fallback). This is the minimal change and restores uniqueness.
- Or: seed every Reactor-produced element with an `AutomationId` derived from the render-tree path. That also makes `#id` selectors actually usable.

### B2. Errors from dispatcher-thread tools come back as `InternalError` with an `AggregateException` message

**Human note:** This may have been fixed, I just saw another agent dealing with this (maybe)... verify this is still an issue before fixing

**Where:** `src/Reactor/Hosting/Devtools/DevtoolsMcpServer.cs:197-223` (`OnDispatcher<T>`).

**What I saw:**

```json
{"error":{"code":-32603,"message":"One or more errors occurred. (Selector matched no elements.)"}}
```

The code is `-32603 InternalError`; the expected code for a resolver miss is `-32000 ToolExecution` (`SelectorResolver.cs:112-114`). The original `McpToolException.Data` (which contains `{code: "unknown-selector"}`) is also lost.

**Root cause:** `Task.Wait(int)` rethrows `AggregateException` when the task is faulted — before the `IsFaulted`-unwrap branch at `DevtoolsMcpServer.cs:217-221` can execute. The comment on the method (line 193-195) says exceptions "surface with their original type… not wrapped in AggregateException" — but that branch is unreachable. Every error that originates on the UI dispatcher (which is ~all the interesting tools) hits `catch (Exception)` in `McpDispatcher.Dispatch` (`McpDispatcher.cs:74-81`) and comes out as `-32603` with the AggregateException prose.

**Fix:** replace the `if (!tcs.Task.Wait(timeoutMs))` with a non-throwing wait. For example:

```csharp
if (!((IAsyncResult)tcs.Task).AsyncWaitHandle.WaitOne(timeoutMs))
    throw new McpToolException("Dispatcher call timed out.");
```

or `Task.WaitAny(new[] { tcs.Task }, timeoutMs)`, both of which are non-throwing. The existing `IsFaulted` / `ExceptionDispatchInfo` branch then works as written.

There's a test-coverage gap too: no current test asserts that a dispatcher-thrown `McpToolException` round-trips with its `Code` and `Data` preserved. Worth adding one alongside the fix.

### B3. Selector-scoped screenshot crop is off by the content offset (or DPI)

**Where:** `DevtoolsUiaTools.cs:248-258`, `ScreenshotCapture.cs:18-79`.

**What I saw:** `screenshot` for `selector: "r:…/Button[2]"` (the visible "+ 1" button in the Counter demo) returned `bounds: {x:160, y:177, width:43, height:32}`. The rendered PNG showed the **"Cour"** of "Current count" — the text that sits *above* the button row. The button itself is visually at roughly (195, 240) in the window, so the crop is off by ~(35, 60) pixels. Full-window screenshots are correct.

Selector resolution was correct — `click` on the same selector incremented the counter. So the bug is in the crop math, not the selector.

**Likely cause:** `fe.TransformToVisual(w.Content).TransformPoint((0,0))` returns DIPs relative to `w.Content`. But `ScreenshotCapture` applies that offset atop a window bitmap sampled in *physical pixels* via `PrintWindow`. On a scaled display (125% / 150%) the crop rect is multiplied wrong. Even at 100% scale my crop was off, which suggests there's also an un-accounted content-area offset between `w.Content` and the client-rect origin.

**Suggested fix:** look up the window's current DPI scale (`GetDpiForWindow`) and multiply the crop rect by `scale / 96`. Also verify that `w.Content.TransformToVisual(null)` isn't needed to reach client-origin coordinates before adding the chrome offset.

### B4. `--devtools run` ignores `Run<T>` and picks the first alphabetical Component

**Where:** `src/Reactor/Hosting/ReactorApp.cs:268-295` (`RunRunSubverb`), `ReactorApp.cs:431-439` (`FindAllComponentNames`).

**What I saw:** I launched TestApp via `--devtools run --mcp-port 39417`. The window came up showing a tiny colored rectangle titled "Preview — AccentBadge". That's the nested `AccentBadge` helper component inside `ContextDemo.cs`, which wins alphabetically. The TestApp's `App.cs` clearly says `ReactorApp.Run<DemoApp>(...)`, so I expected `DemoApp` to be the default.

I spent my first few minutes thinking the app was broken before figuring out I needed to call `switchComponent` to `DemoApp`. An agent with less patience would have filed a bug against the reconciler.

**Fix:** when the host passes `Run<T>`, thread `typeof(T)` into `DevtoolsCliOptions` (or a new field on the host context) and use it as the default when `--component` isn't specified. Current behavior silently discards the explicit choice the host developer made.

Alternative (less ideal): emit an explicit log line like `"[devtools] No --component passed; defaulting to 'AccentBadge' (alphabetical). Pass --component DemoApp to match Run<DemoApp>."`

### B5. Reactor `Button("text", …)` has no automation name — `[name='text']` never works

**Where:** Button factory in `src/Reactor/**/Factories*.cs` (whichever registers `Button(string, Action)`); tree in `TreeWalker.ExtractText` (`TreeWalker.cs:197-204`).

**What I saw:** my very first click attempt used the most obvious selector — `[name='+ 1']`. It returned `Selector matched no elements`. Three button-clicks wasted before I dropped to node-ids. The walker's `text` field on the Button does get populated (`Button.Content?.ToString()` → "+ 1"), but `AutomationProperties.Name` is unset, so the `[name=]` selector path can't see it.

This is the path a human agent would try first. It should work.

**Fix options:**
- When a Reactor Button has string content, auto-apply `AutomationProperties.SetName(btn, content)`.
- Or: extend `SelectorResolver` so `[name='X']` also matches against the `TreeNode.Text` field the walker already computes. Keep `[name=]` semantics consistent with what `tree` reports — if `tree` shows `text: "+ 1"`, then `[name='+ 1']` should match that node.

### B6. `waitFor` timeouts log as `ok` in the rolling log

**Where:** `src/Reactor/Hosting/Devtools/McpDispatcher.cs:110-131`, `DevtoolsUiaTools.cs:505-525`.

**What I saw:** `waitFor` with an impossible predicate and a 500 ms timeout returned `{ok:false, reason:"timeout", observed:{…}}`. The rolling log wrote `2026-04-18T16:13:38.785Z  waitFor  -  558ms  ok  0`. A grep for `err` misses it.

The tool deliberately returns a structured soft-failure rather than throwing; the log records success because the dispatcher didn't catch an exception. The two perspectives disagree.

**Fix:** let tool handlers return a sentinel (or inspect the returned object for `ok:false`) so the logger records timeouts as `err`. Or accept this and document it — but if this feature is meant for post-hoc debugging of agent sessions, the log is the primary artifact and it currently lies.

### B7. TestApp's own `App.cs` still uses the deprecated `preview:` parameter

**Where:** `samples/Reactor.TestApp/App.cs:5-9`.

**What I saw:** every launch prints `[reactor] 'preview:' is deprecated; use 'devtools:'.` Minor noise, but it suggests Phase 4 didn't update the in-tree samples.

**Fix:** update `App.cs` (and probably the other `samples/` roots) to use `devtools: true` so the deprecation warning is only hit by out-of-tree callers who copy-pasted from old docs.

---

## UX confusion — where I got stuck or had to try multiple things

### U1. The default component is surprising

Already covered under B4. Worth flagging separately because it's the *first* thing a new agent encounters and it sets the tone. I thought the app was broken. Even a one-line stderr saying "defaulting to AccentBadge alphabetically" would have saved me.

### U2. Selector ambiguity errors don't help me pick a better selector

When `click` fails with "Selector matched multiple elements", the `data` payload *does* contain `candidates` (`SelectorResolver.cs:118`). But because of B2, that payload gets dropped on the floor and I only see the string message. So when my `invoke CheckBox` selector matched two CheckBoxes, I had no hint on what to try next beyond "be more specific."

Once B2 is fixed this will resolve itself, but the candidates array should also include a **suggested canonical selector** per match — ideally the node id the next call could use. Right now the agent has to re-walk the tree and figure out disambiguation manually.

### U3. The tree is noisy with templated parts

A full `tree` call emits ~50+ nodes for the Counter demo, most of which are template internals (TickBar×6, Rectangle×6, Grid×10, ContentPresenter×8, Path, Thumb, etc.). The agent has to filter out those to find the three Buttons and the Slider it actually wants.

**Suggestion:** add an `interactives` (or `tree` with `view: "interactive"`) tool that returns only nodes with a UIA pattern (Invoke / Toggle / Value / SelectionItem / Scroll) plus their parent container. Each result should include a stable, suggested selector. That's what an agent actually wants 80% of the time.

### U4. Type-path selector `TextBox[1]` disambiguates by walker iteration order, not by parent

I expected `TextBox[1]` to mean "the TextBox whose parent sibling-index is 1". It actually means "the second TextBox the walker encounters in DFS order" — different semantics entirely. Happened to work here but is fragile: inserting a new TextBox anywhere earlier in the tree would silently rebind `TextBox[1]` to a different control.

**Suggestion:** either document this clearly in the spec's "Selector grammar" section, or change the semantics so the index refers to sibling-index (matching the node id format). Right now the two conventions disagree inside the same tool.

### U5. `switchComponent` leaves the window id stale

After `switchComponent → DemoApp`, `windows` still returned `id: "preview-accentbadge"`. Stable ids are a reasonable design choice, but the stale id becomes part of every node id in the subsequent `tree` call (e.g. `r:preview-accentbadge/FlexPanel~/Button[2]`). Cosmetic, but it made me second-guess whether my switch had landed. A fresh id on component-swap, or a log line confirming the active component + window pair, would help.

### U6. `fire` on the root component allows calling *any* method, including `Render`

Totally as-designed per the code, but I tripped over it. I fired `Render` on CounterDemo to prove the tool worked; the runtime re-rendered outside the reconciler's awareness. No crash, but I could have corrupted hook state. A safer default would be to restrict `fire` to methods with a specific attribute (`[McpFireable]`) or to methods whose return type is `void` — and reject calls to `Render`, `OnInitialized`, `OnDisposed`, etc. by name.

### U7. I had to read the source to learn that `initialize` isn't implemented

My first request was the standard MCP handshake `initialize`, which returned `Method not found`. The server speaks a subset (`tools/list`, `tools/call`, direct tool names). That's probably fine, but: an MCP-literate client will hit the init step first and bail out. Either implement a minimal `initialize` response, or document the bare-subset protocol prominently in the README.

**Human note:** Implement the whole MCP protocol, even if we don't really need it, to match expectations


---

## Suggestions for making this easier to use

These are "if I were designing this for myself" asks — not bugs, but DX improvements that would've saved round-trips.

1. **Return suggested selectors on every node.** Each `TreeNode` should include a `selector` field that the agent can paste back. Today I have to reassemble `r:<win>/<localId>` myself. For ambiguous matches, include candidates' suggested selectors in the error payload.

2. **Add an `inspect` / `interactives` tool.** Returns only nodes with a UIA pattern + their text + their suggested selector. Covers 80% of agent use cases in one call. Full `tree` is still there for layout debugging.

3. **Attach visible text to Button automation name.** A Reactor `Button("Save", …)` should be selectable via `[name='Save']`. Currently isn't. See B5.

4. **Make ids unique.** See B1. Without this, the node-id selector path is unreliable — the fallback becomes worse than type-paths.

5. **Round-trip structured errors properly.** See B2. Dispatcher-thread errors lose their `code` and `data` on the wire, which is exactly when structured info is most useful (selector resolution lives on the dispatcher).

6. **Log soft failures as `err`.** See B6. The rolling log is the agent session's flight recorder — it should reflect outcome, not just whether the handler threw.

7. **Default component should honor `Run<T>`.** See B4. The host developer already declared intent; ignoring it is a footgun.

8. **Print the curl command on startup.** The `[devtools] MCP serving on http://127.0.0.1:39417/mcp` line is useful; a follow-up line like `curl -s http://127.0.0.1:39417/mcp -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'` would let a human sanity-check in one paste.

9. **Consider `findAll` / `queryAll`.** When I want to count TextBoxes in FormDemo, "Selector matched multiple elements" is the *happy path* — not an error. Give me a list.

10. **Document the protocol subset.** The devtools server implements `tools/list` + `tools/call` + direct-by-name dispatch but not `initialize` / `notifications/*`. A one-paragraph note in the README prevents the standard-MCP-client surprise.

---

## What worked well

For balance: the things that were pleasant.

- **`screenshot` full-window is reliable and fast** (~50 ms) and returns PNG base64 inline — great for multimodal flows.
- **`waitFor` happy path is crisp** — 4 ms elapsed to see `Current count: 3` after three clicks. The `observed` payload on timeout tells me *why* the predicate didn't match, which is the right abstraction.
- **`state` is exactly the right shape** — `useState` index / valueType / value, with primitives passed through and complex objects shaped, no value exfiltration. The `instanceId` is a nice touch.
- **`switchComponent` is fast enough to use casually** (~1 s rebuild-free). Made the "hop between demos to test different widgets" flow painless.
- **Loopback-only + log-everything security posture is clear in the README.** I felt comfortable exercising `fire` because I knew the blast radius was my local machine.
- **The rolling log gave me a complete trace** of the session in one file, including errors. Even with B6 caveat, the first thing I'd reach for to debug a weird session.

---

## Repro commands

For the maintainer who wants to verify any of the above — the session I ran, summarized:

```bash
# Build + launch
dotnet build samples/Reactor.TestApp
samples/Reactor.TestApp/bin/ARM64/Debug/net9.0-windows10.0.22621.0/Reactor.TestApp.exe \
  --devtools run --mcp-port 39417

# B4: shows AccentBadge in the window, not DemoApp
# B5: returns "Selector matched no elements"
curl -s http://127.0.0.1:39417/mcp -H 'Content-Type: application/json' -d \
  '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"click","arguments":{"selector":"[name=''+ 1'']"}}}'

# B2: InternalError wrapping
# (any failed selector, e.g. above)

# B1: FormDemo duplicate ids
curl ... switchComponent FormDemo
curl ... tree view=summary  # two nodes with id r:*/StackPanel~/TextBox[1]

# B3: crop visibly wrong
curl ... screenshot selector=r:preview-accentbadge/FlexPanel~/Button[2]
# decode .result.png; the crop shows "Cour" text, not the "+1" button

# B6: logged as 'ok'
curl ... waitFor predicate.textEquals=Neverland timeoutMs=500
tail %LOCALAPPDATA%/Reactor/devtools/<pid>.log  # "waitFor - 558ms ok 0"
```

Full session log is in `%LOCALAPPDATA%/Reactor/devtools/<pid>.log`.

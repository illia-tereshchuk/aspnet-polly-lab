const $ = (id) => document.getElementById(id);

let ok = 0, fail = 0, retries = 0;

const srConnection = new signalR.HubConnectionBuilder()
  .withUrl("/sgrHub")
  .withAutomaticReconnect()
  .build();

srConnection.on("fakeDep_Called", (e) => {
  if (e.ok) { ok++; $("okCount").textContent = ok; }
  else { fail++; $("failCount").textContent = fail; }
  $("lastLatency").textContent = e.latencyMs + " ms";
  addFeed(e);
});

srConnection.on("fakeDepCall_Retried", (e) => {
  retries++;
  $("retryCount").textContent = retries;
  const at = new Date(e.at).toLocaleTimeString("en-GB");
  const row = document.createElement("div");
  row.className = "text-warning";
  row.textContent = `${at}  ↻ RETRY #${e.attemptNumber}  ${e.reason}`;
  const feed = $("feed");
  feed.prepend(row);
  while (feed.childNodes.length > 100) feed.removeChild(feed.lastChild);
});

// Circuit state → indicator color/label + a timeline row.
const CIRCUIT_UI = {
  Closed:   { cls: "text-bg-success", label: "CLOSED",    hint: "normal — requests reach the dependency" },
  Open:     { cls: "text-bg-danger",  label: "OPEN",      hint: "fail fast — dependency untouched" },
  HalfOpen: { cls: "text-bg-warning", label: "HALF-OPEN", hint: "trial request — testing the waters" },
};

function applyCircuitState(state, detail) {
  const ui = CIRCUIT_UI[state] || CIRCUIT_UI.Closed;
  $("circuitBanner").className = "card mb-3 " + ui.cls;
  $("circuitState").textContent = ui.label;
  $("circuitDetail").textContent = detail || ui.hint;
}

srConnection.on("circuitState_Changed", (e) => {
  applyCircuitState(e.state, e.detail);
  const at = new Date(e.at).toLocaleTimeString("en-GB");
  const ui = CIRCUIT_UI[e.state] || CIRCUIT_UI.Closed;
  const row = document.createElement("div");
  row.className = "fw-bold";
  row.textContent = `${at}  ${ui.label}  — ${e.detail}`;
  const tl = $("timeline");
  tl.prepend(row);
  while (tl.childNodes.length > 50) tl.removeChild(tl.lastChild);
});

srConnection.onreconnecting(() => setConn("reconnecting…", "text-bg-warning"));
srConnection.onreconnected(() => setConn("live", "text-bg-success"));
srConnection.onclose(() => setConn("disconnected", "text-bg-danger"));

srConnection.start()
  .then(() => setConn("live", "text-bg-success"))
  .catch(() => setConn("connection error", "text-bg-danger"));

function setConn(text, cls) {
  const el = $("conn");
  el.textContent = text;
  el.className = "badge " + cls;
}

function addFeed(e) {
  const at = new Date(e.at).toLocaleTimeString("en-GB");
  const row = document.createElement("div");
  row.className = e.ok ? "text-success" : "text-danger";
  row.textContent = `${at}  ${e.ok ? "✔ OK  " : "✘ FAIL"}  ${e.latencyMs}ms  ${e.message}`;
  const feed = $("feed");
  feed.prepend(row);
  while (feed.childNodes.length > 100) feed.removeChild(feed.lastChild);
}

// --- Dependency levers: push to the backend immediately ---
async function patchDependency(patch) {
  await fetch("/api/setFakeDepState", {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(patch),
  });
}

$("up").addEventListener("change", (ev) => patchDependency({ isUp: ev.target.checked }));

$("failRate").addEventListener("input", (ev) => {
  const pct = +ev.target.value;
  $("failLabel").textContent = pct + "%";
  patchDependency({ failurePercentage: pct });
});

$("delay").addEventListener("input", (ev) => {
  const ms = +ev.target.value;
  $("delayLabel").textContent = ms;
  patchDependency({ delayMs: ms });
});

// --- Call button ---
const call = () => fetch("/api/callFakeDep").catch(() => {});
$("callOnce").addEventListener("click", () => call());

// --- Load generator ---
async function patchLoad(patch) {
  await fetch("/api/setLoadGenState", {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(patch),
  });
}

$("loadRunning").addEventListener("change", (ev) => patchLoad({ isRunning: ev.target.checked }));

$("rps").addEventListener("input", (ev) => {
  const rps = +ev.target.value;
  $("rpsLabel").textContent = rps;
  patchLoad({ requestsPerSecond: rps });
});

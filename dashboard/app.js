const clock = document.querySelector('#clock');
const connection = document.querySelector('#connection');
const processed = document.querySelector('#processed');
const safe = document.querySelector('#safe');
const hazard = document.querySelector('#hazard');
const throughput = document.querySelector('#throughput');
let running = true;
let total = 105;
let safeTotal = 51;
let hazardTotal = 43;

function updateClock() {
  clock.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function renderDecisions(decisions) {
  const feed = document.querySelector('#decisionFeed');
  if (!decisions.length) {
    feed.innerHTML = '<div class="feed-row">Waiting for the first processed item...</div>';
    return;
  }
  feed.innerHTML = decisions.map(decision => {
    const result = decision.result === 'HAZARD' ? 'hazard-row' : 'safe-row';
    const time = new Date(decision.timestamp).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit', second: '2-digit' });
    return `<div class="feed-row ${result}"><b>${decision.result}</b> ${decision.material} <time>${time}</time></div>`;
  }).join('');
}
updateClock();
setInterval(updateClock, 1000);

document.querySelector('#start')?.addEventListener('click', () => fetch('/api/start', { method: 'POST' }).then(refreshLiveState));
document.querySelector('#stop')?.addEventListener('click', () => fetch('/api/stop', { method: 'POST' }).then(refreshLiveState));
document.querySelector('#reset')?.addEventListener('click', () => fetch('/api/reset', { method: 'POST' }).then(refreshLiveState));

document.querySelectorAll('.waste-grid button').forEach(button => {
  button.addEventListener('click', () => {
    const text = button.textContent.replace('■ ', '');
    document.querySelector('#classification').textContent = `${text} queued for sensor classification.`;
    fetch('/api/inject', { method: 'POST' }).then(refreshLiveState);
  });
});
document.querySelectorAll('.nav').forEach(button => {
  button.addEventListener('click', () => {
    if (button.dataset.view === 'Knowledge') {
      window.location.href = 'knowledge.html';
      return;
    }
    if (button.dataset.view === 'Configuration') {
      window.location.href = 'configuration.html';
      return;
    }
    document.querySelectorAll('.nav').forEach(item => item.classList.remove('active'));
    button.classList.add('active');
  });
});

async function refreshLiveState() {
  try {
    const response = await fetch('/api/state');
    if (!response.ok) throw new Error('Dashboard API unavailable');
    const state = await response.json();
    document.querySelector('#processed').textContent = state.processed;
    document.querySelector('#safe').textContent = state.safe;
    document.querySelector('#hazard').textContent = state.hazard;
    document.querySelector('#throughput').textContent = state.running ? Math.round(120 * state.confidence / 100) : 0;
    document.querySelector('#beltSpeed').textContent = state.speed.toFixed(2);
    document.querySelector('#classification').textContent = `${state.active} item(s) currently tracked by the live twin.`;
    renderDecisions(state.decisions);
    const latest = state.decisions[0];
    if (latest) {
      const label = latest.result === 'HAZARD' ? '⚠ HAZARD — DIVERT TO REJECT' : '✓ SAFE — KEEP ON BELT';
      document.querySelector('.classification-body strong').textContent = label;
      document.querySelector('#classification').textContent = `${latest.material} classified by the live twin.`;
    }
    document.querySelector('.status-strip div:nth-child(1) strong').textContent = state.sensors.depth ? 'ONLINE' : 'OFFLINE';
    document.querySelector('.status-strip div:nth-child(2) strong').textContent = state.sensors.depth ? 'ONLINE' : 'OFFLINE';
    document.querySelector('.status-strip div:nth-child(3) strong').textContent = state.sensors.nir ? 'ONLINE' : 'OFFLINE';
    document.querySelector('#connection').textContent = state.running ? '● CONNECTED' : '● PAUSED';
    document.querySelector('#connection').className = state.running ? 'pill green' : 'pill';
    document.querySelector('.spec-values strong').textContent = `${state.length.toFixed(1)} m`;
    document.querySelectorAll('.spec-values strong')[1].textContent = `${state.width.toFixed(1)} m`;
    document.querySelectorAll('.spec-values strong')[2].textContent = `${state.speed.toFixed(1)} m/s`;
  } catch {
    document.querySelector('#connection').textContent = '● OFFLINE';
    document.querySelector('#connection').className = 'pill';
  }
}

refreshLiveState();
setInterval(refreshLiveState, 1000);

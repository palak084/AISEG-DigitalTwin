const clock = document.querySelector('#clock');
function updateClock() { clock.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }); }
updateClock();
setInterval(updateClock, 1000);
document.querySelectorAll('[data-link]').forEach(button => button.addEventListener('click', () => { window.location.href = button.dataset.link; }));

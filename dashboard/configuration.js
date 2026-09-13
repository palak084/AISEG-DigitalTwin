const clock = document.querySelector('#clock');
const updateClock = () => { clock.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }); };
updateClock();
setInterval(updateClock, 1000);
document.querySelectorAll('[data-link]').forEach(button => button.addEventListener('click', () => { window.location.href = button.dataset.link; }));
document.querySelector('#save').addEventListener('click', () => { document.querySelector('#saveState').textContent = 'Configuration saved'; });
document.querySelector('#defaults').addEventListener('click', () => { document.querySelectorAll('input[type="number"]').forEach(input => input.defaultValue && (input.value = input.defaultValue)); document.querySelector('#saveState').textContent = 'Defaults restored'; });

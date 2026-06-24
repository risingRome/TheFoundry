(() => {
    const root = document.documentElement;
    const toggle = document.getElementById('themeToggle');

    const applyTheme = theme => {
        root.setAttribute('data-bs-theme', theme);
        localStorage.setItem('foundry-theme', theme);
        window.dispatchEvent(new CustomEvent('foundry:themechange', { detail: { theme } }));
    };

    toggle?.addEventListener('click', () => {
        applyTheme(root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark');
    });

    if (window.lucide) {
        window.lucide.createIcons({ attrs: { 'stroke-width': 1.8 } });
    }
})();

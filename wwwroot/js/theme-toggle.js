// ============================================
// EduConnect Theme Toggle & Background Setup
// ============================================

(function() {
    const THEME_KEY = 'educonnect-theme';
    const LIGHT_THEME = 'light';
    const DARK_THEME = 'dark';

    // Initialize theme on page load
    function initializeTheme() {
        const savedTheme = localStorage.getItem(THEME_KEY);
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const isDarkMode = savedTheme ? savedTheme === DARK_THEME : prefersDark;

        setTheme(isDarkMode ? DARK_THEME : LIGHT_THEME);
    }

    // Set theme
    function setTheme(theme) {
        const html = document.documentElement;
        const isDark = theme === DARK_THEME;

        html.setAttribute('data-bs-theme', theme);
        localStorage.setItem(THEME_KEY, theme);

        // Update button icon
        updateThemeButton(isDark);
    }

    // Toggle theme
    window.toggleTheme = function() {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme') || LIGHT_THEME;
        const newTheme = currentTheme === LIGHT_THEME ? DARK_THEME : LIGHT_THEME;
        setTheme(newTheme);
    };

    // Update theme button
    function updateThemeButton(isDark) {
        const btn = document.getElementById('themeToggleBtn');
        if (btn) {
            btn.innerHTML = isDark ? '☀️' : '🌙';
            btn.setAttribute('title', isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode');
            btn.setAttribute('aria-label', isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode');
        }
    }

    // Setup animated bubble background
    function setupAnimatedBackground() {
        // Create background container if it doesn't exist
        let bgContainer = document.getElementById('animated-background');
        if (!bgContainer) {
            bgContainer = document.createElement('div');
            bgContainer.id = 'animated-background';
            bgContainer.className = 'bubble-background';

            // Create bubble container
            const bubbleContainer = document.createElement('div');
            bubbleContainer.className = 'bubble-container';

            // Create 15 bubbles
            for (let i = 0; i < 15; i++) {
                const bubble = document.createElement('div');
                bubble.className = 'bubble';
                bubbleContainer.appendChild(bubble);
            }

            // Create 3 floating particles
            for (let i = 0; i < 3; i++) {
                const particle = document.createElement('div');
                particle.className = 'particle';
                bubbleContainer.appendChild(particle);
            }

            // Create overlay
            const overlay = document.createElement('div');
            overlay.className = 'bubble-overlay';

            bgContainer.appendChild(bubbleContainer);
            bgContainer.appendChild(overlay);
            document.body.insertBefore(bgContainer, document.body.firstChild);
        }
    }

    // Listen for system theme changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        // Only auto-switch if user hasn't saved a preference
        if (!localStorage.getItem(THEME_KEY)) {
            setTheme(e.matches ? DARK_THEME : LIGHT_THEME);
        }
    });

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            initializeTheme();
            setupAnimatedBackground();
        });
    } else {
        initializeTheme();
        setupAnimatedBackground();
    }

    // Expose theme info for debugging
    window.themeInfo = function() {
        return {
            current: document.documentElement.getAttribute('data-bs-theme'),
            saved: localStorage.getItem(THEME_KEY),
            prefersDark: window.matchMedia('(prefers-color-scheme: dark)').matches
        };
    };
})();

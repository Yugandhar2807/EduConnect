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

    // Setup animated background
    function setupAnimatedBackground() {
        // Create background container if it doesn't exist
        let bgContainer = document.getElementById('animated-background');
        if (!bgContainer) {
            bgContainer = document.createElement('div');
            bgContainer.id = 'animated-background';
            bgContainer.className = 'animated-background';

            // Create video element
            const video = document.createElement('video');
            video.autoplay = true;
            video.muted = true;
            video.loop = true;
            video.className = 'background-video';
            video.innerHTML = `
                <source src="/videos/background.mp4" type="video/mp4">
                Your browser does not support the video tag.
            `;

            // Fallback: Create animated gradient
            video.addEventListener('error', function() {
                createGradientBackground(bgContainer);
            });

            // Create overlay
            const overlay = document.createElement('div');
            overlay.className = 'background-overlay';

            bgContainer.appendChild(video);
            bgContainer.appendChild(overlay);
            document.body.insertBefore(bgContainer, document.body.firstChild);
        }
    }

    // Create animated gradient fallback
    function createGradientBackground(container) {
        container.style.background = 'linear-gradient(-45deg, #ee7752, #e73c7e, #23a6d5, #23d5ab)';
        container.style.backgroundSize = '400% 400%';
        container.style.animation = 'gradient-animation 15s ease infinite';

        // Add animation styles
        const style = document.createElement('style');
        style.innerHTML = `
            @keyframes gradient-animation {
                0% {
                    background-position: 0% 50%;
                }
                50% {
                    background-position: 100% 50%;
                }
                100% {
                    background-position: 0% 50%;
                }
            }
        `;
        document.head.appendChild(style);
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

class JellyRequestConfig {
    constructor() {
        this.apiBase = '/JellyRequest';
        this.config = {};
        this.init();
    }

    async init() {
        await this.loadConfiguration();
        this.setupEventListeners();
        this.populateForm();
    }

    setupEventListeners() {
        // Form submission
        const form = document.getElementById('configForm');
        if (form) {
            form.addEventListener('submit', (e) => this.handleFormSubmit(e));
        }

        // Test connection buttons
        const testRadarrBtn = document.getElementById('testRadarrConnection');
        const testSonarrBtn = document.getElementById('testSonarrConnection');
        
        if (testRadarrBtn) {
            testRadarrBtn.addEventListener('click', () => this.testRadarrConnection());
        }

        if (testSonarrBtn) {
            testSonarrBtn.addEventListener('click', () => this.testSonarrConnection());
        }

        // Reset button
        const resetBtn = document.getElementById('resetConfig');
        if (resetBtn) {
            resetBtn.addEventListener('click', () => this.resetConfiguration());
        }

        // Auto-save on field change
        document.querySelectorAll('.form-control, input[type="checkbox"]').forEach(field => {
            field.addEventListener('change', () => this.autoSave());
        });
    }

    async loadConfiguration() {
        try {
            // This would typically call an API endpoint to get current config
            // For now, we'll use default values
            this.config = {
                tmdbApiKey: '',
                tmdbLanguage: 'en-US',
                includeAdultContent: false,
                radarrUrl: '',
                radarrApiKey: '',
                radarrQualityProfileId: 1,
                radarrRootFolderId: 1,
                sonarrUrl: '',
                sonarrApiKey: '',
                sonarrQualityProfileId: 1,
                sonarrRootFolderId: 1,
                allowRegularUserRequests: true,
                maxRequestsPerUser: 10,
                pollingIntervalMinutes: 5,
                enableNotifications: true
            };

            // Try to load saved config from localStorage for development
            const savedConfig = localStorage.getItem('jellyrequest-config');
            if (savedConfig) {
                this.config = { ...this.config, ...JSON.parse(savedConfig) };
            }
        } catch (error) {
            console.error('Error loading configuration:', error);
            this.showToast('Error loading configuration', 'error');
        }
    }

    populateForm() {
        // Text and number inputs
        Object.keys(this.config).forEach(key => {
            const field = document.getElementById(key);
            if (field) {
                if (field.type === 'checkbox') {
                    field.checked = this.config[key];
                } else {
                    field.value = this.config[key];
                }
            }
        });
    }

    async handleFormSubmit(e) {
        e.preventDefault();
        
        if (!this.validateForm()) {
            return;
        }

        this.showLoading();
        
        try {
            await this.saveConfiguration();
            this.showToast('Configuration saved successfully!', 'success');
        } catch (error) {
            console.error('Error saving configuration:', error);
            this.showToast('Error saving configuration', 'error');
        } finally {
            this.hideLoading();
        }
    }

    validateForm() {
        const tmdbApiKey = document.getElementById('tmdbApiKey').value.trim();
        const radarrUrl = document.getElementById('radarrUrl').value.trim();
        const radarrApiKey = document.getElementById('radarrApiKey').value.trim();
        const sonarrUrl = document.getElementById('sonarrUrl').value.trim();
        const sonarrApiKey = document.getElementById('sonarrApiKey').value.trim();

        if (!tmdbApiKey) {
            this.showToast('TMDB API Key is required', 'error');
            return false;
        }

        if (radarrUrl && !radarrApiKey) {
            this.showToast('Radarr API Key is required when Radarr URL is provided', 'error');
            return false;
        }

        if (sonarrUrl && !sonarrApiKey) {
            this.showToast('Sonarr API Key is required when Sonarr URL is provided', 'error');
            return false;
        }

        // Validate URLs
        if (radarrUrl && !this.isValidUrl(radarrUrl)) {
            this.showToast('Invalid Radarr URL', 'error');
            return false;
        }

        if (sonarrUrl && !this.isValidUrl(sonarrUrl)) {
            this.showToast('Invalid Sonarr URL', 'error');
            return false;
        }

        return true;
    }

    isValidUrl(string) {
        try {
            new URL(string);
            return true;
        } catch (_) {
            return false;
        }
    }

    async saveConfiguration() {
        // Collect form data
        const formData = new FormData(document.getElementById('configForm'));
        const newConfig = {};

        for (const [key, value] of formData.entries()) {
            if (value === 'on') {
                // Handle checkboxes
                const checkbox = document.getElementById(key);
                newConfig[key] = checkbox ? checkbox.checked : true;
            } else {
                // Handle text/number inputs
                const field = document.getElementById(key);
                if (field) {
                    if (field.type === 'number') {
                        newConfig[key] = parseInt(value) || 0;
                    } else {
                        newConfig[key] = value;
                    }
                }
            }
        }

        // Handle unchecked checkboxes
        document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
            if (!formData.has(checkbox.id)) {
                newConfig[checkbox.id] = false;
            }
        });

        this.config = { ...this.config, ...newConfig };

        // Save to localStorage for development
        localStorage.setItem('jellyrequest-config', JSON.stringify(this.config));

        // In a real implementation, this would call the backend API
        // await fetch(`${this.apiBase}/config`, {
        //     method: 'POST',
        //     headers: { 'Content-Type': 'application/json' },
        //     body: JSON.stringify(this.config)
        // });
    }

    async autoSave() {
        try {
            await this.saveConfiguration();
            // Show subtle indicator that config was saved
            this.showToast('Configuration auto-saved', 'info', 2000);
        } catch (error) {
            console.error('Auto-save failed:', error);
        }
    }

    async testRadarrConnection() {
        const url = document.getElementById('radarrUrl').value.trim();
        const apiKey = document.getElementById('radarrApiKey').value.trim();

        if (!url || !apiKey) {
            this.showToast('Please enter Radarr URL and API Key', 'error');
            return;
        }

        const statusElement = document.getElementById('radarrConnectionStatus');
        const button = document.getElementById('testRadarrConnection');
        
        this.setConnectionStatus(statusElement, button, 'testing');

        try {
            // In a real implementation, this would call the backend API
            // const response = await fetch(`${this.apiBase}/test/radarr`, {
            //     method: 'POST',
            //     headers: { 'Content-Type': 'application/json' },
            //     body: JSON.stringify({ url, apiKey })
            // });

            // Simulate connection test
            await this.simulateConnectionTest();
            
            this.setConnectionStatus(statusElement, button, 'success');
            this.showToast('Radarr connection successful!', 'success');
        } catch (error) {
            console.error('Radarr connection test failed:', error);
            this.setConnectionStatus(statusElement, button, 'error');
            this.showToast('Radarr connection failed', 'error');
        }
    }

    async testSonarrConnection() {
        const url = document.getElementById('sonarrUrl').value.trim();
        const apiKey = document.getElementById('sonarrApiKey').value.trim();

        if (!url || !apiKey) {
            this.showToast('Please enter Sonarr URL and API Key', 'error');
            return;
        }

        const statusElement = document.getElementById('sonarrConnectionStatus');
        const button = document.getElementById('testSonarrConnection');
        
        this.setConnectionStatus(statusElement, button, 'testing');

        try {
            // In a real implementation, this would call the backend API
            // const response = await fetch(`${this.apiBase}/test/sonarr`, {
            //     method: 'POST',
            //     headers: { 'Content-Type': 'application/json' },
            //     body: JSON.stringify({ url, apiKey })
            // });

            // Simulate connection test
            await this.simulateConnectionTest();
            
            this.setConnectionStatus(statusElement, button, 'success');
            this.showToast('Sonarr connection successful!', 'success');
        } catch (error) {
            console.error('Sonarr connection test failed:', error);
            this.setConnectionStatus(statusElement, button, 'error');
            this.showToast('Sonarr connection failed', 'error');
        }
    }

    async simulateConnectionTest() {
        // Simulate network delay
        return new Promise(resolve => setTimeout(resolve, 1500));
    }

    setConnectionStatus(statusElement, button, status) {
        if (!statusElement || !button) return;

        // Reset classes
        statusElement.className = 'connection-status';
        button.disabled = false;

        switch (status) {
            case 'testing':
                statusElement.className = 'connection-status testing';
                statusElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Testing...';
                button.disabled = true;
                break;
            case 'success':
                statusElement.className = 'connection-status success';
                statusElement.innerHTML = '<i class="fas fa-check-circle"></i> Connected';
                break;
            case 'error':
                statusElement.className = 'connection-status error';
                statusElement.innerHTML = '<i class="fas fa-times-circle"></i> Connection Failed';
                break;
        }
    }

    async resetConfiguration() {
        if (!confirm('Are you sure you want to reset all configuration to default values?')) {
            return;
        }

        this.showLoading();

        try {
            await this.loadConfiguration();
            this.populateForm();
            localStorage.removeItem('jellyrequest-config');
            this.showToast('Configuration reset to defaults', 'success');
        } catch (error) {
            console.error('Error resetting configuration:', error);
            this.showToast('Error resetting configuration', 'error');
        } finally {
            this.hideLoading();
        }
    }

    showLoading() {
        // Could add a loading overlay here
        document.body.style.cursor = 'wait';
    }

    hideLoading() {
        document.body.style.cursor = 'default';
    }

    showToast(message, type = 'info', duration = 5000) {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <i class="fas ${this.getToastIcon(type)}"></i>
                <span>${message}</span>
            </div>
            <button class="toast-close" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        `;

        container.appendChild(toast);

        // Auto remove after specified duration
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, duration);
    }

    getToastIcon(type) {
        switch (type) {
            case 'success': return 'fa-check-circle';
            case 'error': return 'fa-exclamation-circle';
            case 'warning': return 'fa-exclamation-triangle';
            default: return 'fa-info-circle';
        }
    }
}

// Initialize the configuration page
const jellyRequestConfig = new JellyRequestConfig();

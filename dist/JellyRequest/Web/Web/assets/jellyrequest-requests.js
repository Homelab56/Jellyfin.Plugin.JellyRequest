class JellyRequestRequests {
    constructor() {
        this.apiBase = '/JellyRequest';
        this.currentUserId = this.getCurrentUserId();
        this.requests = [];
        this.filteredRequests = [];
        this.init();
    }

    async init() {
        this.setupEventListeners();
        await this.loadRequests();
        this.hideLoading();
    }

    getCurrentUserId() {
        // Get current user ID from Jellyfin's context
        if (window.ApiClient && window.ApiClient.getCurrentUserId) {
            return window.ApiClient.getCurrentUserId();
        }
        
        // Fallback for development/testing
        return localStorage.getItem('jellyfinUserId') || 'test-user';
    }

    setupEventListeners() {
        // Navigation
        const backToDiscover = document.getElementById('backToDiscover');
        if (backToDiscover) {
            backToDiscover.addEventListener('click', () => {
                window.location.href = 'discover.html';
            });
        }

        const startExploring = document.getElementById('startExploring');
        if (startExploring) {
            startExploring.addEventListener('click', () => {
                window.location.href = 'discover.html';
            });
        }

        // Refresh
        const refreshBtn = document.getElementById('refreshBtn');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.refreshRequests());
        }

        // Filters
        const statusFilter = document.getElementById('statusFilter');
        const typeFilter = document.getElementById('typeFilter');
        
        if (statusFilter) {
            statusFilter.addEventListener('change', () => this.applyFilters());
        }

        if (typeFilter) {
            typeFilter.addEventListener('change', () => this.applyFilters());
        }

        // Modal controls
        const closeRequestModal = document.getElementById('closeRequestModal');
        const requestDetailModal = document.getElementById('requestDetailModal');
        
        if (closeRequestModal && requestDetailModal) {
            closeRequestModal.addEventListener('click', () => this.hideRequestDetailModal());
            requestDetailModal.addEventListener('click', (e) => {
                if (e.target === requestDetailModal) this.hideRequestDetailModal();
            });
        }

        // Request detail buttons
        const playRequestBtn = document.getElementById('playRequestBtn');
        const cancelRequestBtn = document.getElementById('cancelRequestBtn');

        if (playRequestBtn) {
            playRequestBtn.addEventListener('click', () => this.playCurrentRequest());
        }

        if (cancelRequestBtn) {
            cancelRequestBtn.addEventListener('click', () => this.cancelCurrentRequest());
        }
    }

    async loadRequests() {
        this.showLoading();
        
        try {
            const response = await fetch(`${this.apiBase}/requests?userId=${this.currentUserId}`);
            if (!response.ok) throw new Error('Failed to load requests');
            
            this.requests = await response.json();
            this.filteredRequests = [...this.requests];
            this.renderRequests();
        } catch (error) {
            console.error('Error loading requests:', error);
            this.showToast('Error loading requests', 'error');
        } finally {
            this.hideLoading();
        }
    }

    async refreshRequests() {
        const refreshBtn = document.getElementById('refreshBtn');
        if (refreshBtn) {
            refreshBtn.disabled = true;
            refreshBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Refreshing...';
        }

        try {
            await this.loadRequests();
            this.showToast('Requests refreshed', 'success');
        } catch (error) {
            this.showToast('Error refreshing requests', 'error');
        } finally {
            if (refreshBtn) {
                refreshBtn.disabled = false;
                refreshBtn.innerHTML = '<i class="fas fa-sync-alt"></i> Refresh';
            }
        }
    }

    applyFilters() {
        const statusFilter = document.getElementById('statusFilter')?.value || '';
        const typeFilter = document.getElementById('typeFilter')?.value || '';

        this.filteredRequests = this.requests.filter(request => {
            const statusMatch = !statusFilter || request.status === statusFilter;
            const typeMatch = !typeFilter || request.mediaType === typeFilter;
            return statusMatch && typeMatch;
        });

        this.renderRequests();
    }

    renderRequests() {
        const emptyState = document.getElementById('emptyState');
        const requestsSection = document.getElementById('requestsSection');
        const requestsList = document.getElementById('requestsList');

        if (!requestsList) return;

        if (this.filteredRequests.length === 0) {
            if (emptyState) emptyState.classList.remove('hidden');
            if (requestsSection) requestsSection.classList.add('hidden');
            return;
        }

        if (emptyState) emptyState.classList.add('hidden');
        if (requestsSection) requestsSection.classList.remove('hidden');

        requestsList.innerHTML = '';
        
        this.filteredRequests.forEach(request => {
            const requestItem = this.createRequestItem(request);
            requestsList.appendChild(requestItem);
        });
    }

    createRequestItem(request) {
        const item = document.createElement('div');
        item.className = 'request-item';
        item.dataset.requestId = request.id;

        const posterUrl = request.posterPath ? 
            `https://image.tmdb.org/t/p/w300${request.posterPath}` : 
            'https://via.placeholder.com/150x225?text=No+Image';

        const statusClass = this.getStatusClass(request.status);
        const statusText = this.getStatusText(request.status);
        const progress = this.calculateProgress(request);

        item.innerHTML = `
            <div class="request-poster">
                <img src="${posterUrl}" alt="${request.title}" loading="lazy">
            </div>
            <div class="request-info">
                <div class="request-header">
                    <h3 class="request-title">${request.title}</h3>
                    <span class="request-year">${request.year}</span>
                </div>
                <div class="request-meta">
                    <span class="request-type">${request.mediaType === 'movie' ? 'Movie' : 'TV Show'}</span>
                    <span class="request-status ${statusClass}">${statusText}</span>
                </div>
                <div class="request-date">
                    <small>Requested: ${this.formatDate(request.requestDate)}</small>
                </div>
                ${progress.show ? `
                    <div class="request-progress">
                        <div class="progress-bar">
                            <div class="progress-fill" style="width: ${progress.percentage}%"></div>
                        </div>
                        <span class="progress-text">${progress.text}</span>
                    </div>
                ` : ''}
                <div class="request-actions">
                    ${request.status === 'Available' ? 
                        `<button class="btn btn-primary btn-sm" onclick="jellyRequestRequests.playRequest(${request.id})">
                            <i class="fas fa-play"></i> Play
                        </button>` : ''
                    }
                    ${['Requested', 'Downloading'].includes(request.status) ? 
                        `<button class="btn btn-secondary btn-sm" onclick="jellyRequestRequests.showRequestDetail(${request.id})">
                            <i class="fas fa-info-circle"></i> Details
                        </button>` : ''
                    }
                    ${['Requested', 'Downloading'].includes(request.status) ? 
                        `<button class="btn btn-danger btn-sm" onclick="jellyRequestRequests.cancelRequest(${request.id})">
                            <i class="fas fa-times"></i> Cancel
                        </button>` : ''
                    }
                </div>
            </div>
        `;

        return item;
    }

    getStatusClass(status) {
        switch (status) {
            case 'Requested': return 'status-requested';
            case 'Downloading': return 'status-downloading';
            case 'Completed': return 'status-completed';
            case 'Available': return 'status-available';
            case 'Failed': return 'status-failed';
            default: return 'status-unknown';
        }
    }

    getStatusText(status) {
        switch (status) {
            case 'Requested': return 'Requested';
            case 'Downloading': return 'Downloading';
            case 'Completed': return 'Completed';
            case 'Available': return 'Available';
            case 'Failed': return 'Failed';
            default: return 'Unknown';
        }
    }

    calculateProgress(request) {
        if (request.status === 'Downloading') {
            // In a real implementation, this would come from the API
            return {
                show: true,
                percentage: Math.floor(Math.random() * 80) + 10, // Simulated progress
                text: 'Downloading...'
            };
        }
        
        return { show: false };
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    async showRequestDetail(requestId) {
        const request = this.requests.find(r => r.id === requestId);
        if (!request) return;

        this.currentRequest = request;

        const modal = document.getElementById('requestDetailModal');
        if (!modal) return;

        // Set poster
        const poster = document.getElementById('requestDetailPoster');
        if (poster) {
            poster.src = request.posterPath ? 
                `https://image.tmdb.org/t/p/w300${request.posterPath}` : 
                'https://via.placeholder.com/300x450?text=No+Image';
            poster.alt = request.title;
        }

        // Set info
        document.getElementById('requestDetailTitle').textContent = request.title;
        document.getElementById('requestDetailYear').textContent = request.year;
        document.getElementById('requestDetailType').textContent = request.mediaType === 'movie' ? 'Movie' : 'TV Show';
        document.getElementById('requestDetailDate').textContent = this.formatDate(request.requestDate);

        // Set status
        const statusElement = document.getElementById('requestDetailStatus');
        if (statusElement) {
            statusElement.textContent = this.getStatusText(request.status);
            statusElement.className = `status-badge ${this.getStatusClass(request.status)}`;
        }

        // Set progress
        const progressContainer = document.getElementById('requestProgressContainer');
        const progressBar = document.getElementById('requestProgressBar');
        const progressText = document.getElementById('requestProgressText');

        if (request.status === 'Downloading') {
            if (progressContainer) progressContainer.classList.remove('hidden');
            const progress = this.calculateProgress(request);
            if (progressBar) progressBar.style.width = `${progress.percentage}%`;
            if (progressText) progressText.textContent = `${progress.percentage}%`;
        } else {
            if (progressContainer) progressContainer.classList.add('hidden');
        }

        // Update action buttons
        const playBtn = document.getElementById('playRequestBtn');
        const cancelBtn = document.getElementById('cancelRequestBtn');

        if (playBtn) {
            playBtn.classList.toggle('hidden', request.status !== 'Available');
        }

        if (cancelBtn) {
            cancelBtn.classList.toggle('hidden', !['Requested', 'Downloading'].includes(request.status));
        }

        // Show modal
        modal.classList.remove('hidden');
    }

    hideRequestDetailModal() {
        const modal = document.getElementById('requestDetailModal');
        if (modal) {
            modal.classList.add('hidden');
        }
        this.currentRequest = null;
    }

    async playRequest(requestId) {
        const request = this.requests.find(r => r.id === requestId);
        if (!request) return;

        // In a real implementation, this would navigate to the Jellyfin player
        this.showToast(`Opening "${request.title}"...`, 'info');
    }

    async playCurrentRequest() {
        if (!this.currentRequest) return;
        await this.playRequest(this.currentRequest.id);
    }

    async cancelRequest(requestId) {
        const request = this.requests.find(r => r.id === requestId);
        if (!request) return;

        if (!confirm(`Are you sure you want to cancel the request for "${request.title}"?`)) {
            return;
        }

        try {
            const response = await fetch(`${this.apiBase}/request/${requestId}?userId=${this.currentUserId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                throw new Error('Failed to cancel request');
            }

            // Remove from local array
            this.requests = this.requests.filter(r => r.id !== requestId);
            this.filteredRequests = this.filteredRequests.filter(r => r.id !== requestId);
            
            this.renderRequests();
            this.hideRequestDetailModal();
            this.showToast(`Request cancelled for "${request.title}"`, 'success');
        } catch (error) {
            console.error('Error cancelling request:', error);
            this.showToast('Error cancelling request', 'error');
        }
    }

    async cancelCurrentRequest() {
        if (!this.currentRequest) return;
        await this.cancelRequest(this.currentRequest.id);
    }

    showLoading() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.classList.remove('hidden');
        }
    }

    hideLoading() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.classList.add('hidden');
        }
    }

    showToast(message, type = 'info') {
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

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, 5000);
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

// Initialize the requests page
const jellyRequestRequests = new JellyRequestRequests();

// Make it globally available for inline event handlers
window.jellyRequestRequests = jellyRequestRequests;

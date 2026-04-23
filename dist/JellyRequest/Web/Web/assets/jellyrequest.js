class JellyRequest {
    constructor() {
        this.apiBase = '/JellyRequest';
        this.currentUserId = this.getCurrentUserId();
        this.currentItem = null;
        this.init();
    }

    async init() {
        this.setupEventListeners();
        await this.loadTrendingContent();
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
        // Search functionality with debouncing
        const searchInput = document.getElementById('searchInput');
        const searchBtn = document.getElementById('searchBtn');
        let searchTimeout;
        
        if (searchInput && searchBtn) {
            searchBtn.addEventListener('click', () => this.performSearch());
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                const query = e.target.value.trim();
                if (query.length >= 2) {
                    searchTimeout = setTimeout(() => this.performSearch(), 400);
                } else if (query.length === 0) {
                    this.hideSearchResults();
                    this.loadTrendingContent();
                }
            });
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    clearTimeout(searchTimeout);
                    this.performSearch();
                }
            });
        }

        // Navigation
        const myRequestsBtn = document.getElementById('myRequestsBtn');
        if (myRequestsBtn) {
            myRequestsBtn.addEventListener('click', () => {
                window.location.href = 'myrequests.html';
            });
        }

        // Modal controls
        const closeModal = document.getElementById('closeModal');
        const detailModal = document.getElementById('detailModal');
        
        if (closeModal && detailModal) {
            closeModal.addEventListener('click', () => this.hideDetailModal());
            detailModal.addEventListener('click', (e) => {
                if (e.target === detailModal) this.hideDetailModal();
            });
        }

        // Carousel scroll buttons
        document.querySelectorAll('.scroll-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const target = e.currentTarget.dataset.target;
                const direction = e.currentTarget.classList.contains('scroll-left') ? 'left' : 'right';
                this.scrollCarousel(target, direction);
            });
        });

        // Detail modal buttons
        const requestBtn = document.getElementById('requestBtn');
        const playBtn = document.getElementById('playBtn');
        const trailerBtn = document.getElementById('trailerBtn');

        if (requestBtn) {
            requestBtn.addEventListener('click', () => this.makeRequest());
        }

        if (playBtn) {
            playBtn.addEventListener('click', () => this.playItem());
        }

        if (trailerBtn) {
            trailerBtn.addEventListener('click', () => this.watchTrailer());
        }
    }

    async loadTrendingContent() {
        try {
            const [trendingMovies, trendingTv, popularMovies, popularTv, topRatedMovies, topRatedTv, nowPlaying] = await Promise.all([
                this.fetchTrending('movie'),
                this.fetchTrending('tv'),
                this.fetchPopular('movie'),
                this.fetchPopular('tv'),
                this.fetchTopRated('movie'),
                this.fetchTopRated('tv'),
                this.fetchNowPlaying()
            ]);

            this.renderCarousel('trendingMovies', trendingMovies);
            this.renderCarousel('trendingTv', trendingTv);
            this.renderCarousel('popularMovies', popularMovies);
            this.renderCarousel('popularTv', popularTv);
            this.renderCarousel('topRatedMovies', topRatedMovies);
            this.renderCarousel('topRatedTv', topRatedTv);
            this.renderCarousel('nowPlaying', nowPlaying);
        } catch (error) {
            console.error('Error loading trending content:', error);
            this.showToast('Error loading content', 'error');
        }
    }

    async fetchTrending(type) {
        const response = await fetch(`${this.apiBase}/trending?type=${type}`);
        if (!response.ok) throw new Error(`Failed to fetch trending ${type}`);
        return await response.json();
    }

    async fetchPopular(type) {
        const response = await fetch(`${this.apiBase}/popular?type=${type}`);
        if (!response.ok) throw new Error(`Failed to fetch popular ${type}`);
        return await response.json();
    }

    async fetchTopRated(type) {
        const response = await fetch(`${this.apiBase}/toprated?type=${type}`);
        if (!response.ok) throw new Error(`Failed to fetch top rated ${type}`);
        return await response.json();
    }

    async fetchNowPlaying() {
        const response = await fetch(`${this.apiBase}/nowplaying`);
        if (!response.ok) throw new Error('Failed to fetch now playing movies');
        return await response.json();
    }

    async performSearch() {
        const query = document.getElementById('searchInput')?.value.trim();
        if (!query) return;

        this.showLoading();
        
        try {
            const response = await fetch(`${this.apiBase}/search?query=${encodeURIComponent(query)}`);
            if (!response.ok) throw new Error('Search failed');
            
            const results = await response.json();
            this.displaySearchResults(results);
        } catch (error) {
            console.error('Search error:', error);
            this.showToast('Search failed', 'error');
        } finally {
            this.hideLoading();
        }
    }

    hideSearchResults() {
        const searchSection = document.getElementById('searchResults');
        if (searchSection) {
            searchSection.classList.add('hidden');
        }
    }

    displaySearchResults(results) {
        const searchSection = document.getElementById('searchResults');
        const searchGrid = document.getElementById('searchResultsGrid');
        
        if (!searchSection || !searchGrid) return;

        searchGrid.innerHTML = '';
        
        // Combine movies and TV shows
        const allItems = [
            ...results.movies.map(item => ({ ...item, mediaType: 'movie' })),
            ...results.tvShows.map(item => ({ ...item, mediaType: 'tv' }))
        ];

        allItems.forEach(item => {
            const card = this.createContentCard(item);
            searchGrid.appendChild(card);
        });

        searchSection.classList.remove('hidden');
    }

    renderCarousel(containerId, items) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const track = container.querySelector('.carousel-track');
        if (!track) return;

        track.innerHTML = '';
        
        items.forEach(item => {
            const card = this.createContentCard(item);
            track.appendChild(card);
        });
    }

    createContentCard(item) {
        const card = document.createElement('div');
        card.className = 'content-card';
        card.dataset.tmdbId = item.id;
        card.dataset.mediaType = item.mediaType;

        const posterUrl = item.posterUrl || 'https://via.placeholder.com/300x450?text=No+Image';
        const year = item.year || '';
        const rating = item.voteAverage > 0 ? item.voteAverage.toFixed(1) : 'N/A';

        card.innerHTML = `
            <div class="card-poster">
                <img src="${posterUrl}" alt="${item.title}" loading="lazy">
                <div class="card-overlay">
                    <div class="card-actions">
                        ${item.isInLibrary ? 
                            `<button class="btn btn-play" onclick="jellyRequest.playItem(${item.id}, '${item.mediaType}')">
                                <i class="fas fa-play"></i>
                            </button>` :
                            `<button class="btn btn-request" onclick="jellyRequest.requestItem(${item.id}, '${item.mediaType}')">
                                <i class="fas fa-plus"></i>
                            </button>`
                        }
                        <button class="btn btn-info" onclick="jellyRequest.showDetail(${item.id}, '${item.mediaType}')">
                            <i class="fas fa-info-circle"></i>
                        </button>
                    </div>
                </div>
            </div>
            <div class="card-info">
                <h3 class="card-title">${item.title}</h3>
                <div class="card-meta">
                    <span class="card-year">${year}</span>
                    <span class="card-rating">
                        <i class="fas fa-star"></i> ${rating}
                    </span>
                </div>
                ${item.isInLibrary ? '<span class="in-library-badge">In Library</span>' : ''}
            </div>
        `;

        // Add hover effect
        card.addEventListener('mouseenter', () => this.loadItemPreview(item));
        
        return card;
    }

    async loadItemPreview(item) {
        // Could implement lazy loading of additional preview data here
    }

    async showDetail(tmdbId, mediaType) {
        this.showLoading();
        
        try {
            const response = await fetch(`${this.apiBase}/detail?tmdbId=${tmdbId}&type=${mediaType}`);
            if (!response.ok) throw new Error('Failed to load details');
            
            const detail = await response.json();
            this.currentItem = detail;
            this.renderDetailModal(detail);
        } catch (error) {
            console.error('Error loading details:', error);
            this.showToast('Failed to load details', 'error');
        } finally {
            this.hideLoading();
        }
    }

    renderDetailModal(detail) {
        const modal = document.getElementById('detailModal');
        if (!modal) return;

        // Set backdrop
        const backdrop = modal.querySelector('.detail-backdrop');
        if (backdrop) {
            backdrop.style.backgroundImage = `url(${detail.backdropUrl || 'https://via.placeholder.com/1920x1080?text=No+Backdrop'})`;
        }

        // Set poster
        const poster = document.getElementById('detailPoster');
        if (poster) {
            poster.src = detail.posterUrl || 'https://via.placeholder.com/300x450?text=No+Image';
            poster.alt = detail.title;
        }

        // Set basic info
        document.getElementById('detailTitle').textContent = detail.title;
        document.getElementById('detailYear').textContent = detail.year;
        document.getElementById('detailRating').textContent = detail.formattedRating;
        document.getElementById('detailRuntime').textContent = detail.formattedRuntime || '';
        document.getElementById('detailOverview').textContent = detail.overview;

        // Set genres
        const genresContainer = document.getElementById('detailGenres');
        if (genresContainer) {
            genresContainer.innerHTML = detail.genres.map(genre => 
                `<span class="genre-tag">${genre}</span>`
            ).join('');
        }

        // Set cast
        const castElement = document.getElementById('detailCast');
        if (castElement) {
            castElement.textContent = detail.cast.slice(0, 5).join(', ');
        }

        // Set director
        const directorElement = document.getElementById('detailDirector');
        if (directorElement) {
            directorElement.textContent = detail.director || 'N/A';
        }

        // Update action buttons
        const playBtn = document.getElementById('playBtn');
        const requestBtn = document.getElementById('requestBtn');
        const trailerBtn = document.getElementById('trailerBtn');

        if (playBtn) {
            playBtn.classList.toggle('hidden', !detail.isInLibrary);
        }

        if (requestBtn) {
            requestBtn.classList.toggle('hidden', detail.isInLibrary);
        }

        if (trailerBtn) {
            trailerBtn.classList.toggle('hidden', !detail.videos || detail.videos.length === 0);
        }

        // Show modal
        modal.classList.remove('hidden');
    }

    hideDetailModal() {
        const modal = document.getElementById('detailModal');
        if (modal) {
            modal.classList.add('hidden');
        }
        this.currentItem = null;
    }

    async makeRequest() {
        if (!this.currentItem) return;

        try {
            const response = await fetch(`${this.apiBase}/request`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    tmdbId: this.currentItem.id,
                    mediaType: this.currentItem.mediaType,
                    userId: this.currentUserId
                })
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || 'Request failed');
            }

            const request = await response.json();
            this.showToast(`Request sent for "${this.currentItem.title}"!`, 'success');
            this.hideDetailModal();
            
            // Update UI to reflect the request
            this.updateItemStatus(this.currentItem.id, this.currentItem.mediaType, 'Requested');
        } catch (error) {
            console.error('Request error:', error);
            this.showToast(error.message || 'Failed to send request', 'error');
        }
    }

    async requestItem(tmdbId, mediaType) {
        this.showLoading();
        
        try {
            const response = await fetch(`${this.apiBase}/request`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    tmdbId: tmdbId,
                    mediaType: mediaType,
                    userId: this.currentUserId
                })
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || 'Request failed');
            }

            const request = await response.json();
            this.showToast('Request sent successfully!', 'success');
            
            // Update UI
            this.updateItemStatus(tmdbId, mediaType, 'Requested');
        } catch (error) {
            console.error('Request error:', error);
            this.showToast(error.message || 'Failed to send request', 'error');
        } finally {
            this.hideLoading();
        }
    }

    playItem(tmdbId, mediaType) {
        // Navigate to Jellyfin player
        if (tmdbId && mediaType) {
            // This would need to be implemented based on Jellyfin's navigation
            this.showToast('Opening player...', 'info');
        }
    }

    watchTrailer() {
        if (!this.currentItem || !this.currentItem.videos || this.currentItem.videos.length === 0) return;
        
        const trailer = this.currentItem.videos[0];
        window.open(trailer.youTubeUrl, '_blank');
    }

    updateItemStatus(tmdbId, mediaType, status) {
        // Update cards to reflect new status
        const cards = document.querySelectorAll(`[data-tmdb-id="${tmdbId}"][data-media-type="${mediaType}"]`);
        cards.forEach(card => {
            const actions = card.querySelector('.card-actions');
            if (actions && status === 'Requested') {
                actions.innerHTML = `
                    <button class="btn btn-requested" disabled>
                        <i class="fas fa-clock"></i> Requested
                    </button>
                    <button class="btn btn-info" onclick="jellyRequest.showDetail(${tmdbId}, '${mediaType}')">
                        <i class="fas fa-info-circle"></i>
                    </button>
                `;
            }
        });
    }

    scrollCarousel(containerId, direction) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const scrollAmount = 300;
        if (direction === 'left') {
            container.scrollLeft -= scrollAmount;
        } else {
            container.scrollLeft += scrollAmount;
        }
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

// Initialize the application
const jellyRequest = new JellyRequest();

// Make it globally available for inline event handlers
window.jellyRequest = jellyRequest;

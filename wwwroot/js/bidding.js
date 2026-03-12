/**
 * Bidding Page - Frontend JavaScript
 * Handles auction display, real-time bidding via SignalR, and bid placement.
 */

document.addEventListener('DOMContentLoaded', function () {
    // Get auction ID from URL
    const pathParts = window.location.pathname.split('/');
    const auctionId = pathParts[pathParts.length - 1];

    if (!auctionId || isNaN(parseInt(auctionId))) {
        showError();
        return;
    }

    // Initialize the bidding page
    const biddingPage = new BiddingPage(parseInt(auctionId));
    biddingPage.init();
});

class BiddingPage {
    constructor(auctionId) {
        this.auctionId = auctionId;
        this.auction = null;
        this.bids = [];
        this.currentPhotoIndex = 0;
        this.photos = [];
        this.connection = null;
        this.countdownInterval = null;
        this.isAuthenticated = false;
        this.minimumBidIncrement = 100; // $100 minimum increment

        // DOM Elements
        this.elements = {
            loadingState: document.getElementById('loadingState'),
            errorState: document.getElementById('errorState'),
            auctionContent: document.getElementById('auctionContent'),
            mainPhoto: document.getElementById('mainPhoto'),
            thumbnailStrip: document.getElementById('thumbnailStrip'),
            photoCounter: document.getElementById('photoCounter'),
            prevPhotoBtn: document.getElementById('prevPhotoBtn'),
            nextPhotoBtn: document.getElementById('nextPhotoBtn'),
            vehicleTitle: document.getElementById('vehicleTitle'),
            listingTitle: document.getElementById('listingTitle'),
            vehicleYear: document.getElementById('vehicleYear'),
            vehicleMake: document.getElementById('vehicleMake'),
            vehicleModel: document.getElementById('vehicleModel'),
            vehicleMileage: document.getElementById('vehicleMileage'),
            vehicleDescription: document.getElementById('vehicleDescription'),
            bidHistoryContent: document.getElementById('bidHistoryContent'),
            auctionStatusBadge: document.getElementById('auctionStatusBadge'),
            countdownDays: document.getElementById('countdownDays'),
            countdownHours: document.getElementById('countdownHours'),
            countdownMinutes: document.getElementById('countdownMinutes'),
            countdownSeconds: document.getElementById('countdownSeconds'),
            currentBidAmount: document.getElementById('currentBidAmount'),
            startingPrice: document.getElementById('startingPrice'),
            reserveStatus: document.getElementById('reserveStatus'),
            reservePriceRow: document.getElementById('reservePriceRow'),
            totalBidsCount: document.getElementById('totalBidsCount'),
            minimumBid: document.getElementById('minimumBid'),
            bidAmountInput: document.getElementById('bidAmountInput'),
            placeBidBtn: document.getElementById('placeBidBtn'),
            bidFormSection: document.getElementById('bidFormSection'),
            loginPrompt: document.getElementById('loginPrompt'),
            auctionEndedSection: document.getElementById('auctionEndedSection'),
            winnerInfo: document.getElementById('winnerInfo'),
            auctionId: document.getElementById('auctionId'),
            auctionStartTime: document.getElementById('auctionStartTime'),
            auctionEndTime: document.getElementById('auctionEndTime'),
            bidConfirmModal: document.getElementById('bidConfirmModal'),
            confirmBidAmount: document.getElementById('confirmBidAmount'),
            cancelBidBtn: document.getElementById('cancelBidBtn'),
            confirmBidBtn: document.getElementById('confirmBidBtn'),
            bidToast: document.getElementById('bidToast'),
            toastMessage: document.getElementById('toastMessage')
        };
    }

    async init() {
        try {
            // Check authentication status
            await this.checkAuthStatus();

            // Load auction data
            await this.loadAuction();

            // Load bid history
            await this.loadBidHistory();

            // Setup SignalR connection
            await this.setupSignalR();

            // Setup event listeners
            this.setupEventListeners();

            // Start countdown timer
            this.startCountdown();

            // Show content
            this.elements.loadingState.classList.add('hidden');
            this.elements.auctionContent.classList.remove('hidden');
        } catch (error) {
            console.error('Error initializing bidding page:', error);
            showError();
        }
    }

    async checkAuthStatus() {
        try {
            const response = await fetch('/api/auth/status');
            if (response.ok) {
                const data = await response.json();
                this.isAuthenticated = data.isAuthenticated;
            }
        } catch (error) {
            console.log('Auth check failed, assuming not authenticated');
            this.isAuthenticated = false;
        }
    }

    async loadAuction() {
        const response = await fetch(`/api/auctions/${this.auctionId}`);
        if (!response.ok) {
            throw new Error('Failed to load auction');
        }

        this.auction = await response.json();
        this.photos = this.auction.photoUrls || [];

        this.renderAuction();
    }

    async loadBidHistory() {
        try {
            const response = await fetch(`/api/auctions/${this.auctionId}/bids`);
            if (response.ok) {
                this.bids = await response.json();
                this.renderBidHistory();
            }
        } catch (error) {
            console.log('Failed to load bid history');
        }
    }

    renderAuction() {
        const a = this.auction;

        // Vehicle info
        this.elements.vehicleTitle.textContent = `${a.year} ${a.makeName} ${a.modelName}`;
        this.elements.listingTitle.textContent = a.title || '';
        this.elements.vehicleYear.textContent = a.year;
        this.elements.vehicleMake.textContent = a.makeName;
        this.elements.vehicleModel.textContent = a.modelName;
        this.elements.vehicleMileage.textContent = a.mileage ? `${a.mileage.toLocaleString()} km` : 'N/A';
        this.elements.vehicleDescription.textContent = a.description || '';

        // Photos
        this.renderPhotos();

        // Auction details
        this.elements.auctionId.textContent = `#${a.auctionId}`;
        this.elements.auctionStartTime.textContent = this.formatDateTime(a.startTime);
        this.elements.auctionEndTime.textContent = this.formatDateTime(a.endTime);

        // Prices
        this.elements.startingPrice.textContent = this.formatCurrency(a.startPrice);
        
        if (a.reservePrice) {
            this.elements.reservePriceRow.style.display = 'flex';
            const currentBid = a.currentBid || 0;
            const reserveMet = currentBid >= a.reservePrice;
            this.elements.reserveStatus.textContent = reserveMet ? 'Met ✓' : 'Not Met';
            this.elements.reserveStatus.className = `reserve-status ${reserveMet ? 'met' : 'not-met'}`;
        } else {
            this.elements.reservePriceRow.style.display = 'none';
        }

        // Current bid
        this.updateCurrentBid(a.currentBid);

        // Auction status
        this.updateAuctionStatus();
    }

    renderPhotos() {
        if (this.photos.length === 0) {
            this.elements.mainPhoto.src = '/images/no-photo.png';
            this.elements.prevPhotoBtn.style.display = 'none';
            this.elements.nextPhotoBtn.style.display = 'none';
            this.elements.photoCounter.style.display = 'none';
            return;
        }

        this.elements.mainPhoto.src = this.photos[this.currentPhotoIndex];
        this.elements.photoCounter.textContent = `${this.currentPhotoIndex + 1} / ${this.photos.length}`;

        // Thumbnails
        this.elements.thumbnailStrip.innerHTML = this.photos.map((url, index) => `
            <img 
                src="${url}" 
                class="thumbnail ${index === this.currentPhotoIndex ? 'active' : ''}" 
                data-index="${index}"
                alt="Photo ${index + 1}"
            />
        `).join('');

        // Show/hide nav buttons
        const showNav = this.photos.length > 1;
        this.elements.prevPhotoBtn.style.display = showNav ? 'flex' : 'none';
        this.elements.nextPhotoBtn.style.display = showNav ? 'flex' : 'none';
    }

    renderBidHistory() {
        if (this.bids.length === 0) {
            this.elements.bidHistoryContent.innerHTML = `
                <p class="no-bids-message">No bids yet. Be the first to bid!</p>
            `;
            return;
        }

        const sortedBids = [...this.bids].sort((a, b) => b.amount - a.amount);
        
        this.elements.bidHistoryContent.innerHTML = `
            <div class="bid-history-list">
                ${sortedBids.map((bid, index) => `
                    <div class="bid-history-item ${index === 0 ? 'winning' : ''}">
                        <div class="bidder-info">
                            <div class="bidder-avatar">${this.getInitials(bid.bidderName || 'Anonymous')}</div>
                            <div>
                                <div class="bidder-name">
                                    ${bid.bidderName || 'Bidder'}
                                    ${index === 0 ? '<span class="winning-badge">Highest</span>' : ''}
                                </div>
                                <div class="bid-time">${this.formatRelativeTime(bid.placedAt)}</div>
                            </div>
                        </div>
                        <div class="bid-amount">${this.formatCurrency(bid.amount)}</div>
                    </div>
                `).join('')}
            </div>
        `;
    }

    updateCurrentBid(amount) {
        const currentBid = amount || this.auction.startPrice;
        this.elements.currentBidAmount.textContent = this.formatCurrency(currentBid);
        this.elements.totalBidsCount.textContent = this.bids.length;

        // Update minimum bid
        const minBid = (amount || this.auction.startPrice) + this.minimumBidIncrement;
        this.elements.minimumBid.textContent = this.formatCurrency(minBid);
        this.elements.bidAmountInput.min = minBid;
        this.elements.bidAmountInput.placeholder = minBid.toLocaleString();

        // Update reserve status
        if (this.auction.reservePrice) {
            const reserveMet = currentBid >= this.auction.reservePrice;
            this.elements.reserveStatus.textContent = reserveMet ? 'Met ✓' : 'Not Met';
            this.elements.reserveStatus.className = `reserve-status ${reserveMet ? 'met' : 'not-met'}`;
        }
    }

    updateAuctionStatus() {
        const now = new Date();
        const endTime = new Date(this.auction.endTime);
        const startTime = new Date(this.auction.startTime);

        if (now < startTime) {
            this.elements.auctionStatusBadge.textContent = 'Starting Soon';
            this.elements.auctionStatusBadge.className = 'status-badge';
            this.showBidForm(false);
        } else if (now >= endTime) {
            this.elements.auctionStatusBadge.textContent = 'Ended';
            this.elements.auctionStatusBadge.className = 'status-badge ended';
            this.showAuctionEnded();
        } else {
            const timeLeft = endTime - now;
            const hoursLeft = timeLeft / (1000 * 60 * 60);

            if (hoursLeft < 1) {
                this.elements.auctionStatusBadge.textContent = 'Ending Soon!';
                this.elements.auctionStatusBadge.className = 'status-badge ending-soon';
            } else {
                this.elements.auctionStatusBadge.textContent = 'Active';
                this.elements.auctionStatusBadge.className = 'status-badge active';
            }
            this.showBidForm(true);
        }
    }

    showBidForm(show) {
        if (show) {
            if (this.isAuthenticated) {
                this.elements.bidFormSection.classList.remove('hidden');
                this.elements.loginPrompt.classList.add('hidden');
            } else {
                this.elements.bidFormSection.classList.add('hidden');
                this.elements.loginPrompt.classList.remove('hidden');
            }
            this.elements.auctionEndedSection.classList.add('hidden');
        } else {
            this.elements.bidFormSection.classList.add('hidden');
            this.elements.loginPrompt.classList.add('hidden');
        }
    }

    showAuctionEnded() {
        this.elements.bidFormSection.classList.add('hidden');
        this.elements.loginPrompt.classList.add('hidden');
        this.elements.auctionEndedSection.classList.remove('hidden');

        if (this.bids.length > 0) {
            const winningBid = [...this.bids].sort((a, b) => b.amount - a.amount)[0];
            this.elements.winnerInfo.textContent = `Winning bid: ${this.formatCurrency(winningBid.amount)}`;
        } else {
            this.elements.winnerInfo.textContent = 'No bids were placed.';
        }

        // Stop countdown
        if (this.countdownInterval) {
            clearInterval(this.countdownInterval);
        }
    }

    startCountdown() {
        this.updateCountdown();
        this.countdownInterval = setInterval(() => this.updateCountdown(), 1000);
    }

    updateCountdown() {
        const now = new Date();
        const endTime = new Date(this.auction.endTime);
        const diff = endTime - now;

        if (diff <= 0) {
            this.elements.countdownDays.textContent = '0';
            this.elements.countdownHours.textContent = '0';
            this.elements.countdownMinutes.textContent = '0';
            this.elements.countdownSeconds.textContent = '0';
            this.updateAuctionStatus();
            return;
        }

        const days = Math.floor(diff / (1000 * 60 * 60 * 24));
        const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((diff % (1000 * 60)) / 1000);

        this.elements.countdownDays.textContent = days;
        this.elements.countdownHours.textContent = hours;
        this.elements.countdownMinutes.textContent = minutes;
        this.elements.countdownSeconds.textContent = seconds;

        // Update status if ending soon
        this.updateAuctionStatus();
    }

    async setupSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/bidding')
                .withAutomaticReconnect()
                .build();

            // Handle new bids
            this.connection.on('NewBid', (bidData) => {
                this.handleNewBid(bidData);
            });

            // Handle user joined/left (optional UI updates)
            this.connection.on('UserJoined', (connectionId) => {
                console.log('User joined auction');
            });

            this.connection.on('UserLeft', (connectionId) => {
                console.log('User left auction');
            });

            await this.connection.start();
            console.log('SignalR connected');

            // Join the auction room
            await this.connection.invoke('JoinAuctionRoom', this.auctionId.toString());
        } catch (error) {
            console.error('SignalR connection failed:', error);
        }
    }

    handleNewBid(bidData) {
        // Add to bids array
        this.bids.unshift(bidData);

        // Update UI
        this.updateCurrentBid(bidData.amount);
        this.renderBidHistory();

        // Flash animation
        this.elements.currentBidAmount.classList.add('new-bid-flash');
        setTimeout(() => {
            this.elements.currentBidAmount.classList.remove('new-bid-flash');
        }, 500);

        // Show toast for other users' bids
        if (!bidData.isOwnBid) {
            this.showToast(`New bid: ${this.formatCurrency(bidData.amount)}`, false);
        }
    }

    setupEventListeners() {
        // Photo navigation
        this.elements.prevPhotoBtn.addEventListener('click', () => this.navigatePhoto(-1));
        this.elements.nextPhotoBtn.addEventListener('click', () => this.navigatePhoto(1));
        this.elements.thumbnailStrip.addEventListener('click', (e) => {
            if (e.target.classList.contains('thumbnail')) {
                this.currentPhotoIndex = parseInt(e.target.dataset.index);
                this.renderPhotos();
            }
        });

        // Quick bid buttons
        document.querySelectorAll('.quick-bid-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const increment = parseInt(btn.dataset.increment);
                const currentMin = parseInt(this.elements.bidAmountInput.min) || 0;
                this.elements.bidAmountInput.value = currentMin + increment - this.minimumBidIncrement;
            });
        });

        // Place bid button
        this.elements.placeBidBtn.addEventListener('click', () => this.showBidConfirmation());

        // Enter key on bid input
        this.elements.bidAmountInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.showBidConfirmation();
            }
        });

        // Modal buttons
        this.elements.cancelBidBtn.addEventListener('click', () => this.hideBidConfirmation());
        this.elements.confirmBidBtn.addEventListener('click', () => this.placeBid());

        // Close modal on overlay click
        this.elements.bidConfirmModal.addEventListener('click', (e) => {
            if (e.target === this.elements.bidConfirmModal) {
                this.hideBidConfirmation();
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && !this.elements.bidConfirmModal.classList.contains('hidden')) {
                this.hideBidConfirmation();
            }
            if (e.key === 'ArrowLeft') this.navigatePhoto(-1);
            if (e.key === 'ArrowRight') this.navigatePhoto(1);
        });
    }

    navigatePhoto(direction) {
        if (this.photos.length <= 1) return;
        
        this.currentPhotoIndex += direction;
        if (this.currentPhotoIndex < 0) this.currentPhotoIndex = this.photos.length - 1;
        if (this.currentPhotoIndex >= this.photos.length) this.currentPhotoIndex = 0;
        
        this.renderPhotos();
    }

    showBidConfirmation() {
        const amount = parseFloat(this.elements.bidAmountInput.value);
        const minBid = parseFloat(this.elements.bidAmountInput.min);

        if (!amount || isNaN(amount)) {
            this.showToast('Please enter a bid amount', true);
            return;
        }

        if (amount < minBid) {
            this.showToast(`Minimum bid is ${this.formatCurrency(minBid)}`, true);
            return;
        }

        this.elements.confirmBidAmount.textContent = this.formatCurrency(amount);
        this.elements.bidConfirmModal.classList.remove('hidden');
    }

    hideBidConfirmation() {
        this.elements.bidConfirmModal.classList.add('hidden');
    }

    async placeBid() {
        const amount = parseFloat(this.elements.bidAmountInput.value);
        
        this.elements.confirmBidBtn.disabled = true;
        this.elements.confirmBidBtn.textContent = 'Placing Bid...';

        try {
            const response = await fetch(`/api/auctions/${this.auctionId}/bids`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    auctionId: this.auctionId,
                    amount: amount
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to place bid');
            }

            const result = await response.json();

            // Success
            this.hideBidConfirmation();
            this.elements.bidAmountInput.value = '';
            this.showToast('Bid placed successfully!', false);

            // The SignalR will handle updating the UI with the new bid
            // But we can also update immediately for responsiveness
            this.handleNewBid({ ...result, isOwnBid: true });

        } catch (error) {
            console.error('Error placing bid:', error);
            this.showToast(error.message || 'Failed to place bid. Please try again.', true);
        } finally {
            this.elements.confirmBidBtn.disabled = false;
            this.elements.confirmBidBtn.textContent = 'Confirm Bid';
        }
    }

    showToast(message, isError) {
        this.elements.toastMessage.textContent = message;
        this.elements.bidToast.classList.remove('hidden', 'error');
        if (isError) {
            this.elements.bidToast.classList.add('error');
        }

        setTimeout(() => {
            this.elements.bidToast.classList.add('hidden');
        }, 4000);
    }

    // Utility methods
    formatCurrency(value) {
        if (value === null || value === undefined) return '$0';
        return new Intl.NumberFormat('en-CA', {
            style: 'currency',
            currency: 'CAD',
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(value);
    }

    formatDateTime(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString('en-CA', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: 'numeric',
            minute: '2-digit',
            hour12: true
        });
    }

    formatRelativeTime(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diff = now - date;

        const minutes = Math.floor(diff / 60000);
        const hours = Math.floor(diff / 3600000);
        const days = Math.floor(diff / 86400000);

        if (minutes < 1) return 'Just now';
        if (minutes < 60) return `${minutes}m ago`;
        if (hours < 24) return `${hours}h ago`;
        return `${days}d ago`;
    }

    getInitials(name) {
        return name
            .split(' ')
            .map(n => n[0])
            .join('')
            .toUpperCase()
            .slice(0, 2);
    }
}

function showError() {
    document.getElementById('loadingState').classList.add('hidden');
    document.getElementById('errorState').classList.remove('hidden');
}

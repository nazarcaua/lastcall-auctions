(async function(){
    const path = window.location.pathname.split('/');
    const id = path[path.length-1];
    const msg = document.getElementById('message');
    const form = document.getElementById('editForm');
    let listingId = null; // will be set once auction is loaded
    const newPhotoFiles = [];

    function getAuthHeaders() {
        const token = localStorage.getItem('authToken');
        const headers = {};
        if (token) headers['Authorization'] = `Bearer ${token}`;
        return headers;
    }

    // --- Photo management ---
    function renderExistingPhotos(photos, containerId) {
        const container = document.getElementById(containerId);
        container.innerHTML = '';
        if (!photos || photos.length === 0) return;

        photos.forEach(url => {
            const item = document.createElement('div');
            item.className = 'photo-preview-item';

            const img = document.createElement('img');
            img.src = url;
            img.alt = 'Listing photo';
            item.appendChild(img);

            const removeBtn = document.createElement('button');
            removeBtn.className = 'remove-photo';
            removeBtn.type = 'button';
            removeBtn.textContent = '×';
            removeBtn.addEventListener('click', async () => {
                if (!confirm('Delete this photo?')) return;
                const fileName = url.split('/').pop();
                const resp = await fetch(`/api/listings/${listingId}/photos/${fileName}`, {
                    method: 'DELETE',
                    headers: getAuthHeaders(),
                    credentials: 'include'
                });
                if (resp.ok) {
                    item.remove();
                } else {
                    alert('Failed to delete photo.');
                }
            });
            item.appendChild(removeBtn);
            container.appendChild(item);
        });
    }

    // New photo file input
    const fileInput = document.getElementById('newPhotos');
    const uploadArea = fileInput.closest('.photo-upload-area');

    fileInput.addEventListener('change', () => {
        addNewPhotoPreviews(fileInput.files);
        fileInput.value = '';
    });

    uploadArea.addEventListener('dragover', (e) => { e.preventDefault(); uploadArea.classList.add('dragover'); });
    uploadArea.addEventListener('dragleave', () => uploadArea.classList.remove('dragover'));
    uploadArea.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadArea.classList.remove('dragover');
        addNewPhotoPreviews(e.dataTransfer.files);
    });

    function addNewPhotoPreviews(fileList) {
        const allowed = ['.jpg', '.jpeg', '.png', '.webp'];
        const maxSize = 5 * 1024 * 1024;
        const container = document.getElementById('existingPhotos');

        for (const file of fileList) {
            const ext = '.' + file.name.split('.').pop().toLowerCase();
            if (!allowed.includes(ext)) { alert(`File type '${ext}' is not allowed.`); continue; }
            if (file.size > maxSize) { alert(`File '${file.name}' exceeds the 5 MB limit.`); continue; }

            const idx = newPhotoFiles.length;
            newPhotoFiles.push(file);

            const item = document.createElement('div');
            item.className = 'photo-preview-item';
            item.dataset.newIndex = idx;

            const img = document.createElement('img');
            img.src = URL.createObjectURL(file);
            img.alt = file.name;
            item.appendChild(img);

            const removeBtn = document.createElement('button');
            removeBtn.className = 'remove-photo';
            removeBtn.type = 'button';
            removeBtn.textContent = '×';
            removeBtn.addEventListener('click', () => {
                newPhotoFiles[idx] = null;
                item.remove();
            });
            item.appendChild(removeBtn);
            container.appendChild(item);
        }
    }

    // --- Load auction and listing data ---
    try {
        const res = await fetch(`/api/auctions/${id}`);
        if(!res.ok){ msg.textContent = 'Failed to load auction'; return; }
        const auction = await res.json();
        listingId = auction.listingId;

        // Fill auction fields
        document.querySelector('input[name="startPrice"]').value = auction.startPrice || '';
        document.querySelector('input[name="reservePrice"]').value = auction.reservePrice || '';
        if(auction.endTime){
            const dt = new Date(auction.endTime);
            const local = new Date(dt.getTime() - dt.getTimezoneOffset()*60000).toISOString().slice(0,16);
            document.querySelector('input[name="endTime"]').value = local;
        }

        // Load listing details
        const listingRes = await fetch(`/api/listings/${listingId}`);
        if(!listingRes.ok){ msg.textContent = 'Failed to load listing'; return; }
        const listing = await listingRes.json();

        document.querySelector('input[name="title"]').value = listing.title || '';
        document.querySelector('textarea[name="description"]').value = listing.description || '';

        // Load existing photos
        renderExistingPhotos(listing.photoUrls || [], 'existingPhotos');

        // Populate years/makes/models selects
        const years = await fetch('/api/vehicles/years').then(r => r.json());
        const yearSelect = document.querySelector('select[name="year"]');
        years.forEach(y => { const opt = document.createElement('option'); opt.value = y.year; opt.textContent = y.year; yearSelect.appendChild(opt); });
        yearSelect.value = listing.year;

        const makeSelect = document.querySelector('select[name="makeId"]');
        const modelSelect = document.querySelector('select[name="modelId"]');

        async function loadMakes() {
            const year = yearSelect.value;
            const makes = await fetch(`/api/vehicles/years/${year}/makes`).then(r => r.json());
            makeSelect.innerHTML = '';
            makes.forEach(m => { const o = document.createElement('option'); o.value = m.makeId; o.textContent = m.name; makeSelect.appendChild(o); });
            await loadModels();
        }
        async function loadModels(){
            const year = yearSelect.value; const makeId = makeSelect.value; if(!makeId) return;
            const models = await fetch(`/api/vehicles/years/${year}/makes/${makeId}/models`).then(r => r.json());
            modelSelect.innerHTML = '';
            models.forEach(m => { const o = document.createElement('option'); o.value = m.modelId; o.textContent = m.name; modelSelect.appendChild(o); });
            modelSelect.value = listing.modelId;
        }

        yearSelect.addEventListener('change', loadMakes);
        makeSelect.addEventListener('change', loadModels);

        // set other listing details
        document.querySelector('input[name="vin"]').value = listing.vin || '';
        document.querySelector('input[name="mileage"]').value = listing.mileage || '';
        document.querySelector('select[name="conditionGrade"]').value = listing.conditionGrade || '5';
        document.querySelector('input[name="city"]').value = listing.city || '';
        document.querySelector('input[name="region"]').value = listing.region || '';
        document.querySelector('input[name="country"]').value = listing.country || '';
        document.querySelector('input[name="postalCode"]').value = listing.postalCode || '';

    } catch(e){ msg.textContent = 'Network error loading auction/listing'; console.error(e); }

    // --- Save handler ---
    form.addEventListener('submit', async (e)=>{
        e.preventDefault();
        msg.textContent = 'Saving...';

        if (!listingId) { msg.textContent = 'Listing not loaded yet.'; return; }

        // Listing payload
        const listingPayload = {
            title: document.querySelector('input[name="title"]').value.trim(),
            description: document.querySelector('textarea[name="description"]').value.trim(),
            year: parseInt(document.querySelector('select[name="year"]').value,10),
            makeId: parseInt(document.querySelector('select[name="makeId"]').value,10),
            modelId: parseInt(document.querySelector('select[name="modelId"]').value,10),
            vin: document.querySelector('input[name="vin"]').value || null,
            mileage: document.querySelector('input[name="mileage"]').value ? parseInt(document.querySelector('input[name="mileage"]').value,10) : null,
            conditionGrade: parseInt(document.querySelector('select[name="conditionGrade"]').value,10),
            city: document.querySelector('input[name="city"]').value.trim(),
            region: document.querySelector('input[name="region"]').value.trim() || null,
            country: document.querySelector('input[name="country"]').value.trim(),
            postalCode: document.querySelector('input[name="postalCode"]').value.trim() || null
        };

        // Auction payload
        const auctionPayload = {
            startPrice: document.querySelector('input[name="startPrice"]').value ? parseFloat(document.querySelector('input[name="startPrice"]').value) : null,
            reservePrice: document.querySelector('input[name="reservePrice"]').value ? parseFloat(document.querySelector('input[name="reservePrice"]').value) : null,
            endTime: document.querySelector('input[name="endTime"]').value ? new Date(document.querySelector('input[name="endTime"]').value).toISOString() : null
        };

        try{
            const token = localStorage.getItem('authToken');
            const authHeaders = token ? { 'Authorization': `Bearer ${token}` } : {};

            // First update listing
            const listingResp = await fetch(`/api/listings/${listingId}`, {
                method: 'PATCH',
                headers: Object.assign({ 'Content-Type': 'application/json' }, authHeaders),
                credentials: 'include',
                body: JSON.stringify(listingPayload)
            });
            const listingJson = await listingResp.json();
            if(!listingResp.ok){ msg.textContent = listingJson.message || 'Failed to save listing'; return; }

            // Then update auction
            const auctionResp = await fetch(`/api/auctions/${id}`, {
                method: 'PATCH',
                headers: Object.assign({ 'Content-Type': 'application/json' }, authHeaders),
                credentials: 'include',
                body: JSON.stringify(auctionPayload)
            });
            const auctionJson = await auctionResp.json();
            if(!auctionResp.ok){ msg.textContent = auctionJson.message || 'Failed to save auction'; return; }

            // Upload new photos if any
            const validNewFiles = newPhotoFiles.filter(f => f !== null);
            if (validNewFiles.length > 0) {
                msg.textContent = 'Uploading new photos...';
                const formData = new FormData();
                validNewFiles.forEach(f => formData.append('photos', f));

                const photoResp = await fetch(`/api/listings/${listingId}/photos`, {
                    method: 'POST',
                    headers: authHeaders,
                    credentials: 'include',
                    body: formData
                });
                const photoJson = await photoResp.json().catch(() => null);
                if (!photoResp.ok) {
                    msg.textContent = `Saved but photo upload failed: ${photoJson?.message || 'Unknown error'}`;
                    setTimeout(() => window.location.href = '/Seller/Dashboard', 2000);
                    return;
                }
            }

            msg.textContent = 'Saved. Redirecting...';
            setTimeout(()=> window.location.href = '/Seller/Dashboard',700);
        }catch(e){ msg.textContent = 'Network error saving'; console.error(e); }
    });
})();

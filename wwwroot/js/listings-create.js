const api = {
    getYears: () => fetch('/api/vehicles/years').then(r => r.json()),
    getMakes: (year) => fetch(`/api/vehicles/years/${year}/makes`).then(r => r.json()),
    getModels: (year, makeId) => fetch(`/api/vehicles/years/${year}/makes/${makeId}/models`).then(r => r.json()),
    createListing: (payload) => fetch('/api/listings', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        credentials: 'include',
        body: JSON.stringify(payload)
    }).then(async r => ({ ok: r.ok, status: r.status, body: await r.json().catch(() => null) }))
};

// Store selected photo files per vehicle form index
const photoFilesByIndex = {};

async function createVehicleForm(index) {
    const container = document.createElement('div');
    container.className = 'vehicle-form listing-card';
    container.dataset.index = index;
    container.innerHTML = `
        <h3>?? Vehicle ${index + 1}</h3>
        <button type="button" class="remove">Remove</button>

        <label>Vehicle Title</label>
        <input name="title" placeholder="e.g. 2018 Honda Civic Touring - Low KM" required />

        <div class="row">
            <div>
                <label>Year</label>
                <select name="year" required></select>
            </div>
            <div>
                <label>Make</label>
                <select name="makeId" required></select>
            </div>
            <div>
                <label>Model</label>
                <select name="modelId" required></select>
            </div>
        </div>

        <div class="row">
            <div>
                <label>Kilometers</label>
                <input name="mileage" type="number" min="0" placeholder="e.g. 85000" />
            </div>
            <div>
                <label>Condition</label>
                <select name="conditionGrade" required>
                    <option value="5">5 - Excellent</option>
                    <option value="4">4 - Very Good</option>
                    <option value="3" selected>3 - Good</option>
                    <option value="2">2 - Fair</option>
                    <option value="1">1 - Poor</option>
                </select>
            </div>
        </div>

        <label>Description</label>
        <textarea name="description" placeholder="Describe the vehicle's features, history, and condition..."></textarea>

        <div class="row">
            <div>
                <label>Starting Price (CAD)</label>
                <input name="startPrice" type="number" step="100" min="0" placeholder="e.g. 15000" required />
            </div>
            <div>
                <label>Reserve Price (optional)</label>
                <input name="reservePrice" type="number" step="100" min="0" placeholder="Minimum to sell" />
            </div>
        </div>

        <label>Photos (max 10, up to 5 MB each)</label>
        <div class="photo-upload-area">
            <input type="file" name="photos" multiple accept=".jpg,.jpeg,.png,.webp" />
            <div class="upload-text"><strong>Click or drag</strong> to add photos (.jpg, .png, .webp)</div>
        </div>
        <div class="photo-preview-grid" data-previews></div>
    `;

    // Initialize photo file storage for this form
    photoFilesByIndex[index] = [];

    // Photo file input handling
    const fileInput = container.querySelector('input[name="photos"]');
    const previewGrid = container.querySelector('[data-previews]');
    const uploadArea = container.querySelector('.photo-upload-area');

    fileInput.addEventListener('change', () => {
        addPhotos(index, fileInput.files, previewGrid);
        fileInput.value = ''; // reset so same file can be re-selected
    });

    // Drag and drop
    uploadArea.addEventListener('dragover', (e) => { e.preventDefault(); uploadArea.classList.add('dragover'); });
    uploadArea.addEventListener('dragleave', () => uploadArea.classList.remove('dragover'));
    uploadArea.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadArea.classList.remove('dragover');
        addPhotos(index, e.dataTransfer.files, previewGrid);
    });

    // Populate years
    const yearSelect = container.querySelector('select[name="year"]');

    // Add a placeholder option
    const placeholderYear = document.createElement('option');
    placeholderYear.value = '';
    placeholderYear.textContent = 'Select Year';
    placeholderYear.disabled = true;
    placeholderYear.selected = true;
    yearSelect.appendChild(placeholderYear);

    try {
        const response = await fetch('/api/vehicles/years');
        if (!response.ok) {
            console.error('Failed to fetch years:', response.status);
            return container;
        }
        const years = await response.json();
        console.log('Years loaded:', years.length);

        years.forEach(y => {
            const opt = document.createElement('option');
            opt.value = y.year;
            opt.textContent = y.year;
            yearSelect.appendChild(opt);
        });
    } catch (error) {
        console.error('Error fetching years:', error);
    }

    // Populate makes when year changes
    const makeSelect = container.querySelector('select[name="makeId"]');
    const modelSelect = container.querySelector('select[name="modelId"]');

    // Add placeholder for make
    const placeholderMake = document.createElement('option');
    placeholderMake.value = '';
    placeholderMake.textContent = 'Select Make';
    placeholderMake.disabled = true;
    placeholderMake.selected = true;
    makeSelect.appendChild(placeholderMake);

    // Add placeholder for model
    const placeholderModel = document.createElement('option');
    placeholderModel.value = '';
    placeholderModel.textContent = 'Select Model';
    placeholderModel.disabled = true;
    placeholderModel.selected = true;
    modelSelect.appendChild(placeholderModel);

    async function loadMakes() {
        const year = yearSelect.value;
        if (!year) return;

        try {
            const makes = await fetch(`/api/vehicles/years/${year}/makes`).then(r => r.json());
            makeSelect.innerHTML = '<option value="" disabled selected>Select Make</option>';
            makes.forEach(m => {
                const opt = document.createElement('option');
                opt.value = m.makeId;
                opt.textContent = m.name;
                makeSelect.appendChild(opt);
            });
            // Reset models when year changes
            modelSelect.innerHTML = '<option value="" disabled selected>Select Model</option>';
        } catch (error) {
            console.error('Error loading makes:', error);
        }
    }

    async function loadModels() {
        const year = yearSelect.value;
        const makeId = makeSelect.value;
        if (!year || !makeId) return;

        try {
            const models = await fetch(`/api/vehicles/years/${year}/makes/${makeId}/models`).then(r => r.json());
            modelSelect.innerHTML = '<option value="" disabled selected>Select Model</option>';
            models.forEach(m => {
                const opt = document.createElement('option');
                opt.value = m.modelId;
                opt.textContent = m.name;
                modelSelect.appendChild(opt);
            });
        } catch (error) {
            console.error('Error loading models:', error);
        }
    }

    yearSelect.addEventListener('change', loadMakes);
    makeSelect.addEventListener('change', loadModels);

    container.querySelector('.remove').addEventListener('click', (e) => {
        e.preventDefault();
        delete photoFilesByIndex[index];
        container.remove();
        // Update empty state after removal
        const formsCount = document.querySelectorAll('.vehicle-form').length;
        const emptyState = document.getElementById('emptyState');
        if (emptyState) {
            emptyState.classList.toggle('hidden', formsCount > 0);
        }
    });

    return container;
}

function addPhotos(formIndex, fileList, previewGrid) {
    const allowed = ['.jpg', '.jpeg', '.png', '.webp'];
    const maxSize = 5 * 1024 * 1024;
    const maxCount = 10;

    for (const file of fileList) {
        if (photoFilesByIndex[formIndex].length >= maxCount) {
            alert(`Maximum ${maxCount} photos per vehicle.`);
            break;
        }
        const ext = '.' + file.name.split('.').pop().toLowerCase();
        if (!allowed.includes(ext)) {
            alert(`File type '${ext}' is not allowed. Use .jpg, .jpeg, .png, or .webp.`);
            continue;
        }
        if (file.size > maxSize) {
            alert(`File '${file.name}' exceeds the 5 MB limit.`);
            continue;
        }
        photoFilesByIndex[formIndex].push(file);
        addPreviewThumbnail(formIndex, file, photoFilesByIndex[formIndex].length - 1, previewGrid);
    }
}

function addPreviewThumbnail(formIndex, file, fileIndex, previewGrid) {
    const item = document.createElement('div');
    item.className = 'photo-preview-item';
    item.dataset.fileIndex = fileIndex;

    const img = document.createElement('img');
    img.src = URL.createObjectURL(file);
    img.alt = file.name;
    item.appendChild(img);

    const removeBtn = document.createElement('button');
    removeBtn.className = 'remove-photo';
    removeBtn.type = 'button';
    removeBtn.textContent = '×';
    removeBtn.addEventListener('click', () => {
        photoFilesByIndex[formIndex][fileIndex] = null; // mark as removed
        item.remove();
    });
    item.appendChild(removeBtn);

    previewGrid.appendChild(item);
}

async function uploadPhotosForListing(listingId, files) {
    // Filter out null entries (removed photos)
    const validFiles = files.filter(f => f !== null);
    if (validFiles.length === 0) return { ok: true, uploaded: 0 };

    const formData = new FormData();
    validFiles.forEach(f => formData.append('photos', f));

    const token = localStorage.getItem('authToken');
    const headers = {};
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const resp = await fetch(`/api/listings/${listingId}/photos`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: formData
    });

    const json = await resp.json().catch(() => null);
    return { ok: resp.ok, uploaded: json?.urls?.length || 0, message: json?.message || '' };
}

(async function () {
    const formsRoot = document.getElementById('formsRoot');
    let index = 0;

    async function addForm() {
        const form = await createVehicleForm(index);
        formsRoot.appendChild(form);
        index++;
    }

    document.getElementById('addBtn').addEventListener('click', async (e) => {
        e.preventDefault();
        await addForm();
    });

    document.getElementById('submitBtn').addEventListener('click', async (e) => {
        e.preventDefault();
        const resultDiv = document.getElementById('status');
        resultDiv.className = 'status';
        resultDiv.textContent = '';

        const vehicleForms = document.querySelectorAll('.vehicle-form');
        if (vehicleForms.length === 0) {
            alert('Add at least one vehicle.');
            return;
        }

        // Collect vehicle data and their form indices (for photo mapping)
        const vehiclesData = [];
        const formIndices = [];

        for (const vf of vehicleForms) {
            const formIdx = parseInt(vf.dataset.index, 10);
            formIndices.push(formIdx);

            vehiclesData.push({
                title: vf.querySelector('input[name="title"]').value,
                year: parseInt(vf.querySelector('select[name="year"]').value, 10),
                makeId: parseInt(vf.querySelector('select[name="makeId"]').value, 10),
                modelId: parseInt(vf.querySelector('select[name="modelId"]').value, 10),
                vin: null,
                mileage: vf.querySelector('input[name="mileage"]').value ? parseInt(vf.querySelector('input[name="mileage"]').value, 10) : null,
                conditionGrade: parseInt(vf.querySelector('select[name="conditionGrade"]').value, 10),
                description: vf.querySelector('textarea[name="description"]').value || null,
                startPrice: vf.querySelector('input[name="startPrice"]').value ? parseFloat(vf.querySelector('input[name="startPrice"]').value) : 0,
                reservePrice: vf.querySelector('input[name="reservePrice"]').value ? parseFloat(vf.querySelector('input[name="reservePrice"]').value) : null
            });
        }

        const payload = {
            auctionGroupTitle: document.getElementById('groupTitle').value || null,
            vehicles: vehiclesData,
            startTime: document.getElementById('groupStartTime').value ? new Date(document.getElementById('groupStartTime').value).toISOString() : null,
            endTime: document.getElementById('groupEndTime').value ? new Date(document.getElementById('groupEndTime').value).toISOString() : null
        };

        try {
            resultDiv.textContent = 'Creating listings...';
            const token = localStorage.getItem('authToken');

            const response = await fetch('/api/listings', {
                method: 'POST',
                headers: Object.assign({ 'Content-Type': 'application/json' }, token ? { 'Authorization': `Bearer ${token}` } : {}),
                credentials: 'include',
                body: JSON.stringify(payload)
            });

            const resJson = await response.json();
            if (!response.ok) {
                resultDiv.className = 'status error';
                resultDiv.textContent = resJson.message || 'Failed to create listings';
                return;
            }

            // resJson is an array of ListingResponseDto objects
            const createdListings = Array.isArray(resJson) ? resJson : [];

            // Upload photos for each listing
            let photoErrors = [];
            let totalUploaded = 0;

            for (let i = 0; i < createdListings.length && i < formIndices.length; i++) {
                const listing = createdListings[i];
                const formIdx = formIndices[i];
                const files = photoFilesByIndex[formIdx] || [];
                const validFiles = files.filter(f => f !== null);

                if (validFiles.length > 0) {
                    resultDiv.textContent = `Uploading photos for "${listing.title}"... (${i + 1}/${createdListings.length})`;
                    const result = await uploadPhotosForListing(listing.listingId, files);
                    if (result.ok) {
                        totalUploaded += result.uploaded;
                    } else {
                        photoErrors.push(`${listing.title}: ${result.message}`);
                    }
                }
            }

            if (photoErrors.length > 0) {
                resultDiv.className = 'status error';
                resultDiv.textContent = `Listings created but some photos failed: ${photoErrors.join('; ')}`;
                // Still redirect after a delay so they can read the message
                setTimeout(() => window.location.href = '/Seller/Dashboard', 3000);
            } else {
                resultDiv.className = 'status success';
                const photoMsg = totalUploaded > 0 ? ` ${totalUploaded} photo(s) uploaded.` : '';
                resultDiv.textContent = `Listings created successfully.${photoMsg} Redirecting...`;
                setTimeout(() => window.location.href = '/Seller/Dashboard', 900);
            }
        } catch (err) {
            resultDiv.className = 'status error';
            resultDiv.textContent = 'Network error';
            console.error(err);
        }
    });

    // Add first form by default
    await addForm();

    // Update empty state visibility
    function updateEmptyState() {
        const formsCount = document.querySelectorAll('.vehicle-form').length;
        const emptyState = document.getElementById('emptyState');
        if (emptyState) {
            emptyState.classList.toggle('hidden', formsCount > 0);
        }
    }

    // Initial empty state update
    updateEmptyState();

    // Schedule preview update
    function updateSchedulePreview() {
        const startInput = document.getElementById('groupStartTime');
        const endInput = document.getElementById('groupEndTime');
        const preview = document.getElementById('schedulePreview');
        const previewText = document.getElementById('scheduleText');

        if (!endInput.value) {
            preview.classList.remove('active');
            previewText.textContent = 'Set the end time to see auction duration';
            return;
        }

        const startTime = startInput.value ? new Date(startInput.value) : new Date();
        const endTime = new Date(endInput.value);

        if (endTime <= startTime) {
            preview.classList.remove('active');
            previewText.textContent = '?? End time must be after start time';
            return;
        }

        const diffMs = endTime - startTime;
        const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
        const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

        let durationText = '';
        if (diffDays > 0) durationText += `${diffDays} day${diffDays > 1 ? 's' : ''} `;
        if (diffHours > 0) durationText += `${diffHours} hour${diffHours > 1 ? 's' : ''} `;
        if (diffMinutes > 0 && diffDays === 0) durationText += `${diffMinutes} minute${diffMinutes > 1 ? 's' : ''}`;

        const startLabel = startInput.value ? 
            `Starts: ${startTime.toLocaleString()}` : 
            'Starts: Immediately';
        const endLabel = `Ends: ${endTime.toLocaleString()}`;

        preview.classList.add('active');
        previewText.innerHTML = `<strong>${durationText.trim()}</strong> — ${startLabel} | ${endLabel}`;
    }

    document.getElementById('groupStartTime').addEventListener('change', updateSchedulePreview);
    document.getElementById('groupEndTime').addEventListener('change', updateSchedulePreview);

    // Update empty state on form changes
    updateEmptyState();

    // Override the add form to update empty state
    const originalAddBtn = document.getElementById('addBtn');
    originalAddBtn.addEventListener('click', () => {
        setTimeout(updateEmptyState, 100);
    });
})();

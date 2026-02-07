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

function createCard(index) {
    const card = document.createElement('div');
    card.className = 'listing-card';
    card.dataset.index = index;
    card.innerHTML = `
        <div class="card-header">
            <h3>Vehicle ${index + 1}</h3>
            <button class="remove">Remove</button>
        </div>
        <div class="card-body">
            <label>Title</label>
            <input name="title" placeholder="e.g. 2018 Honda Civic Touring" required />

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
                    <input name="mileage" type="number" min="0" />
                </div>
                <div>
                    <label>Condition Grade</label>
                    <select name="conditionGrade" required>
                        <option value="5">5 - Excellent</option>
                        <option value="4">4 - Very Good</option>
                        <option value="3">3 - Good</option>
                        <option value="2">2 - Fair</option>
                        <option value="1">1 - Poor</option>
                    </select>
                </div>
            </div>

            <label>Description</label>
            <textarea name="description" rows="3"></textarea>

            <div class="row">
                <div>
                    <label>Starting Price</label>
                    <input name="startPrice" type="number" step="0.01" required />
                </div>
                <div>
                    <label>Reserve Price</label>
                    <input name="reservePrice" type="number" step="0.01" />
                </div>
                <div>
                    <label>Auction End (local)</label>
                    <input name="endTime" type="datetime-local" required />
                </div>
            </div>

            <label>Location - City</label>
            <input name="city" required />
            <div class="row">
                <div>
                    <label>Region</label>
                    <input name="region" />
                </div>
                <div>
                    <label>Country</label>
                    <input name="country" required value="Canada" />
                </div>
                <div>
                    <label>Postal Code</label>
                    <input name="postalCode" />
                </div>
            </div>

        </div>
    `;

    const removeBtn = card.querySelector('.remove');
    removeBtn.addEventListener('click', (e) => { e.preventDefault(); card.remove(); updateIndices(); });

    // Populate years & makes & models
    const yearSelect = card.querySelector('select[name="year"]');
    const makeSelect = card.querySelector('select[name="makeId"]');
    const modelSelect = card.querySelector('select[name="modelId"]');

    api.getYears().then(years => {
        years.forEach(y => {
            const opt = document.createElement('option'); opt.value = y.year; opt.textContent = y.year; yearSelect.appendChild(opt);
        });
        // trigger load makes
        loadMakes();
    });

    async function loadMakes() {
        const year = yearSelect.value;
        try {
            const makes = await api.getMakes(year);
            makeSelect.innerHTML = '';
            makes.forEach(m => { const o = document.createElement('option'); o.value = m.makeId; o.textContent = m.name; makeSelect.appendChild(o); });
            await loadModels();
        } catch { /* ignore */ }
    }

    async function loadModels() {
        const year = yearSelect.value; const makeId = makeSelect.value; if (!makeId) return;
        try {
            const models = await api.getModels(year, makeId);
            modelSelect.innerHTML = '';
            models.forEach(m => { const o = document.createElement('option'); o.value = m.modelId; o.textContent = m.name; modelSelect.appendChild(o); });
        } catch { }
    }

    yearSelect.addEventListener('change', loadMakes);
    makeSelect.addEventListener('change', loadModels);

    return card;
}

function updateIndices() {
    document.querySelectorAll('.listing-card').forEach((c, i) => c.querySelector('h3').textContent = `Vehicle ${i + 1}`);
}

(async function init() {
    const root = document.getElementById('formsRoot');
    const addBtn = document.getElementById('addBtn');
    const submitBtn = document.getElementById('submitBtn');
    const status = document.getElementById('status');

    function setStatus(msg, isError = false) { status.textContent = msg; status.className = isError ? 'status error' : 'status'; }

    addBtn.addEventListener('click', async (e) => { e.preventDefault(); const c = await createCard(document.querySelectorAll('.listing-card').length); root.appendChild(c); });

    // Add initial card
    root.appendChild(await createCard(0));

    submitBtn.addEventListener('click', async (e) => {
        e.preventDefault();
        setStatus('Submitting...');
        const cards = Array.from(document.querySelectorAll('.listing-card'));
        if (cards.length === 0) { setStatus('Add at least one listing', true); return; }

        for (const card of cards) {
            const payload = {
                title: card.querySelector('input[name="title"]').value.trim(),
                description: card.querySelector('textarea[name="description"]').value.trim(),
                year: parseInt(card.querySelector('select[name="year"]').value, 10),
                makeId: parseInt(card.querySelector('select[name="makeId"]').value, 10),
                modelId: parseInt(card.querySelector('select[name="modelId"]').value, 10),
                vin: null,
                mileage: card.querySelector('input[name="mileage"]').value ? parseInt(card.querySelector('input[name="mileage"]').value, 10) : null,
                conditionGrade: parseInt(card.querySelector('select[name="conditionGrade"]').value, 10),
                city: card.querySelector('input[name="city"]').value.trim(),
                region: card.querySelector('input[name="region"]').value.trim() || null,
                country: card.querySelector('input[name="country"]').value.trim(),
                postalCode: card.querySelector('input[name="postalCode"]').value.trim() || null,
                startPrice: card.querySelector('input[name="startPrice"]').value ? parseFloat(card.querySelector('input[name="startPrice"]').value) : null,
                reservePrice: card.querySelector('input[name="reservePrice"]').value ? parseFloat(card.querySelector('input[name="reservePrice"]').value) : null,
                endTime: card.querySelector('input[name="endTime"]').value ? new Date(card.querySelector('input[name="endTime"]').value).toISOString() : null
            };

            // Basic validation
            if (!payload.title || !payload.year || !payload.makeId || !payload.modelId || !payload.city || !payload.country) {
                setStatus('Please fill required fields on all cards', true);
                return;
            }

            // Create listing
            const res = await api.createListing(payload);
            if (!res.ok) {
                setStatus(res.body?.message || `Failed to create listing (status ${res.status})`, true);
                return;
            }
        }

        setStatus('All listings created. Redirecting...');
        setTimeout(() => window.location.href = '/Seller/Dashboard', 900);
    });
})();

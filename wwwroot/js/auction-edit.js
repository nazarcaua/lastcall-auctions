(async function() {
    const config = window.auctionEditData || {};
    const isGroup = config.isGroup;
    const auctionIds = config.auctionIds || [];
    const singleAuctionId = config.singleAuctionId;
    const groupId = config.groupId;
    const loadingState = document.getElementById("loadingState");
    const errorState = document.getElementById("errorState");
    const errorMessage = document.getElementById("errorMessage");
    const editContent = document.getElementById("editContent");
    const vehiclesContainer = document.getElementById("vehiclesContainer");
    const saveStatus = document.getElementById("saveStatus");
    const saveAllBtn = document.getElementById("saveAllBtn");
    const addVehicleBtn = document.getElementById("addVehicleBtn");
    let auctionsData = [];
    let newPhotosByAuction = {};
    let deletedPhotosByAuction = {};
    let newVehicles = [];
    let yearsCache = [];
    function getAuthHeaders() { const token = localStorage.getItem("authToken"); return token ? { "Authorization": "Bearer " + token } : {}; }
    function showError(msg) { loadingState.classList.add("hidden"); editContent.classList.add("hidden"); errorState.classList.remove("hidden"); errorMessage.textContent = msg; }
    function showContent() { loadingState.classList.add("hidden"); errorState.classList.add("hidden"); editContent.classList.remove("hidden"); }
    function formatLocalDateTime(iso) { if (!iso) return ""; const dt = new Date(iso); return new Date(dt.getTime() - dt.getTimezoneOffset() * 60000).toISOString().slice(0, 16); }
    function decodeHtml(html) { const txt = document.createElement("textarea"); txt.innerHTML = html || ""; return txt.value; }
    async function loadYears() { try { return await fetch("/api/vehicles/years").then(r => r.json()); } catch { return []; } }
    async function loadAuctionData(id) { const aRes = await fetch("/api/auctions/" + id, { headers: getAuthHeaders(), credentials: "include" }); if (!aRes.ok) throw new Error("Failed to load auction"); const auction = await aRes.json(); const lRes = await fetch("/api/listings/" + auction.listingId, { headers: getAuthHeaders(), credentials: "include" }); if (!lRes.ok) throw new Error("Failed to load listing"); return { auction, listing: await lRes.json() }; }
    function createVehicleCard(idx, auction, listing, isNew) {
        const card = document.createElement("div");
        card.className = "vehicle-edit-card" + (isNew ? " new-vehicle" : "");
        const aid = auction ? auction.auctionId : "new-" + Date.now();
        card.dataset.auctionId = aid;
        card.dataset.listingId = listing ? listing.listingId : "";
        card.dataset.isNew = isNew ? "true" : "false";
        const title = listing ? decodeHtml(listing.title) : "";
        const desc = listing ? decodeHtml(listing.description) : "";
        const vin = listing ? decodeHtml(listing.vin) : "";
        const makeName = listing ? decodeHtml(listing.makeName) : "";
        const modelName = listing ? decodeHtml(listing.modelName) : "";
        const headerText = isNew ? "New Vehicle" : (listing.year + " " + makeName + " " + modelName);
        const condGrade = listing ? listing.conditionGrade : 3;
        let html = '<div class="vehicle-card-header"><h3><span class="vehicle-number">' + (idx + 1) + '</span> ' + headerText;
        if (isNew) html += ' <span class="new-badge">NEW</span>';
        html += '</h3>';
        if (isNew) html += '<button type="button" class="btn-remove-vehicle">&times;</button>';
        html += '</div><div class="vehicle-card-body">';
        html += '<div class="form-group"><label>Title</label><input type="text" name="title" value="' + title.replace(/"/g, "&quot;") + '" placeholder="e.g. 2018 Honda Civic"/></div>';
        html += '<div class="form-row"><div class="form-group"><label>Year</label><select name="year" data-current="' + (listing ? listing.year : "") + '"></select></div>';
        html += '<div class="form-group"><label>Make</label><select name="makeId" data-current="' + (listing ? listing.makeId : "") + '"></select></div>';
        html += '<div class="form-group"><label>Model</label><select name="modelId" data-current="' + (listing ? listing.modelId : "") + '"></select></div></div>';
        html += '<div class="form-row-2"><div class="form-group"><label>Kilometers</label><input type="number" name="mileage" value="' + (listing && listing.mileage ? listing.mileage : "") + '" min="0"/></div>';
        html += '<div class="form-group"><label>Condition</label><select name="conditionGrade">';
        html += '<option value="5"' + (condGrade === 5 ? " selected" : "") + '>5 - Excellent</option>';
        html += '<option value="4"' + (condGrade === 4 ? " selected" : "") + '>4 - Very Good</option>';
        html += '<option value="3"' + (condGrade === 3 ? " selected" : "") + '>3 - Good</option>';
        html += '<option value="2"' + (condGrade === 2 ? " selected" : "") + '>2 - Fair</option>';
        html += '<option value="1"' + (condGrade === 1 ? " selected" : "") + '>1 - Poor</option></select></div></div>';
        html += '<div class="form-group"><label>Description</label><textarea name="description">' + desc + '</textarea></div>';
        html += '<div class="form-group"><label>VIN</label><input type="text" name="vin" value="' + vin.replace(/"/g, "&quot;") + '"/></div>';
        html += '<div class="photos-section"><h4>Photos</h4><div class="photo-grid" id="photos-' + aid + '">';
        if (!isNew && listing && listing.photoUrls) { listing.photoUrls.forEach(function(u) { html += '<div class="photo-item" data-url="' + u + '"><img src="' + u + '"/><button type="button" class="delete-photo-btn">&times;</button></div>'; }); }
        html += '</div><div class="photo-upload-zone" id="upload-' + aid + '"><input type="file" multiple accept=".jpg,.jpeg,.png,.webp"/><div class="upload-text"><strong>Click/drag</strong> to add</div></div></div>';
        html += '<div class="pricing-section"><h4>Pricing</h4><div class="form-row-2">';
        html += '<div class="form-group"><label>Start Price (CAD)</label><input type="number" name="startPrice" value="' + (auction && auction.startPrice ? auction.startPrice : "") + '" step="100" min="0"/></div>';
        html += '<div class="form-group"><label>Reserve Price</label><input type="number" name="reservePrice" value="' + (auction && auction.reservePrice ? auction.reservePrice : "") + '" step="100" min="0"/></div></div></div></div>';
        card.innerHTML = html;
        if (isNew) { const removeBtn = card.querySelector(".btn-remove-vehicle"); removeBtn.onclick = function() { const nIdx = newVehicles.findIndex(function(v) { return v.tempId === aid; }); if (nIdx >= 0) newVehicles.splice(nIdx, 1); delete newPhotosByAuction[aid]; card.remove(); renumberVehicles(); }; }
        return card;
    }
    function renumberVehicles() { var cards = document.querySelectorAll(".vehicle-edit-card"); for (var i = 0; i < cards.length; i++) { var num = cards[i].querySelector(".vehicle-number"); if (num) num.textContent = i + 1; } }
    async function initDropdowns(card, years) {
        const yS = card.querySelector("select[name='year']"), mS = card.querySelector("select[name='makeId']"), mdS = card.querySelector("select[name='modelId']");
        const cY = yS.dataset.current, cM = mS.dataset.current, cMd = mdS.dataset.current;
        yS.innerHTML = "<option value=''>Year</option>";
        for (var i = 0; i < years.length; i++) { var y = years[i]; var o = document.createElement("option"); o.value = y.year; o.textContent = y.year; if (y.year == cY) o.selected = true; yS.appendChild(o); }
        async function loadMakes() { if (!yS.value) { mS.innerHTML = "<option value=''>Make</option>"; mdS.innerHTML = "<option value=''>Model</option>"; return; } var makes = await fetch("/api/vehicles/years/" + yS.value + "/makes").then(function(r) { return r.json(); }); mS.innerHTML = "<option value=''>Make</option>"; for (var i = 0; i < makes.length; i++) { var m = makes[i]; var o = document.createElement("option"); o.value = m.makeId; o.textContent = m.name; if (m.makeId == cM) o.selected = true; mS.appendChild(o); } await loadModels(); }
        async function loadModels() { if (!yS.value || !mS.value) { mdS.innerHTML = "<option value=''>Model</option>"; return; } var models = await fetch("/api/vehicles/years/" + yS.value + "/makes/" + mS.value + "/models").then(function(r) { return r.json(); }); mdS.innerHTML = "<option value=''>Model</option>"; for (var i = 0; i < models.length; i++) { var m = models[i]; var o = document.createElement("option"); o.value = m.modelId; o.textContent = m.name; if (m.modelId == cMd) o.selected = true; mdS.appendChild(o); } }
        yS.onchange = loadMakes; mS.onchange = loadModels; await loadMakes();
    }
    function initPhotos(card, aid) {
        var grid = card.querySelector("#photos-" + aid), zone = card.querySelector("#upload-" + aid); if (!grid || !zone) return;
        var inp = zone.querySelector("input");
        newPhotosByAuction[aid] = []; deletedPhotosByAuction[aid] = [];
        grid.onclick = function(e) { if (e.target.classList.contains("delete-photo-btn")) { var it = e.target.closest(".photo-item"); if (it.dataset.url && !it.classList.contains("new-photo")) deletedPhotosByAuction[aid].push(it.dataset.url); it.remove(); } };
        inp.onchange = function() { addPhotos(aid, inp.files, grid); inp.value = ""; };
        zone.ondragover = function(e) { e.preventDefault(); zone.classList.add("dragover"); };
        zone.ondragleave = function() { zone.classList.remove("dragover"); };
        zone.ondrop = function(e) { e.preventDefault(); zone.classList.remove("dragover"); addPhotos(aid, e.dataTransfer.files, grid); };
    }
    function addPhotos(aid, files, grid) {
        var allowed = [".jpg", ".jpeg", ".png", ".webp"], max = 5 * 1024 * 1024;
        for (var i = 0; i < files.length; i++) { var f = files[i]; var ext = "." + f.name.split(".").pop().toLowerCase(); if (allowed.indexOf(ext) < 0) { alert("Invalid: " + ext); continue; } if (f.size > max) { alert("Too large"); continue; } var idx = newPhotosByAuction[aid].length; newPhotosByAuction[aid].push(f); var it = document.createElement("div"); it.className = "photo-item new-photo"; it.innerHTML = '<img src="' + URL.createObjectURL(f) + '"/><button type="button" class="delete-photo-btn">&times;</button>'; (function(x, el) { el.querySelector("button").onclick = function() { newPhotosByAuction[aid][x] = null; el.remove(); }; })(idx, it); grid.appendChild(it); }
    }
    function updatePreview() {
        var sI = document.getElementById("startTime"), eI = document.getElementById("endTime"), pv = document.getElementById("schedulePreview"), pt = document.getElementById("scheduleText");
        if (!sI.value || !eI.value) { pv.classList.remove("active"); pt.textContent = "Set times"; return; }
        var s = new Date(sI.value), e = new Date(eI.value); if (e <= s) { pv.classList.remove("active"); pt.textContent = "End must be after start"; return; }
        var d = e - s, days = Math.floor(d / 86400000), hrs = Math.floor((d % 86400000) / 3600000), mins = Math.floor((d % 3600000) / 60000);
        var txt = ""; if (days > 0) txt += days + "d "; if (hrs > 0) txt += hrs + "h "; if (mins > 0 && days === 0) txt += mins + "m";
        pv.classList.add("active"); pt.innerHTML = "<strong>" + txt.trim() + "</strong> - Ends: " + e.toLocaleString();
    }
    async function addNewVehicle() {
        var tempId = "new-" + Date.now(); newVehicles.push({ tempId: tempId });
        var idx = document.querySelectorAll(".vehicle-edit-card").length;
        var card = createVehicleCard(idx, null, null, true); card.dataset.auctionId = tempId;
        vehiclesContainer.appendChild(card); await initDropdowns(card, yearsCache); initPhotos(card, tempId);
        card.scrollIntoView({ behavior: "smooth", block: "start" });
    }
    async function saveAll() {
        saveStatus.textContent = "Saving..."; saveStatus.className = ""; saveAllBtn.disabled = true;
        try {
            for (var ni = 0; ni < newVehicles.length; ni++) {
                var nv = newVehicles[ni]; var card = document.querySelector('[data-auction-id="' + nv.tempId + '"]'); if (!card) continue;
                saveStatus.textContent = "Creating new vehicle...";
                var payload = { auctionGroupTitle: null, vehicles: [{ title: card.querySelector("input[name='title']").value.trim(), year: parseInt(card.querySelector("select[name='year']").value, 10), makeId: parseInt(card.querySelector("select[name='makeId']").value, 10), modelId: parseInt(card.querySelector("select[name='modelId']").value, 10), vin: card.querySelector("input[name='vin']").value || null, mileage: card.querySelector("input[name='mileage']").value ? parseInt(card.querySelector("input[name='mileage']").value, 10) : null, conditionGrade: parseInt(card.querySelector("select[name='conditionGrade']").value, 10), description: card.querySelector("textarea[name='description']").value.trim(), startPrice: card.querySelector("input[name='startPrice']").value ? parseFloat(card.querySelector("input[name='startPrice']").value) : 0, reservePrice: card.querySelector("input[name='reservePrice']").value ? parseFloat(card.querySelector("input[name='reservePrice']").value) : null }], startTime: document.getElementById("startTime").value ? new Date(document.getElementById("startTime").value).toISOString() : null, endTime: document.getElementById("endTime").value ? new Date(document.getElementById("endTime").value).toISOString() : null };
                var res = await fetch("/api/listings", { method: "POST", headers: Object.assign({ "Content-Type": "application/json" }, getAuthHeaders()), credentials: "include", body: JSON.stringify(payload) });
                if (!res.ok) throw new Error("Failed to create vehicle");
                var created = await res.json(); var newListingId = created[0] ? created[0].listingId : null;
                var nP = (newPhotosByAuction[nv.tempId] || []).filter(function(x) { return x; });
                if (nP.length > 0 && newListingId) { var fd = new FormData(); nP.forEach(function(x) { fd.append("photos", x); }); await fetch("/api/listings/" + newListingId + "/photos", { method: "POST", headers: getAuthHeaders(), credentials: "include", body: fd }); }
            }
            var cards = document.querySelectorAll('.vehicle-edit-card:not(.new-vehicle)'); var n = 0;
            for (var ci = 0; ci < cards.length; ci++) {
                var c = cards[ci]; var aid = c.dataset.auctionId, lid = c.dataset.listingId; if (!lid) continue;
                saveStatus.textContent = "Saving vehicle " + (n + 1) + "/" + cards.length + "...";
                var lP = { title: c.querySelector("input[name='title']").value.trim(), description: c.querySelector("textarea[name='description']").value.trim(), year: parseInt(c.querySelector("select[name='year']").value, 10), makeId: parseInt(c.querySelector("select[name='makeId']").value, 10), modelId: parseInt(c.querySelector("select[name='modelId']").value, 10), vin: c.querySelector("input[name='vin']").value || null, mileage: c.querySelector("input[name='mileage']").value ? parseInt(c.querySelector("input[name='mileage']").value, 10) : null, conditionGrade: parseInt(c.querySelector("select[name='conditionGrade']").value, 10) };
                var aP = { startPrice: c.querySelector("input[name='startPrice']").value ? parseFloat(c.querySelector("input[name='startPrice']").value) : null, reservePrice: c.querySelector("input[name='reservePrice']").value ? parseFloat(c.querySelector("input[name='reservePrice']").value) : null, endTime: document.getElementById("endTime").value ? new Date(document.getElementById("endTime").value).toISOString() : null };
                await fetch("/api/listings/" + lid, { method: "PATCH", headers: Object.assign({ "Content-Type": "application/json" }, getAuthHeaders()), credentials: "include", body: JSON.stringify(lP) });
                await fetch("/api/auctions/" + aid, { method: "PATCH", headers: Object.assign({ "Content-Type": "application/json" }, getAuthHeaders()), credentials: "include", body: JSON.stringify(aP) });
                var delPhotos = deletedPhotosByAuction[aid] || []; for (var di = 0; di < delPhotos.length; di++) { var fn = delPhotos[di].split("/").pop(); await fetch("/api/listings/" + lid + "/photos/" + fn, { method: "DELETE", headers: getAuthHeaders(), credentials: "include" }); }
                var newP = (newPhotosByAuction[aid] || []).filter(function(x) { return x; }); if (newP.length > 0) { var fdNew = new FormData(); newP.forEach(function(x) { fdNew.append("photos", x); }); await fetch("/api/listings/" + lid + "/photos", { method: "POST", headers: getAuthHeaders(), credentials: "include", body: fdNew }); }
                n++;
            }
            saveStatus.textContent = "Saved! Redirecting..."; saveStatus.className = "success"; setTimeout(function() { window.location.href = "/Seller/Dashboard"; }, 1000);
        } catch (e) { saveStatus.textContent = "Error: " + e.message; saveStatus.className = "error"; saveAllBtn.disabled = false; }
    }
    try {
        yearsCache = await loadYears(); var ids = isGroup ? auctionIds : (singleAuctionId ? [singleAuctionId] : []); if (ids.length === 0) { showError("No auction ID"); return; }
        for (var i = 0; i < ids.length; i++) auctionsData.push(await loadAuctionData(ids[i]));
        var first = auctionsData[0].auction, firstListing = auctionsData[0].listing;
        document.getElementById("groupTitle").value = decodeHtml(config.groupTitle) || decodeHtml(firstListing.title) || "";
        document.getElementById("startTime").value = formatLocalDateTime(first.startTime);
        document.getElementById("endTime").value = formatLocalDateTime(first.endTime);
        for (var i = 0; i < auctionsData.length; i++) { var d = auctionsData[i]; var card = createVehicleCard(i, d.auction, d.listing, false); vehiclesContainer.appendChild(card); await initDropdowns(card, yearsCache); initPhotos(card, d.auction.auctionId); }
        document.getElementById("startTime").onchange = updatePreview; document.getElementById("endTime").onchange = updatePreview; updatePreview();
        addVehicleBtn.onclick = addNewVehicle; saveAllBtn.onclick = saveAll; showContent();
    } catch (e) { showError(e.message || "Failed to load"); }
})();

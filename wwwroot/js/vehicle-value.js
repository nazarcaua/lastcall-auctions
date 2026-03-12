/**
 * Vehicle Value Estimator - Frontend JavaScript
 * Handles cascading dropdowns, issue management, and API calls for vehicle valuation.
 */

document.addEventListener('DOMContentLoaded', function () {
    // DOM Elements
    const yearSelect = document.getElementById('yearSelect');
    const makeSelect = document.getElementById('makeSelect');
    const modelSelect = document.getElementById('modelSelect');
    const mileageInput = document.getElementById('mileageInput');
    const provinceSelect = document.getElementById('provinceSelect');

    const issueCategorySelect = document.getElementById('issueCategorySelect');
    const issueSeveritySelect = document.getElementById('issueSeveritySelect');
    const issueDescriptionInput = document.getElementById('issueDescriptionInput');
    const addIssueBtn = document.getElementById('addIssueBtn');
    const issuesList = document.getElementById('issuesList');

    const estimateBtn = document.getElementById('estimateBtn');
    const resultsSection = document.getElementById('resultsSection');
    const loadingOverlay = document.getElementById('loadingOverlay');
    const newEstimateBtn = document.getElementById('newEstimateBtn');
    const printResultsBtn = document.getElementById('printResultsBtn');

    // State
    let issues = [];

    // Province full names for display
    const provinceNames = {
        'AB': 'Alberta',
        'BC': 'British Columbia',
        'MB': 'Manitoba',
        'NB': 'New Brunswick',
        'NL': 'Newfoundland and Labrador',
        'NS': 'Nova Scotia',
        'NT': 'Northwest Territories',
        'NU': 'Nunavut',
        'ON': 'Ontario',
        'PE': 'Prince Edward Island',
        'QC': 'Quebec',
        'SK': 'Saskatchewan',
        'YT': 'Yukon'
    };

    // Initialize
    init();

    function init() {
        loadYears();
        setupEventListeners();
        renderIssues();
    }

    function setupEventListeners() {
        yearSelect.addEventListener('change', onYearChange);
        makeSelect.addEventListener('change', onMakeChange);
        addIssueBtn.addEventListener('click', addIssue);
        estimateBtn.addEventListener('click', getEstimate);
        newEstimateBtn.addEventListener('click', resetForm);
        printResultsBtn.addEventListener('click', () => window.print());

        // Allow Enter key to add issue
        issueDescriptionInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                addIssue();
            }
        });
    }

    // ============================
    // Cascading Dropdowns
    // ============================

    async function loadYears() {
        try {
            const response = await fetch('/api/vehicles/years');
            if (!response.ok) throw new Error('Failed to load years');

            const years = await response.json();
            yearSelect.innerHTML = '<option value="">Select Year</option>';

            years.forEach(y => {
                const option = document.createElement('option');
                option.value = y.year;
                option.textContent = y.year;
                yearSelect.appendChild(option);
            });
        } catch (error) {
            console.error('Error loading years:', error);
            showError('Failed to load vehicle years. Please refresh the page.');
        }
    }

    async function onYearChange() {
        const year = yearSelect.value;

        // Reset dependent dropdowns
        makeSelect.innerHTML = '<option value="">Select Make</option>';
        makeSelect.disabled = true;
        modelSelect.innerHTML = '<option value="">Select Model</option>';
        modelSelect.disabled = true;

        if (!year) return;

        try {
            const response = await fetch(`/api/vehicles/makes?year=${year}`);
            if (!response.ok) throw new Error('Failed to load makes');

            const makes = await response.json();
            makes.forEach(m => {
                const option = document.createElement('option');
                option.value = m.makeId;
                option.textContent = m.name;
                makeSelect.appendChild(option);
            });

            makeSelect.disabled = false;
        } catch (error) {
            console.error('Error loading makes:', error);
            showError('Failed to load vehicle makes.');
        }
    }

    async function onMakeChange() {
        const year = yearSelect.value;
        const makeId = makeSelect.value;

        // Reset model dropdown
        modelSelect.innerHTML = '<option value="">Select Model</option>';
        modelSelect.disabled = true;

        if (!makeId) return;

        try {
            const response = await fetch(`/api/vehicles/models?year=${year}&makeId=${makeId}`);
            if (!response.ok) throw new Error('Failed to load models');

            const models = await response.json();
            models.forEach(m => {
                const option = document.createElement('option');
                option.value = m.modelId;
                option.textContent = m.name;
                modelSelect.appendChild(option);
            });

            modelSelect.disabled = false;
        } catch (error) {
            console.error('Error loading models:', error);
            showError('Failed to load vehicle models.');
        }
    }

    // ============================
    // Issue Management
    // ============================

    function addIssue() {
        const category = issueCategorySelect.value;
        const severity = issueSeveritySelect.value;
        const description = issueDescriptionInput.value.trim();

        if (!category) {
            showError('Please select an issue category.');
            return;
        }

        if (!description) {
            showError('Please describe the issue.');
            return;
        }

        issues.push({
            id: Date.now(),
            category,
            severity,
            description
        });

        // Reset form
        issueCategorySelect.value = '';
        issueSeveritySelect.value = 'Minor';
        issueDescriptionInput.value = '';

        renderIssues();
    }

    function removeIssue(id) {
        issues = issues.filter(issue => issue.id !== id);
        renderIssues();
    }

    function renderIssues() {
        if (issues.length === 0) {
            issuesList.innerHTML = '<p class="no-issues-message">No issues added yet. Add issues below to get repair cost estimates.</p>';
            return;
        }

        issuesList.innerHTML = issues.map(issue => `
            <div class="issue-item" data-id="${issue.id}">
                <div class="issue-info">
                    <div class="issue-header">
                        <span class="issue-category">${issue.category}</span>
                        <span class="issue-severity ${issue.severity.toLowerCase()}">${issue.severity}</span>
                    </div>
                    <p class="issue-description">${escapeHtml(issue.description)}</p>
                </div>
                <button type="button" class="remove-issue-btn" onclick="window.vehicleValue.removeIssue(${issue.id})" title="Remove issue">&times;</button>
            </div>
        `).join('');
    }

    // ============================
    // Get Estimate
    // ============================

    async function getEstimate() {
        // Validation
        if (!yearSelect.value) {
            showError('Please select a year.');
            return;
        }
        if (!makeSelect.value) {
            showError('Please select a make.');
            return;
        }
        if (!modelSelect.value) {
            showError('Please select a model.');
            return;
        }
        if (!provinceSelect.value) {
            showError('Please select your province.');
            return;
        }

        const requestData = {
            year: parseInt(yearSelect.value),
            makeId: parseInt(makeSelect.value),
            modelId: parseInt(modelSelect.value),
            mileage: mileageInput.value ? parseInt(mileageInput.value) : null,
            province: provinceSelect.value,
            issues: issues.map(i => ({
                category: i.category,
                description: i.description,
                severity: i.severity
            }))
        };

        showLoading(true);

        try {
            const response = await fetch('/api/vehicles/value-estimate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            if (!response.ok) {
                throw new Error('Failed to get estimate');
            }

            const result = await response.json();
            displayResults(result);
        } catch (error) {
            console.error('Error getting estimate:', error);
            showError('Failed to get vehicle value estimate. Please try again.');
        } finally {
            showLoading(false);
        }
    }

    function displayResults(data) {
        // Vehicle title
        document.getElementById('vehicleTitle').textContent = data.vehicleName || 
            `${data.year} ${data.make} ${data.model}`;

        // Value summary
        document.getElementById('estimatedValue').textContent = formatCurrency(data.estimatedValue);
        document.getElementById('valueRangeLow').textContent = formatCurrency(data.valueRangeLow);
        document.getElementById('valueRangeHigh').textContent = formatCurrency(data.valueRangeHigh);
        document.getElementById('baseValue').textContent = formatCurrency(data.baseValue);
        document.getElementById('mileageAdjustment').textContent = formatCurrency(data.mileageAdjustment);
        document.getElementById('issuesDeduction').textContent = '-' + formatCurrency(data.issuesDeduction);

        // Repair costs
        document.getElementById('totalRepairCost').textContent = formatCurrency(data.totalRepairCost);
        document.getElementById('regionName').textContent = provinceNames[provinceSelect.value] || provinceSelect.value;

        // Value after repairs
        document.getElementById('valueAfterRepairs').textContent = formatCurrency(data.valueAfterRepairs);
        document.getElementById('recommendation').textContent = data.recommendation || '';

        // Repair table
        const repairTableBody = document.getElementById('repairTableBody');
        const repairDetailsSection = document.getElementById('repairDetailsSection');

        if (data.repairEstimates && data.repairEstimates.length > 0) {
            repairDetailsSection.style.display = 'block';
            repairTableBody.innerHTML = data.repairEstimates.map(repair => `
                <tr>
                    <td>
                        <strong>${repair.category}</strong><br>
                        <span class="notes-cell">${escapeHtml(repair.issue)}</span>
                    </td>
                    <td><span class="issue-severity ${repair.severity.toLowerCase()}">${repair.severity}</span></td>
                    <td>${formatCurrency(repair.partsCost)}</td>
                    <td>${formatCurrency(repair.laborCost)}</td>
                    <td><strong>${formatCurrency(repair.totalCost)}</strong></td>
                    <td>${repair.repairTimeEstimate}</td>
                </tr>
            `).join('');
        } else {
            repairDetailsSection.style.display = 'none';
        }

        // Show results
        resultsSection.classList.remove('hidden');
        resultsSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function resetForm() {
        // Reset dropdowns
        yearSelect.value = '';
        makeSelect.innerHTML = '<option value="">Select Make</option>';
        makeSelect.disabled = true;
        modelSelect.innerHTML = '<option value="">Select Model</option>';
        modelSelect.disabled = true;
        mileageInput.value = '';
        provinceSelect.value = '';

        // Reset issues
        issues = [];
        renderIssues();

        // Reset issue form
        issueCategorySelect.value = '';
        issueSeveritySelect.value = 'Minor';
        issueDescriptionInput.value = '';

        // Hide results
        resultsSection.classList.add('hidden');

        // Scroll to top
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // ============================
    // Utility Functions
    // ============================

    function formatCurrency(value) {
        if (value === null || value === undefined) return '$0';
        return new Intl.NumberFormat('en-CA', {
            style: 'currency',
            currency: 'CAD',
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(value);
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showLoading(show) {
        if (show) {
            loadingOverlay.classList.remove('hidden');
        } else {
            loadingOverlay.classList.add('hidden');
        }
    }

    function showError(message) {
        // Simple alert for now - could be replaced with a toast notification
        alert(message);
    }

    // Expose removeIssue to global scope for onclick handlers
    window.vehicleValue = {
        removeIssue
    };
});

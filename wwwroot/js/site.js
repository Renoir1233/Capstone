// UniHealth Clinic Portal – Full backend-connected application
class UniHealthApp {
    constructor() {
        // Chart References
        this.charts = {};

        // Active State variables
        this.currentTab = 'dashboard-tab';
        this.emrFilterDept = 'all';
        this.emrSearchText = '';

        this.init();
    }

    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.bindEvents();
        });
    }

    // ==========================================
    // AUTHENTICATION
    // ==========================================
    async login() {
        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value.trim();
        const errEl    = document.getElementById('login-error');

        if (!username || !password) {
            errEl.textContent = 'Please enter your username and password.';
            errEl.style.display = 'block';
            return;
        }

        const btn = document.querySelector('.btn-signin');
        btn.textContent = 'Signing in…';
        btn.disabled    = true;
        errEl.style.display = 'none';

        try {
            const res = await fetch('/api/auth/login', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({ username, password })
            });

            const data = await res.json();

            if (res.ok && data.success) {
                // Success - redirect to home, let server decide which dashboard to show
                window.location.href = '/';
            } else {
                errEl.textContent = data.message || 'Invalid username or password.';
                errEl.style.display = 'block';
            }
        } catch (err) {
            errEl.textContent = 'Connection error. Please try again.';
            errEl.style.display = 'block';
        } finally {
            btn.textContent = 'Sign In';
            btn.disabled    = false;
        }
    }

    async logout() {
        try {
            await fetch('/api/auth/logout', { method: 'POST' });
        } catch (_) {}
        document.getElementById('portal-view').style.display = 'none';
        document.getElementById('login-view').style.display  = 'flex';
        document.getElementById('password').value = '';
        this.destroyCharts();
    }

    bindEvents() {
        // Check if user is already authenticated on page load
        const portalView = document.getElementById('portal-view');
        if (portalView && portalView.style.display !== 'none') {
            // User is authenticated, load dashboard data
            this.loadDashboard();
            this.renderQueue();
            this.renderEmr();
            this.renderPharmacy();
        }

        // Admin user menu toggle
        this.setupAdminUserMenu();

        // Mobile menu toggle
        this.setupMobileMenu();

        // Tab switching handler
        document.querySelectorAll('.nav-link-custom').forEach(link => {
            link.addEventListener('click', () => {
                const targetTab = link.getAttribute('data-tab');
                this.switchTab(targetTab);
            });
        });
    }

    setupMobileMenu() {
        // Create mobile menu elements if they don't exist
        if (!document.querySelector('.mobile-menu-toggle')) {
            const menuToggle = document.createElement('button');
            menuToggle.className = 'mobile-menu-toggle';
            menuToggle.innerHTML = '<i class="fa-solid fa-bars"></i>';
            document.body.appendChild(menuToggle);

            const menuOverlay = document.createElement('div');
            menuOverlay.className = 'mobile-menu-overlay';
            document.body.appendChild(menuOverlay);

            // Toggle menu
            menuToggle.addEventListener('click', () => {
                const sidebar = document.querySelector('.portal-sidebar');
                sidebar.classList.toggle('mobile-open');
                menuOverlay.classList.toggle('active');
            });

            // Close menu when clicking overlay
            menuOverlay.addEventListener('click', () => {
                const sidebar = document.querySelector('.portal-sidebar');
                sidebar.classList.remove('mobile-open');
                menuOverlay.classList.remove('active');
            });

            // Close menu when clicking nav link on mobile
            document.querySelectorAll('.nav-link-custom').forEach(link => {
                link.addEventListener('click', () => {
                    if (window.innerWidth <= 768) {
                        const sidebar = document.querySelector('.portal-sidebar');
                        sidebar.classList.remove('mobile-open');
                        menuOverlay.classList.remove('active');
                    }
                });
            });
        }
    }

    setupAdminUserMenu() {
        const userMenuToggle = document.getElementById('admin-user-menu-toggle');
        const userProfileTrigger = document.getElementById('admin-user-profile-trigger');
        const userDropdown = document.getElementById('admin-user-dropdown-menu');
        
        if (userMenuToggle && userDropdown) {
            userMenuToggle.addEventListener('click', (e) => {
                e.stopPropagation();
                const isVisible = userDropdown.style.display === 'block';
                userDropdown.style.display = isVisible ? 'none' : 'block';
                const icon = userMenuToggle.querySelector('i');
                icon.className = isVisible ? 'fa-solid fa-chevron-up' : 'fa-solid fa-chevron-down';
            });

            if (userProfileTrigger) {
                userProfileTrigger.addEventListener('click', (e) => {
                    e.stopPropagation();
                    const isVisible = userDropdown.style.display === 'block';
                    userDropdown.style.display = isVisible ? 'none' : 'block';
                    const icon = userMenuToggle.querySelector('i');
                    icon.className = isVisible ? 'fa-solid fa-chevron-up' : 'fa-solid fa-chevron-down';
                });
            }

            // Close dropdown when clicking outside
            document.addEventListener('click', () => {
                userDropdown.style.display = 'none';
                const icon = userMenuToggle.querySelector('i');
                if (icon) icon.className = 'fa-solid fa-chevron-up';
            });

            // Prevent dropdown from closing when clicking inside it
            userDropdown.addEventListener('click', (e) => {
                // Menu items will handle their own actions
                userDropdown.style.display = 'none';
                const icon = userMenuToggle.querySelector('i');
                if (icon) icon.className = 'fa-solid fa-chevron-up';
            });
        }
    }

    switchTab(tabId) {
        document.querySelectorAll('.nav-link-custom').forEach(link => {
            link.classList.toggle('active', link.getAttribute('data-tab') === tabId);
        });

        document.querySelectorAll('.tab-section').forEach(section => {
            section.classList.remove('active');
        });

        const targetSection = document.getElementById(tabId);
        if (targetSection) targetSection.classList.add('active');

        this.currentTab = tabId;

        const titles = {
            'dashboard-tab': 'Dashboard',
            'queue-tab':     'Queue Management',
            'emr-tab':       'EMR Records',
            'pharmacy-tab':  'Pharmacy Stock',
            'wellness-tab':  'Wellness Hub',
            'trends-tab':    'Health Trends',
            'reports-tab':   'HR Reports'
        };
        document.getElementById('page-title').innerText = titles[tabId] || 'Dashboard';

        // Reload data when switching tabs
        if (tabId === 'dashboard-tab') {
            this.loadDashboard();
        } else if (tabId === 'queue-tab') {
            this.renderQueue();
        } else if (tabId === 'emr-tab') {
            this.renderEmr();
        } else if (tabId === 'pharmacy-tab') {
            this.renderPharmacy();
        } else if (tabId === 'wellness-tab') {
            this.renderWellness();
        } else if (tabId === 'trends-tab') {
            this.renderHealthTrends();
        }
    }

    // ==========================================
    // DASHBOARD – real API data
    // ==========================================
    async loadDashboard() {
        try {
            const [statsRes, monthlyRes, illnessRes, deptRes] = await Promise.all([
                fetch('/api/dashboard/stats'),
                fetch('/api/dashboard/monthly-visits'),
                fetch('/api/dashboard/top-illnesses'),
                fetch('/api/dashboard/dept-visits')
            ]);

            const stats   = await statsRes.json();
            const monthly = await monthlyRes.json();
            const illness = await illnessRes.json();
            const dept    = await deptRes.json();

            // Update stat cards
            document.getElementById('stat-today-visits').innerText = stats.todayVisits;
            document.getElementById('stat-low-stock').innerText    = stats.lowStockCount;
            document.getElementById('stat-sick-leaves').innerText  = stats.sickLeavesThisMonth;
            document.getElementById('stat-health-score').innerText = `${stats.healthScore}%`;

            // Sick leave trend text
            const slTrend = document.querySelector('#stat-sick-leaves')?.closest('.stats-card')?.querySelector('.stats-trend');
            if (slTrend) {
                const diff = stats.sickLeavesThisMonth - stats.sickLeavesLastMonth;
                if (diff > 0) {
                    slTrend.innerHTML = `<i class="fa-solid fa-arrow-up"></i> ${diff} vs last month`;
                    slTrend.className = 'stats-trend down';
                } else if (diff < 0) {
                    slTrend.innerHTML = `<i class="fa-solid fa-arrow-down"></i> ${Math.abs(diff)} vs last month`;
                    slTrend.className = 'stats-trend up';
                } else {
                    slTrend.innerHTML = 'Same as last month';
                    slTrend.className = 'stats-trend neutral';
                }
            }

            // Initialize charts with live data
            setTimeout(() => this.initializeCharts(monthly, illness, dept), 150);

        } catch (err) {
            console.error('Dashboard load error:', err);
        }
    }

    // ==========================================
    // CHARTS
    // ==========================================
    initializeCharts(monthly, illness, dept) {
        this.destroyCharts();

        // 1. Monthly Clinic Visits (live data)
        const monthlyCtx = document.getElementById('monthlyVisitsChart');
        if (monthlyCtx) {
            this.charts.monthly = new Chart(monthlyCtx.getContext('2d'), {
                type: 'line',
                data: {
                    labels: monthly?.labels || [],
                    datasets: [{
                        label: 'Visits',
                        data: monthly?.data || [],
                        borderColor: '#0a7a57',
                        borderWidth: 3,
                        pointBackgroundColor: '#0a7a57',
                        pointBorderColor: '#ffffff',
                        pointBorderWidth: 2,
                        pointRadius: 6,
                        pointHoverRadius: 8,
                        tension: 0.4,
                        fill: true,
                        backgroundColor: 'rgba(10, 122, 87, 0.05)'
                    }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: { legend: { display: false }, tooltip: { padding: 12, cornerRadius: 8 } },
                    scales: {
                        y: { ticks: { stepSize: 5, color: '#64748b', font: { family: 'Outfit' } }, grid: { color: '#f1f5f9' } },
                        x: { ticks: { color: '#64748b', font: { family: 'Outfit' } }, grid: { display: false } }
                    }
                }
            });
        }

        // 2. Top Illnesses Doughnut (live data)
        const illnessesCtx = document.getElementById('topIllnessesChart');
        if (illnessesCtx) {
            this.charts.illnesses = new Chart(illnessesCtx.getContext('2d'), {
                type: 'doughnut',
                data: {
                    labels: illness?.labels || [],
                    datasets: [{
                        data: illness?.data || [],
                        backgroundColor: ['#10b981', '#3b82f6', '#8b5cf6', '#f59e0b', '#ef4444', '#475569'],
                        borderWidth: 4, borderColor: '#ffffff', hoverOffset: 4
                    }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false, cutout: '70%',
                    plugins: { legend: { display: false }, tooltip: { padding: 12, cornerRadius: 8 } }
                }
            });
        }

        // 3. Department Visits Bar (live data)
        const deptCtx = document.getElementById('deptVisitsChart');
        if (deptCtx) {
            this.charts.dept = new Chart(deptCtx.getContext('2d'), {
                type: 'bar', indexAxis: 'y',
                data: {
                    labels: dept?.labels || [],
                    datasets: [{ data: dept?.data || [], backgroundColor: '#60a5fa', borderRadius: 6, barThickness: 16 }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: { legend: { display: false }, tooltip: { padding: 12, cornerRadius: 8 } },
                    scales: {
                        x: { ticks: { color: '#64748b', font: { family: 'Outfit' } }, grid: { color: '#f1f5f9' } },
                        y: { ticks: { color: '#0f172a', font: { family: 'Outfit', weight: '600', size: 13 } }, grid: { display: false } }
                    }
                }
            });
        }
    }

    async loadDispensingChart() {
        try {
            const res  = await fetch('/api/pharmacy/dispensing-trends');
            const data = await res.json();

            // Wait for canvas to be visible and have real dimensions
            await new Promise(resolve => setTimeout(resolve, 200));

            const dispenseCtx = document.getElementById('dispensingTrendsChart');
            if (!dispenseCtx) return;

            // Destroy old chart instance if it exists
            if (this.charts.dispense) {
                this.charts.dispense.destroy();
                this.charts.dispense = null;
            }

            // Update chart card subtitle based on what data is shown
            const chartSubtitle = document.getElementById('dispensing-chart-subtitle');
            if (chartSubtitle) {
                chartSubtitle.textContent = data.noDispenseData
                    ? 'No dispense records yet — showing current stock levels'
                    : 'Units dispensed per week (top medicines)';
            }

            const isBar  = data.chartType === 'bar';
            const labels = data.labels   || [];
            const datasets = (data.datasets || []).map(ds => ({
                ...ds,
                // ensure bar datasets have borderRadius
                ...(isBar ? { borderRadius: 6, borderWidth: 0 } : {})
            }));

            const commonOptions = {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: 600, easing: 'easeInOutQuart' },
                plugins: {
                    legend: {
                        position: 'top',
                        labels: { boxWidth: 12, font: { family: 'Outfit', size: 12 }, padding: 16 }
                    },
                    tooltip: {
                        padding: 12,
                        cornerRadius: 8,
                        bodyFont: { family: 'Outfit' },
                        titleFont: { family: 'Outfit', weight: '700' }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { color: '#64748b', font: { family: 'Outfit' }, precision: 0 },
                        grid: { color: '#f1f5f9' }
                    },
                    x: {
                        ticks: { color: '#64748b', font: { family: 'Outfit', size: 11 } },
                        grid: { display: false }
                    }
                }
            };

            this.charts.dispense = new Chart(dispenseCtx.getContext('2d'), {
                type: isBar ? 'bar' : 'line',
                data: { labels, datasets },
                options: commonOptions
            });

        } catch (err) {
            console.error('Dispensing chart error:', err);
        }
    }

    destroyCharts() {
        Object.keys(this.charts).forEach(key => {
            if (this.charts[key]) this.charts[key].destroy();
        });
        this.charts = {};
    }

    // ==========================================
    // QUEUE MANAGEMENT – real API
    // ==========================================
    async renderQueue() {
        try {
            const res  = await fetch('/api/queue');
            const data = await res.json();

            const waiting    = data.waiting    || [];
            const withDoctor = data.withDoctor || [];
            const cleared    = data.cleared    || [];

            document.getElementById('waiting-count').innerText = waiting.length;
            document.getElementById('doctor-count').innerText  = withDoctor.length;
            document.getElementById('cleared-count').innerText = cleared.length;

            // Update dashboard today's visits count
            const statEl = document.getElementById('stat-today-visits');
            if (statEl) statEl.innerText = waiting.length + withDoctor.length + cleared.length;

            // 1. Waiting Room
            const waitingListEl = document.getElementById('waiting-list');
            waitingListEl.innerHTML = '';
            if (waiting.length === 0) {
                waitingListEl.innerHTML = '<div style="text-align:center; color:#94a3b8; font-size:13px; padding:20px;">No patients waiting.</div>';
            } else {
                waiting.forEach((item, idx) => {
                    const btnClass = idx === 0 ? 'call' : 'call-outline';
                    waitingListEl.innerHTML += `
                        <div class="queue-card">
                            <div class="queue-card-bar"></div>
                            <div class="queue-card-time-symptom">${item.time} • ${item.chiefComplain}</div>
                            <div class="queue-card-patient">${item.patientName}</div>
                            <div class="queue-card-dept">${item.department}</div>
                            <button class="btn-queue-action ${btnClass}" onclick="app.callIn(${item.id})">Call In</button>
                        </div>`;
                });
            }

            // 2. With Doctor
            const doctorListEl = document.getElementById('doctor-list');
            doctorListEl.innerHTML = '';
            if (withDoctor.length === 0) {
                doctorListEl.innerHTML = '<div style="text-align:center; color:#94a3b8; font-size:13px; padding:20px;">No patients with doctor.</div>';
            } else {
                withDoctor.forEach(item => {
                    doctorListEl.innerHTML += `
                        <div class="queue-card">
                            <div class="queue-card-bar"></div>
                            <div class="queue-card-time-symptom">${item.time} • ${item.chiefComplain}</div>
                            <div class="queue-card-patient">${item.patientName}</div>
                            <div class="queue-card-dept">${item.department}</div>
                            <button class="btn-queue-action log" onclick="app.logVisit(${item.id})">Log Visit</button>
                        </div>`;
                });
            }

            // 3. Cleared Today
            const clearedListEl = document.getElementById('cleared-list');
            clearedListEl.innerHTML = '';
            if (cleared.length === 0) {
                clearedListEl.innerHTML = '<div style="text-align:center; color:#94a3b8; font-size:13px; padding:20px;">No records cleared.</div>';
            } else {
                cleared.forEach(item => {
                    const isSentHome = (item.outcome || '').toLowerCase().includes('sent home');
                    const icon  = isSentHome ? 'fa-solid fa-house-user' : 'fa-regular fa-circle-check';
                    const badge = 'cleared-badge';
                    clearedListEl.innerHTML += `
                        <div class="queue-card">
                            <div class="queue-card-bar"></div>
                            <div class="queue-card-time-symptom">Cleared • ${item.chiefComplain}</div>
                            <div class="queue-card-patient">${item.patientName}</div>
                            <div style="margin-top: 10px;">
                                <span class="${badge}">
                                    <i class="${icon}"></i>
                                    ${item.outcome || 'Fit to Work'}
                                </span>
                            </div>
                        </div>`;
                });
            }

        } catch (err) {
            console.error('Queue load error:', err);
        }
    }

    async callIn(visitId) {
        try {
            const res = await fetch(`/api/queue/${visitId}/callin`, { method: 'PATCH' });
            if (res.ok) {
                await this.renderQueue();
            } else {
                alert('Failed to call in patient.');
            }
        } catch (err) {
            console.error('Call in error:', err);
        }
    }

    logVisit(visitId) {
        this.openLogVisitModal(visitId);
    }

    openLogVisitModal(visitId) {
        this._pendingLogVisitId = visitId;

        // Find patient name from DOM
        const card = document.querySelector(`[onclick="app.logVisit(${visitId})"]`)?.closest('.queue-card');
        const patientName = card?.querySelector('.queue-card-patient')?.textContent || '';
        document.getElementById('log-visit-patient-name').textContent = patientName ? `Patient: ${patientName}` : '';

        // Reset to default selection
        const defaultRadio = document.getElementById('outcome-fit');
        if (defaultRadio) defaultRadio.checked = true;
        document.getElementById('log-visit-notes').value = '';

        // Initialize prescription section
        if (typeof QueueManager !== 'undefined') {
            QueueManager.prescriptions = []; // Clear previous prescriptions
            QueueManager.renderPrescriptionList();
            QueueManager.populateMedicineDropdown('medicine-select-logvisit');
        }

        document.getElementById('log-visit-modal').classList.add('active');
    }

    closeLogVisitModal() {
        document.getElementById('log-visit-modal').classList.remove('active');
        this._pendingLogVisitId = null;
    }

    async confirmLogVisit() {
        const visitId = this._pendingLogVisitId;
        if (!visitId) return;

        const selectedRadio = document.querySelector('input[name="visit-outcome"]:checked');
        const outcome = selectedRadio ? selectedRadio.value : 'Fit to Work';
        const notes   = document.getElementById('log-visit-notes').value.trim();
        const fullOutcome = notes ? `${outcome} — ${notes}` : outcome;

        const btn = document.querySelector('#log-visit-modal .btn-primary-custom');
        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Saving…';

        try {
            // Use QueueManager to submit with prescriptions if available
            if (typeof QueueManager !== 'undefined') {
                await QueueManager.submitLogVisit(visitId);
            } else {
                // Fallback to original without prescriptions
                const res = await fetch(`/api/queue/${visitId}/logvisit`, {
                    method:  'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body:    JSON.stringify({ outcome: fullOutcome })
                });
                if (!res.ok) throw new Error('Failed to log visit');
            }
            
            this.closeLogVisitModal();
            this.showToast(`Visit logged: ${outcome}`, 'success');
            await this.renderQueue();
            await this.renderEmr();
        } catch (err) {
            console.error('Log visit error:', err);
            alert(err.message || 'Failed to log visit.');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="fa-solid fa-clipboard-check"></i> Confirm & Clear Patient';
        }
    }

    // ==========================================
    // EMR RECORDS – real API
    // ==========================================
    async renderEmr() {
        const tableBody = document.getElementById('emr-table-body');
        tableBody.innerHTML = '<tr><td colspan="7" style="text-align:center; color:#94a3b8; padding:20px;">Loading records…</td></tr>';

        try {
            const params = new URLSearchParams();
            if (this.emrFilterDept !== 'all') params.set('dept', this.emrFilterDept);
            if (this.emrSearchText) params.set('search', this.emrSearchText);

            // Load employees AND today's queue so we can mark Completed
            const [empsRes, queueRes] = await Promise.all([
                fetch(`/api/emr?${params}`),
                fetch('/api/queue')
            ]);
            const emps  = await empsRes.json();
            const queue = await queueRes.json();

            // Build a set of patient names that are cleared today
            const clearedNames = new Set(
                (queue.cleared || []).map(v => v.patientName?.toLowerCase())
            );

            tableBody.innerHTML = '';
            if (emps.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="7" style="text-align:center; color:#94a3b8;">No matching records found.</td></tr>';
                return;
            }

            const colors = ['#0f766e','#1d4ed8','#6d28d9','#be185d','#ea580c','#15803d','#0369a1','#b45309','#4f46e5'];
            emps.forEach((emp, idx) => {
                const initials = emp.name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
                const color    = colors[idx % colors.length];

                // Determine status badge
                const isCompleted = clearedNames.has(emp.name?.toLowerCase());
                const statusBadge = isCompleted
                    ? `<span class="status-pill" style="background:#eff6ff;color:#3b82f6;border:1px solid #bfdbfe;">
                           <i class="fa-solid fa-clipboard-check"></i> Completed
                       </span>`
                    : `<span class="status-pill active"><i class="fa-regular fa-circle-check"></i> Completed</span>`;

                tableBody.innerHTML += `
                    <tr>
                        <td class="emp-id-text">${emp.employeeNumber}</td>
                        <td>
                            <div class="emp-name-wrapper">
                                <div class="emp-avatar" style="background-color: ${color}">${initials}</div>
                                <span class="emp-name-text">${emp.name}</span>
                            </div>
                        </td>
                        <td>${emp.department}</td>
                        <td>${emp.age}</td>
                        <td><i class="fa-solid fa-${emp.gender === 'Female' ? 'venus' : 'mars'}" style="color:#64748b; margin-right:4px;"></i> ${emp.gender}</td>
                        <td>${statusBadge}</td>
                        <td>
                            <div class="table-actions">
                                <button class="btn-table-action-view" onclick="app.openEmrDetails(${emp.id})"><i class="fa-regular fa-eye"></i> View</button>
                            </div>
                        </td>
                    </tr>`;
            });
        } catch (err) {
            tableBody.innerHTML = '<tr><td colspan="7" style="text-align:center; color:#ef4444;">Error loading records.</td></tr>';
            console.error('EMR load error:', err);
        }
    }

    searchEmployees() {
        this.emrSearchText = document.getElementById('emr-search').value.toLowerCase();
        this.renderEmr();
    }

    filterDepartment(buttonEl, dept) {
        document.querySelectorAll('.filter-pill').forEach(pill => pill.classList.remove('active'));
        buttonEl.classList.add('active');
        this.emrFilterDept = dept;
        this.renderEmr();
    }

    async openEmrDetails(empId) {
        try {
            const res = await fetch(`/api/emr/${empId}`);
            const emp = await res.json();

            const initials = emp.name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
            const colors   = ['#0f766e','#1d4ed8','#6d28d9','#be185d','#ea580c','#15803d','#0369a1','#b45309','#4f46e5'];
            const color    = colors[empId % colors.length];

            document.getElementById('detail-avatar').innerText = initials;
            document.getElementById('detail-avatar').style.backgroundColor = color;
            document.getElementById('detail-id').innerText   = emp.employeeNumber;
            document.getElementById('detail-name').innerText = emp.name;
            document.getElementById('detail-dept').innerText = emp.department;
            document.getElementById('detail-agb').innerText  = `${emp.age} / ${emp.gender}`;

            // Show visit history
            const visitHistory = emp.visits && emp.visits.length > 0
                ? emp.visits.map(v =>
                    `• ${new Date(v.dateOfConsultation).toLocaleDateString('en-PH')} — ${v.chiefComplain} → ${v.outcome || v.queueStatus}`
                  ).join('\n')
                : 'No visit records on file.';

            document.getElementById('detail-prescriptions').innerText = visitHistory;
            document.getElementById('emr-details-modal').classList.add('active');
        } catch (err) {
            console.error('EMR detail error:', err);
            alert('Failed to load employee record.');
        }
    }

    closeEmrDetailsModal() {
        document.getElementById('emr-details-modal').classList.remove('active');
    }

    // ==========================================
    // PHARMACY – real API
    // ==========================================
    async renderPharmacy() {
        const tableBody = document.getElementById('pharmacy-table-body');
        tableBody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:#94a3b8;padding:20px;">Loading inventory…</td></tr>';

        try {
            const [medsRes, statsRes] = await Promise.all([
                fetch('/api/pharmacy/medicines'),
                fetch('/api/pharmacy/stats')
            ]);

            const meds  = await medsRes.json();
            const stats = await statsRes.json();

            // Update stat cards
            document.getElementById('pharmacy-stat-total').innerText    = stats.total.toLocaleString();
            document.getElementById('pharmacy-stat-low').innerText      = stats.lowCount;
            document.getElementById('pharmacy-stat-expiring').innerText = stats.expiring;
            document.getElementById('stat-low-stock').innerText         = stats.lowCount;

            // Badge dot
            const badge = document.getElementById('pharmacy-badge');
            if (badge) badge.style.display = stats.lowCount > 0 ? 'block' : 'none';

            tableBody.innerHTML = '';
            meds.forEach(item => {
                const statusClass = item.stockStatus.toLowerCase();
                const isLow = item.stockStatus === 'Critical' || item.stockStatus === 'Low';
                const expiry = item.expiryDate
                    ? new Date(item.expiryDate).toLocaleDateString('en-PH')
                    : '—';

                tableBody.innerHTML += `
                    <tr>
                        <td style="font-weight: 700; color: #0f172a; padding: 16px 20px;">
                            ${item.name}
                            <div style="font-size:11px; color:#94a3b8; font-weight:400;">${item.category} | Exp: ${expiry}</div>
                        </td>
                        <td style="padding: 16px 20px;">
                            <div class="stock-progress-container">
                                <span style="font-weight: 700; width: 36px; text-align: right;">${item.currentStock}</span>
                                <div class="stock-progress-bar-bg">
                                    <div class="stock-progress-bar-fill ${statusClass}" style="width: ${item.stockPercent}%"></div>
                                </div>
                                <span style="font-size:11px;color:#94a3b8;">${item.unit}</span>
                            </div>
                        </td>
                        <td style="padding: 16px 20px;"><span class="stock-status-pill ${statusClass}">${item.stockStatus}</span></td>
                        <td style="padding: 16px 20px;">
                            <div style="display:flex; gap:8px; flex-wrap:wrap;">
                                <button class="btn-table-action-view" style="padding: 4px 10px; font-weight:600; color:var(--primary-green); border-color:var(--primary-green);"
                                    onclick="app.openRestockModal(${item.id}, '${item.name.replace(/'/g,"\\'")}', ${item.maxStock - item.currentStock})">
                                    <i class="fa-solid fa-plus"></i> Restock
                                </button>
                                <button class="btn-table-action-view" style="padding: 4px 10px; font-weight:600; color:#e2a100; border-color:#e2a100;"
                                    onclick="app.openDispenseModal(${item.id}, '${item.name.replace(/'/g,"\\'")}', ${item.currentStock})">
                                    <i class="fa-solid fa-minus"></i> Dispense
                                </button>
                            </div>
                        </td>
                    </tr>`;
            });

            // Load dispensing chart
            await this.loadDispensingChart();

        } catch (err) {
            tableBody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:#ef4444;">Error loading pharmacy.</td></tr>';
            console.error('Pharmacy load error:', err);
        }
    }

    openRestockModal(id, name, maxAdditional) {
        document.getElementById('stock-modal-title').textContent   = `Restock: ${name}`;
        document.getElementById('stock-modal-action').textContent  = 'Confirm Restock';
        document.getElementById('stock-modal-qty').value           = '';
        document.getElementById('stock-modal-notes').value         = '';
        document.getElementById('stock-modal-qty').max             = maxAdditional > 0 ? maxAdditional : 9999;
        document.getElementById('stock-modal-hint').textContent    = maxAdditional > 0
            ? `Can add up to ${maxAdditional} units to reach max stock.`
            : 'Already at maximum stock.';

        document.getElementById('stock-modal-action').onclick = () => this.submitRestock(id);
        document.getElementById('pharmacy-stock-modal').classList.add('active');
    }

    openDispenseModal(id, name, currentStock) {
        document.getElementById('stock-modal-title').textContent   = `Dispense: ${name}`;
        document.getElementById('stock-modal-action').textContent  = 'Confirm Dispense';
        document.getElementById('stock-modal-qty').value           = '';
        document.getElementById('stock-modal-notes').value         = '';
        document.getElementById('stock-modal-qty').max             = currentStock;
        document.getElementById('stock-modal-hint').textContent    = `Current stock: ${currentStock} units.`;

        document.getElementById('stock-modal-action').onclick = () => this.submitDispense(id);
        document.getElementById('pharmacy-stock-modal').classList.add('active');
    }

    closeStockModal() {
        document.getElementById('pharmacy-stock-modal').classList.remove('active');
    }

    async submitRestock(id) {
        const qty   = parseInt(document.getElementById('stock-modal-qty').value);
        const notes = document.getElementById('stock-modal-notes').value.trim();

        if (!qty || qty <= 0) {
            alert('Please enter a valid quantity to restock.');
            return;
        }

        try {
            const res = await fetch(`/api/pharmacy/medicines/${id}/restock`, {
                method:  'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({ quantity: qty, notes })
            });
            const data = await res.json();

            if (res.ok && data.success) {
                this.closeStockModal();
                await this.renderPharmacy();
                this.showToast(`Restocked successfully. New stock: ${data.currentStock}`, 'success');
            } else {
                alert(data.message || 'Failed to restock.');
            }
        } catch (err) {
            console.error('Restock error:', err);
        }
    }

    async submitDispense(id) {
        const qty   = parseInt(document.getElementById('stock-modal-qty').value);
        const notes = document.getElementById('stock-modal-notes').value.trim();

        if (!qty || qty <= 0) {
            alert('Please enter a valid quantity to dispense.');
            return;
        }

        try {
            const res = await fetch(`/api/pharmacy/medicines/${id}/dispense`, {
                method:  'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({ quantity: qty, notes })
            });
            const data = await res.json();

            if (res.ok && data.success) {
                this.closeStockModal();
                await this.renderPharmacy();
                this.showToast(`Dispensed successfully. Remaining: ${data.currentStock}`, 'success');
            } else {
                alert(data.message || 'Failed to dispense. ' + (data.message || ''));
            }
        } catch (err) {
            console.error('Dispense error:', err);
        }
    }

    openAddMedicineModal() {
        document.getElementById('add-medicine-form').reset();
        
        // Auto-generate batch number
        const now = new Date();
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        const time = String(now.getHours()).padStart(2, '0') + String(now.getMinutes()).padStart(2, '0');
        const batchNumber = `BATCH-${year}${month}${day}-${time}`;
        document.getElementById('med-batch').value = batchNumber;
        
        document.getElementById('add-medicine-modal').classList.add('active');
    }

    closeAddMedicineModal() {
        document.getElementById('add-medicine-modal').classList.remove('active');
    }

    async submitAddMedicine() {
        const name     = document.getElementById('med-name').value.trim();
        const category = document.getElementById('med-category').value.trim();
        const initQty  = parseInt(document.getElementById('med-initial-stock').value) || 0;
        const minStock = parseInt(document.getElementById('med-min-stock').value) || 20;
        const critSock = parseInt(document.getElementById('med-crit-stock').value) || 10;
        const maxStock = parseInt(document.getElementById('med-max-stock').value) || 100;
        const unit     = document.getElementById('med-unit').value.trim() || 'tablets';
        const batch    = document.getElementById('med-batch').value.trim();
        const expiry   = document.getElementById('med-expiry').value;

        if (!name) { alert('Medicine name is required.'); return; }

        try {
            const res = await fetch('/api/pharmacy/medicines', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({
                    name, category, initialStock: initQty, minStock,
                    criticalStock: critSock, maxStock, unit, batchNumber: batch,
                    expiryDate: expiry || null
                })
            });
            const data = await res.json();

            if (res.ok && data.success) {
                this.closeAddMedicineModal();
                await this.renderPharmacy();
                this.showToast(`${name} added to inventory.`, 'success');
            } else {
                alert(data.message || 'Failed to add medicine.');
            }
        } catch (err) {
            console.error('Add medicine error:', err);
        }
    }

    // ==========================================
    // WELLNESS PROGRAMS
    // ==========================================
    async renderWellness() {
        if (typeof WellnessManager !== 'undefined') {
            await WellnessManager.loadPrograms();
        }
    }

    // ==========================================
    // HEALTH TRENDS
    // ==========================================
    async renderHealthTrends() {
        try {
            const res = await fetch('/api/health-trends/statistics');
            const data = await res.json();

            // Update statistics
            document.getElementById('trend-avg-time').textContent = `${data.avgConsultationTime} min`;
            document.getElementById('trend-avg-time-change').innerHTML = `<i class="fa-solid fa-arrow-down"></i> ${data.avgConsultationTimeChange}m vs Q1`;
            
            document.getElementById('trend-top-day').textContent = data.topConsultationDay;
            document.getElementById('trend-top-day-pct').textContent = `${data.topConsultationDayPercentage}% of weekly visits`;
            
            document.getElementById('trend-referral-rate').textContent = `${data.referralRate}%`;
            document.getElementById('trend-referral-change').innerHTML = `<i class="fa-solid fa-arrow-down"></i> ${data.referralRateChange}% vs Q1`;
            
            document.getElementById('trend-preventive-rate').textContent = `${data.preventiveCheckupsRate}%`;
            document.getElementById('trend-preventive-change').innerHTML = `<i class="fa-solid fa-arrow-up"></i> +${data.preventiveCheckupsChange}% vs Q1`;

            // Update insights text
            document.getElementById('trend-insights-text').textContent = 
                'Our analytics pipeline highlights consultation patterns based on real EMR data. The system tracks consultation times, referral rates, and preventive healthcare metrics to support data-driven healthcare decisions.';

        } catch (err) {
            console.error('Health trends error:', err);
        }
    }

    openCreateProgramModal() {
        document.getElementById('create-program-form').reset();
        // Set default date to tomorrow
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        document.getElementById('program-date').value = tomorrow.toISOString().split('T')[0];
        document.getElementById('create-program-modal').classList.add('active');
    }

    closeCreateProgramModal() {
        document.getElementById('create-program-modal').classList.remove('active');
    }

    async submitCreateProgram() {
        const title = document.getElementById('program-title').value.trim();
        const category = document.getElementById('program-category').value;
        const venue = document.getElementById('program-venue').value.trim();
        const programDate = document.getElementById('program-date').value;
        const capacity = parseInt(document.getElementById('program-capacity').value);
        const targetDept = document.getElementById('program-target-dept').value;
        const description = document.getElementById('program-description').value.trim();

        if (!title || !category || !programDate || !capacity) {
            alert('Please fill in all required fields.');
            return;
        }

        const btn = document.querySelector('#create-program-modal .btn-primary-custom');
        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Creating...';

        try {
            const res = await fetch('/api/wellness/programs', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    title,
                    category,
                    venue,
                    programDate,
                    participantCapacity: capacity,
                    targetDepartment: targetDept,
                    description,
                    createdBy: this.currentUser
                })
            });

            const data = await res.json();
            if (res.ok && data.success) {
                this.closeCreateProgramModal();
                await this.renderWellness();
                this.showToast(`Program "${title}" created successfully!`, 'success');
            } else {
                alert(data.message || 'Failed to create program.');
            }
        } catch (err) {
            console.error('Create program error:', err);
            alert('Failed to create program.');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="fa-solid fa-plus"></i> Create Program';
        }
    }

    // ==========================================
    // NEW VISIT MODAL
    // ==========================================
    async populateEmployeeDropdown() {
        const dropdown = document.getElementById('visit-autofill');
        if (!dropdown) return;
        dropdown.innerHTML = '<option value="">-- Manual Entry --</option>';

        try {
            const res  = await fetch('/api/emr');
            const emps = await res.json();
            emps.forEach(emp => {
                dropdown.innerHTML += `<option value="${emp.id}" data-name="${emp.name}" data-dept="${emp.department}" data-age="${emp.age}" data-gender="${emp.gender}" data-contact="${emp.contactNumber}" data-address="${emp.address}" data-occupation="${emp.occupation}">${emp.name} (${emp.department})</option>`;
            });
        } catch (_) {}
    }

    autofillVisitForm(selectEl) {
        const empId = selectEl.value;
        if (!empId) {
            document.getElementById('visit-name').value       = '';
            document.getElementById('visit-age').value        = '';
            document.getElementById('visit-sex').value        = '';
            document.getElementById('visit-birthday').value   = '';
            document.getElementById('visit-contact').value    = '';
            document.getElementById('visit-occupation').value = '';
            document.getElementById('visit-address').value    = '';
            document.getElementById('visit-dept').value       = '';
            return;
        }

        const opt = selectEl.options[selectEl.selectedIndex];
        document.getElementById('visit-name').value       = opt.dataset.name        || '';
        document.getElementById('visit-age').value        = opt.dataset.age         || '';
        document.getElementById('visit-sex').value        = opt.dataset.gender      || '';
        document.getElementById('visit-contact').value    = opt.dataset.contact     || '';
        document.getElementById('visit-address').value    = opt.dataset.address     || '';
        document.getElementById('visit-occupation').value = opt.dataset.occupation  || '';
        document.getElementById('visit-dept').value       = opt.dataset.dept        || '';
    }

    toggleVitalInput(checkboxId, inputId) {
        const checkbox = document.getElementById(checkboxId);
        const input    = document.getElementById(inputId);
        if (checkbox && input) {
            input.disabled = !checkbox.checked;
            if (!checkbox.checked) { input.value = ''; }
            else { input.focus(); }
        }
    }

    toggleSocialCheckbox(checkedId, oppositeId, inputId) {
        const checkedBox  = document.getElementById(checkedId);
        const oppositeBox = document.getElementById(oppositeId);
        const input       = document.getElementById(inputId);

        if (checkedBox && checkedBox.checked) {
            if (oppositeBox) oppositeBox.checked = false;
            const needsInput = ['check-smoker', 'check-alcohol', 'check-bev'].includes(checkedId);
            if (input) { input.disabled = !needsInput; if (!needsInput) input.value = ''; if (needsInput) input.focus(); }
        } else {
            if (['check-smoker', 'check-alcohol', 'check-bev'].includes(checkedId)) {
                if (input) { input.disabled = true; input.value = ''; }
            }
        }
    }

    openNewVisitModal() {
        this.populateEmployeeDropdown();

        document.getElementById('new-visit-form').reset();
        document.getElementById('visit-autofill').value = '';

        const today = new Date().toISOString().split('T')[0];
        document.getElementById('visit-consult-date').value = today;

        // Disable all vital/social inputs initially
        ['vital-bp','vital-pr','vital-rr','vital-temp','vital-wt','vital-ht','vital-wc',
         'vital-sticks','vital-bottles-alc','vital-bottles-bev'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.disabled = true;
        });

        document.getElementById('new-visit-modal').classList.add('active');
    }

    closeNewVisitModal() {
        document.getElementById('new-visit-modal').classList.remove('active');
    }

    async submitNewVisit() {
        const name       = document.getElementById('visit-name').value.trim();
        const ageVal     = document.getElementById('visit-age').value.trim();
        const sex        = document.getElementById('visit-sex').value;
        const dept       = document.getElementById('visit-dept').value;
        const contactVal = document.getElementById('visit-contact').value.trim();
        const symptom    = document.getElementById('visit-complain').value.trim();
        const consultDate= document.getElementById('visit-consult-date').value;

        if (!name || !ageVal || !sex || !dept || !symptom || !consultDate) {
            alert('Please fill in all required fields.');
            return;
        }

        if (!/^09\d{9}$/.test(contactVal)) {
            alert('Contact number must be a valid Philippines mobile number starting with 09 and exactly 11 digits (e.g. 09171234567).');
            return;
        }

        if (!/^\d+$/.test(ageVal)) {
            alert('Age must be a valid number.');
            return;
        }

        const payload = {
            patientName:   name,
            department:    dept,
            age:           parseInt(ageVal),
            sex:           sex,
            contactNumber: contactVal,
            address:       document.getElementById('visit-address').value.trim(),
            occupation:    document.getElementById('visit-occupation').value.trim(),
            familyHistory: document.getElementById('visit-family-history').value.trim(),
            chiefComplain: symptom,
            birthday:      document.getElementById('visit-birthday').value || null,

            vitalBP:   document.getElementById('check-bp').checked   ? document.getElementById('vital-bp').value   : null,
            vitalPR:   document.getElementById('check-pr').checked   ? document.getElementById('vital-pr').value   : null,
            vitalRR:   document.getElementById('check-rr').checked   ? document.getElementById('vital-rr').value   : null,
            vitalTemp: document.getElementById('check-temp').checked  ? document.getElementById('vital-temp').value : null,
            vitalWT:   document.getElementById('check-wt').checked   ? document.getElementById('vital-wt').value   : null,
            vitalHT:   document.getElementById('check-ht').checked   ? document.getElementById('vital-ht').value   : null,
            vitalWC:   document.getElementById('check-wc').checked   ? document.getElementById('vital-wc').value   : null,

            isSmoker:         document.getElementById('check-smoker').checked,
            sticksPerDay:     document.getElementById('check-smoker').checked    ? parseInt(document.getElementById('vital-sticks').value) || null : null,
            isAlcoholDrinker: document.getElementById('check-alcohol').checked,
            bottlesAlcohol:   document.getElementById('check-alcohol').checked   ? parseInt(document.getElementById('vital-bottles-alc').value) || null : null,
            isBeverageDrinker:document.getElementById('check-bev').checked,
            bottlesBeverage:  document.getElementById('check-bev').checked       ? parseInt(document.getElementById('vital-bottles-bev').value) || null : null,
        };

        const btn = document.querySelector('#new-visit-form button[type="submit"]');
        btn.disabled = true;
        btn.textContent = 'Saving…';

        try {
            const res  = await fetch('/api/queue', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify(payload)
            });
            const data = await res.json();

            if (res.ok && data.success) {
                this.closeNewVisitModal();
                this.showToast(`${name} added to the waiting queue!`, 'success');

                // Reload queue and EMR
                await this.renderQueue();
                await this.renderEmr();
                this.switchTab('queue-tab');
            } else if (res.status === 409) {
                alert(data.message);
            } else {
                alert(data.message || 'Failed to add visit.');
            }
        } catch (err) {
            alert('Network error. Please try again.');
            console.error('Submit visit error:', err);
        } finally {
            btn.disabled = false;
            btn.textContent = 'Add to Queue';
        }
    }

    // ==========================================
    // TOAST NOTIFICATION
    // ==========================================
    showToast(message, type = 'success') {
        let toast = document.getElementById('app-toast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'app-toast';
            toast.style.cssText = `
                position: fixed; bottom: 32px; right: 32px; z-index: 9999;
                background: #0a7a57; color: white; padding: 14px 24px;
                border-radius: 12px; font-size: 14px; font-weight: 600;
                box-shadow: 0 8px 32px rgba(0,0,0,0.18);
                transition: all 0.3s ease; opacity: 0; transform: translateY(20px);
                max-width: 360px; line-height: 1.4;
            `;
            document.body.appendChild(toast);
        }
        toast.style.background = type === 'success' ? '#0a7a57' : '#ef4444';
        toast.textContent = message;
        toast.style.opacity = '1';
        toast.style.transform = 'translateY(0)';

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(20px)';
        }, 3500);
    }

    toggleNotifications() {
        this.showToast('No new alerts. All medical sync streams are operating normally.');
    }
}

// Instantiate global app controller
const app = new UniHealthApp();

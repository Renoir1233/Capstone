// Employee Portal App
const employeeApp = {
    init() {
        this.setupTabSwitching();
        this.setupUserMenu();
        this.setupMobileMenu();
        this.loadDashboard();
    },

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
    },

    setupTabSwitching() {
        document.querySelectorAll('[data-tab]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const tabId = link.getAttribute('data-tab');
                this.switchTab(tabId);
            });
        });
    },

    setupUserMenu() {
        const userMenuToggle = document.getElementById('user-menu-toggle');
        const userProfileTrigger = document.getElementById('user-profile-trigger');
        const userDropdown = document.getElementById('user-dropdown-menu');
        
        if (userMenuToggle && userDropdown) {
            userMenuToggle.addEventListener('click', (e) => {
                e.stopPropagation();
                const isVisible = userDropdown.style.display === 'block';
                userDropdown.style.display = isVisible ? 'none' : 'block';
                const icon = userMenuToggle.querySelector('i');
                icon.className = isVisible ? 'fa-solid fa-chevron-up' : 'fa-solid fa-chevron-down';
            });

            userProfileTrigger.addEventListener('click', (e) => {
                e.stopPropagation();
                const isVisible = userDropdown.style.display === 'block';
                userDropdown.style.display = isVisible ? 'none' : 'block';
                const icon = userMenuToggle.querySelector('i');
                icon.className = isVisible ? 'fa-solid fa-chevron-up' : 'fa-solid fa-chevron-down';
            });

            // Close dropdown when clicking outside
            document.addEventListener('click', () => {
                userDropdown.style.display = 'none';
                const icon = userMenuToggle.querySelector('i');
                icon.className = 'fa-solid fa-chevron-up';
            });

            // Prevent dropdown from closing when clicking inside it
            userDropdown.addEventListener('click', (e) => {
                userDropdown.style.display = 'none';
                const icon = userMenuToggle.querySelector('i');
                icon.className = 'fa-solid fa-chevron-up';
            });
        }
    },

    toggleAIChat() {
        const chatWindow = document.getElementById('ai-chat-window');
        const bubble = document.getElementById('ai-chat-bubble');
        
        if (chatWindow.style.display === 'none' || chatWindow.style.display === '') {
            chatWindow.style.display = 'block';
            bubble.style.display = 'none';
        } else {
            chatWindow.style.display = 'none';
            bubble.style.display = 'flex';
        }
    },

    sendAIChatMessage(event) {
        event.preventDefault();
        const input = document.getElementById('ai-chat-input');
        const message = input.value.trim();
        if (!message) return false;

        this.appendAIChatMessage('user', message);
        input.value = '';

        setTimeout(() => {
            const response = this.getAIResponse(message);
            this.appendAIChatMessage('ai', response);
        }, 1000);

        return false;
    },

    appendAIChatMessage(sender, text) {
        const container = document.getElementById('ai-chat-messages');
        const messageDiv = document.createElement('div');
        messageDiv.style.marginBottom = '16px';
        messageDiv.style.display = 'flex';
        
        if (sender === 'user') {
            messageDiv.style.justifyContent = 'flex-end';
            messageDiv.innerHTML = `
                <div style="background: #3b82f6; color: white; padding: 12px 16px; border-radius: 8px; max-width: 80%;">
                    <p style="margin: 0; font-size: 14px;">${text}</p>
                </div>
            `;
        } else {
            messageDiv.innerHTML = `
                <div style="width: 40px; height: 40px; background: #3b82f6; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; flex-shrink: 0;">
                    <i class="fa-solid fa-robot"></i>
                </div>
                <div style="margin-left: 12px; background: white; padding: 12px 16px; border-radius: 8px; max-width: 80%;">
                    <p style="margin: 0 0 4px 0; font-weight: 600; font-size: 13px;">AI Assistant</p>
                    <p style="margin: 0; font-size: 14px;">${text}</p>
                </div>
            `;
        }
        
        container.appendChild(messageDiv);
        document.getElementById('ai-chat-container').scrollTop = document.getElementById('ai-chat-container').scrollHeight;
    },

    switchTab(tabId) {
        // Close user menu if open
        const userDropdown = document.getElementById('user-dropdown-menu');
        if (userDropdown) {
            userDropdown.style.display = 'none';
            const icon = document.querySelector('#user-menu-toggle i');
            if (icon) icon.className = 'fa-solid fa-chevron-up';
        }

        // Update nav links
        document.querySelectorAll('.nav-link-custom').forEach(l => l.classList.remove('active'));
        document.querySelector(`[data-tab="${tabId}"]`)?.classList.add('active');

        // Update tab sections
        document.querySelectorAll('.tab-section').forEach(section => section.classList.remove('active'));
        document.getElementById(tabId)?.classList.add('active');

        // Update page title
        const titles = {
            'dashboard-tab': 'Dashboard',
            'appointments-tab': 'Appointments',
            'profile-tab': 'My Profile',
            'emr-tab': 'Medical Records',
            'prescriptions-tab': 'Prescriptions',
            'lab-results-tab': 'Lab Results',
            'documents-tab': 'Documents',
            'reminders-tab': 'Health Reminders',
            'ai-assistant-tab': 'AI Clinic Assistant',
            'settings-tab': 'Settings'
        };
        document.getElementById('page-title').textContent = titles[tabId] || 'Employee Portal';

        // Load content based on tab
        if (tabId === 'profile-tab' && !document.getElementById('profile-content').hasAttribute('data-loaded')) {
            this.loadProfile();
        } else if (tabId === 'emr-tab' && !document.getElementById('emr-content').hasAttribute('data-loaded')) {
            this.loadMedicalRecords();
        } else if (tabId === 'prescriptions-tab' && !document.getElementById('prescriptions-content').hasAttribute('data-loaded')) {
            this.loadPrescriptions();
        } else if (tabId === 'reminders-tab' && !document.getElementById('reminders-content').hasAttribute('data-loaded')) {
            this.loadAllReminders();
        } else if (tabId === 'ai-assistant-tab' && !document.getElementById('ai-content').hasAttribute('data-loaded')) {
            this.loadAIAssistant();
        } else if (tabId === 'settings-tab' && !document.getElementById('settings-content').hasAttribute('data-loaded')) {
            this.loadSettings();
        }
    },

    async loadDashboard() {
        try {
            const response = await fetch('/api/employee/dashboard-summary');
            const data = await response.json();

            // Update stats cards
            document.getElementById('stat-total-visits').textContent = data.totalVisits || 0;
            document.getElementById('stat-last-visit').textContent = data.lastVisitDate || 'N/A';
            document.getElementById('stat-active-prescriptions').textContent = data.activePrescriptions || 0;
            document.getElementById('stat-pending-reminders').textContent = data.upcomingReminders?.length || 0;

            // Load reminders list
            this.displayReminders(data.upcomingReminders || []);

            // Load consultations
            const consultResponse = await fetch('/api/employee/recent-consultations');
            const consultData = await consultResponse.json();
            this.displayConsultations(consultData || []);

        } catch (error) {
            console.error('Failed to load dashboard:', error);
        }
    },

    displayReminders(reminders) {
        const container = document.getElementById('reminders-list');
        if (reminders.length === 0) {
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #94a3b8;"><i class="fa-solid fa-check-circle" style="font-size: 48px; margin-bottom: 12px;"></i><p>No upcoming reminders</p></div>';
            return;
        }

        const html = reminders.map(r => {
            const daysColor = r.daysUntil <= 7 ? '#ef4444' : r.daysUntil <= 14 ? '#f59e0b' : '#3b82f6';
            return `
                <div style="border-bottom: 1px solid #e2e8f0; padding: 16px 20px;">
                    <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 8px;">
                        <h4 style="margin: 0; font-size: 15px; font-weight: 600; color: #1e293b;">${r.reminderType}</h4>
                        <span style="background: ${daysColor}; color: white; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                            ${r.daysUntil} days
                        </span>
                    </div>
                    <p style="margin: 0; font-size: 13px; color: #64748b;">Due: ${new Date(r.dueDate).toLocaleDateString('en-US', {month: 'long', day: 'numeric', year: 'numeric'})}</p>
                </div>
            `;
        }).join('');
        container.innerHTML = html;
    },

    displayConsultations(consultations) {
        const container = document.getElementById('consultations-list');
        if (consultations.length === 0) {
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #94a3b8;"><i class="fa-solid fa-clipboard-medical" style="font-size: 48px; margin-bottom: 12px;"></i><p>No consultation history</p></div>';
            return;
        }

        const html = consultations.map(c => `
            <div style="border-bottom: 1px solid #e2e8f0; padding: 16px 20px;">
                <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 8px;">
                    <h4 style="margin: 0; font-size: 15px; font-weight: 600; color: #1e293b;">${c.chiefComplain}</h4>
                    <span style="background: #ecfdf5; color: #10b981; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                        ${c.queueStatus}
                    </span>
                </div>
                <p style="margin: 4px 0 0 0; font-size: 13px; color: #64748b;">${new Date(c.dateOfConsultation).toLocaleDateString('en-US', {month: 'short', day: 'numeric', year: 'numeric'})}</p>
                ${c.outcome ? `<p style="margin: 4px 0 0 0; font-size: 13px; color: #10b981; font-weight: 500;">${c.outcome}</p>` : ''}
            </div>
        `).join('');
        container.innerHTML = html;
    },

    async loadProfile() {
        const container = document.getElementById('profile-content');
        try {
            const response = await fetch('/EmployeePortal/Profile');
            const html = await response.text();
            // Extract just the content part (remove layout)
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const content = doc.querySelector('.row') || doc.body;
            container.innerHTML = content.innerHTML;
            container.setAttribute('data-loaded', 'true');
            
            // Attach event listeners to toggle visibility buttons
            setTimeout(() => {
                document.querySelectorAll('.toggle-visibility-btn').forEach(btn => {
                    btn.addEventListener('click', function(e) {
                        e.preventDefault();
                        const targetId = this.getAttribute('data-target');
                        const input = document.getElementById(targetId);
                        const icon = this.querySelector('i');
                        
                        if (input && icon) {
                            if (input.type === 'password') {
                                input.type = 'text';
                                icon.className = 'fa-regular fa-eye-slash';
                            } else {
                                input.type = 'password';
                                icon.className = 'fa-regular fa-eye';
                            }
                        }
                    });
                });
            }, 100);
        } catch (error) {
            container.innerHTML = '<div class="alert alert-danger">Failed to load profile</div>';
        }
    },

    async loadMedicalRecords() {
        const container = document.getElementById('emr-content');
        try {
            const response = await fetch('/EmployeePortal/MedicalRecords');
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const content = doc.querySelector('.row') || doc.body;
            container.innerHTML = content.innerHTML;
            container.setAttribute('data-loaded', 'true');
        } catch (error) {
            container.innerHTML = '<div class="alert alert-danger">Failed to load medical records</div>';
        }
    },

    async loadPrescriptions() {
        const container = document.getElementById('prescriptions-content');
        try {
            const response = await fetch('/EmployeePortal/Prescriptions');
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const content = doc.querySelector('.row') || doc.body;
            container.innerHTML = content.innerHTML;
            container.setAttribute('data-loaded', 'true');
        } catch (error) {
            container.innerHTML = '<div class="alert alert-danger">Failed to load prescriptions</div>';
        }
    },

    async loadAllReminders() {
        const container = document.getElementById('reminders-content');
        try {
            const response = await fetch('/EmployeePortal/HealthReminders');
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const content = doc.querySelector('.row') || doc.body;
            container.innerHTML = content.innerHTML;
            container.setAttribute('data-loaded', 'true');
        } catch (error) {
            container.innerHTML = '<div class="alert alert-danger">Failed to load health reminders</div>';
        }
    },

    loadAIAssistant() {
        const container = document.getElementById('ai-content');
        container.innerHTML = `
            <div style="max-width: 800px; margin: 0 auto;">
                <div style="background: white; border-radius: 12px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); overflow: hidden;">
                    <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px;">
                        <h5 style="margin: 0;"><i class="fa-solid fa-robot"></i> AI Health Assistant</h5>
                    </div>
                    <div id="chat-container" style="height: 400px; overflow-y: auto; padding: 20px; background: #f8fafc;">
                        <div style="display: flex; margin-bottom: 16px;">
                            <div style="width: 40px; height: 40px; background: #3b82f6; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; flex-shrink: 0;">
                                <i class="fa-solid fa-robot"></i>
                            </div>
                            <div style="margin-left: 12px; background: white; padding: 12px 16px; border-radius: 8px; max-width: 80%;">
                                <p style="margin: 0 0 8px 0; font-weight: 600;">AI Assistant</p>
                                <p style="margin: 0;">Hello! I'm your AI Clinic Assistant. I can help you with:</p>
                                <ul style="margin: 8px 0 0 0;">
                                    <li>General health information</li>
                                    <li>Understanding your medical records</li>
                                    <li>Medication reminders</li>
                                    <li>Wellness tips</li>
                                </ul>
                            </div>
                        </div>
                        <div id="chat-messages"></div>
                    </div>
                    <div style="padding: 16px; background: white; border-top: 1px solid #e2e8f0;">
                        <form id="chat-form" onsubmit="return employeeApp.sendAIMessage(event);" style="display: flex; gap: 8px;">
                            <input type="text" id="chat-input" placeholder="Type your message..." style="flex: 1; padding: 10px 14px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                            <button type="submit" style="padding: 10px 20px; background: #3b82f6; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 600;">
                                <i class="fa-solid fa-paper-plane"></i> Send
                            </button>
                        </form>
                    </div>
                </div>

                <div style="margin-top: 20px; background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
                    <h6 style="margin: 0 0 16px 0;"><i class="fa-solid fa-lightbulb"></i> Quick Questions</h6>
                    <div style="display: grid; gap: 8px;">
                        <button onclick="employeeApp.askQuickQuestion('What are my upcoming health reminders?')" style="text-align: left; padding: 12px; border: 1px solid #e2e8f0; background: white; border-radius: 8px; cursor: pointer; transition: all 0.3s;">
                            <i class="fa-solid fa-bell"></i> What are my upcoming health reminders?
                        </button>
                        <button onclick="employeeApp.askQuickQuestion('Can you explain my latest prescription?')" style="text-align: left; padding: 12px; border: 1px solid #e2e8f0; background: white; border-radius: 8px; cursor: pointer; transition: all 0.3s;">
                            <i class="fa-solid fa-prescription-bottle-medical"></i> Can you explain my latest prescription?
                        </button>
                        <button onclick="employeeApp.askQuickQuestion('What should I do if I have a fever?')" style="text-align: left; padding: 12px; border: 1px solid #e2e8f0; background: white; border-radius: 8px; cursor: pointer; transition: all 0.3s;">
                            <i class="fa-solid fa-thermometer"></i> What should I do if I have a fever?
                        </button>
                    </div>
                </div>
            </div>
        `;
        container.setAttribute('data-loaded', 'true');
    },

    sendAIMessage(event) {
        event.preventDefault();
        const input = document.getElementById('chat-input');
        const message = input.value.trim();
        if (!message) return false;

        this.appendChatMessage('user', message);
        input.value = '';

        setTimeout(() => {
            const response = this.getAIResponse(message);
            this.appendChatMessage('ai', response);
        }, 1000);

        return false;
    },

    askQuickQuestion(question) {
        document.getElementById('chat-input').value = question;
        this.sendAIMessage(new Event('submit'));
    },

    appendChatMessage(sender, text) {
        const container = document.getElementById('chat-messages');
        const messageDiv = document.createElement('div');
        messageDiv.style.marginBottom = '16px';
        messageDiv.style.display = 'flex';
        
        if (sender === 'user') {
            messageDiv.style.justifyContent = 'flex-end';
            messageDiv.innerHTML = `
                <div style="background: #3b82f6; color: white; padding: 12px 16px; border-radius: 8px; max-width: 80%;">
                    <p style="margin: 0;">${text}</p>
                </div>
            `;
        } else {
            messageDiv.innerHTML = `
                <div style="width: 40px; height: 40px; background: #3b82f6; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; flex-shrink: 0;">
                    <i class="fa-solid fa-robot"></i>
                </div>
                <div style="margin-left: 12px; background: white; padding: 12px 16px; border-radius: 8px; max-width: 80%;">
                    <p style="margin: 0 0 4px 0; font-weight: 600; font-size: 13px;">AI Assistant</p>
                    <p style="margin: 0;">${text}</p>
                </div>
            `;
        }
        
        container.appendChild(messageDiv);
        document.getElementById('chat-container').scrollTop = document.getElementById('chat-container').scrollHeight;
    },

    getAIResponse(message) {
        const lowerMessage = message.toLowerCase();
        
        if (lowerMessage.includes('reminder')) {
            return 'Based on your records, you have upcoming reminders for Annual Physical Exam (30 days), Flu Vaccination (15 days), and Blood Pressure Check (7 days). Would you like to schedule any of these?';
        }
        if (lowerMessage.includes('prescription') || lowerMessage.includes('medicine')) {
            return 'I can help you understand your prescriptions. Please check the "My Prescriptions" tab for detailed information about your medications including dosage, frequency, and instructions.';
        }
        if (lowerMessage.includes('fever')) {
            return 'For fever management: 1) Rest and stay hydrated, 2) Take fever-reducing medication as prescribed, 3) Monitor your temperature regularly, 4) If fever persists for more than 3 days or exceeds 39°C, please visit the clinic immediately.';
        }
        
        return 'Thank you for your question. As this is a prototype, I can provide general health information. For specific medical advice, please consult with our healthcare providers at the clinic. Would you like me to help you schedule an appointment?';
    },

    async loadSettings() {
        const container = document.getElementById('settings-content');
        try {
            const response = await fetch('/EmployeePortal/Settings');
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const content = doc.querySelector('.row') || doc.body;
            container.innerHTML = content.innerHTML;
            container.setAttribute('data-loaded', 'true');
        } catch (error) {
            container.innerHTML = '<div class="alert alert-danger">Failed to load settings</div>';
        }
    },

    async logout() {
        try {
            const response = await fetch('/Auth/Logout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            });
            if (response.ok) {
                window.location.href = '/';
            }
        } catch (error) {
            console.error('Logout failed:', error);
            window.location.href = '/';
        }
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    employeeApp.init();
});


// View button functionality
function viewLabResult(testName, date, doctor) {
    const modal = document.createElement('div');
    modal.style.cssText = 'position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 10000;';
    
    modal.innerHTML = `
        <div style="background: white; border-radius: 12px; max-width: 600px; width: 90%; max-height: 80vh; overflow-y: auto; box-shadow: 0 20px 60px rgba(0,0,0,0.3);">
            <div style="padding: 24px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center;">
                <h4 style="margin: 0; font-size: 20px; font-weight: 600; color: #1e293b;">${testName}</h4>
                <button onclick="this.closest('div[style*=fixed]').remove()" style="background: none; border: none; font-size: 24px; color: #94a3b8; cursor: pointer; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;">&times;</button>
            </div>
            <div style="padding: 24px;">
                <div style="margin-bottom: 20px;">
                    <p style="margin: 0; font-size: 13px; color: #64748b;">Ordered by ${doctor}</p>
                    <p style="margin: 4px 0 0 0; font-size: 13px; color: #64748b;">${date}</p>
                </div>
                
                <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin-bottom: 20px;">
                    <h6 style="margin: 0 0 12px 0; font-size: 14px; font-weight: 600; color: #1e293b;">Test Results</h6>
                    <div style="display: grid; gap: 12px;">
                        <div style="display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e2e8f0;">
                            <span style="font-size: 14px; color: #64748b;">Hemoglobin</span>
                            <span style="font-size: 14px; font-weight: 600; color: #1e293b;">14.2 g/dL <span style="color: #10b981; font-size: 12px;">✓ Normal</span></span>
                        </div>
                        <div style="display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e2e8f0;">
                            <span style="font-size: 14px; color: #64748b;">WBC Count</span>
                            <span style="font-size: 14px; font-weight: 600; color: #1e293b;">7,500 /μL <span style="color: #10b981; font-size: 12px;">✓ Normal</span></span>
                        </div>
                        <div style="display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e2e8f0;">
                            <span style="font-size: 14px; color: #64748b;">Platelet Count</span>
                            <span style="font-size: 14px; font-weight: 600; color: #1e293b;">250,000 /μL <span style="color: #10b981; font-size: 12px;">✓ Normal</span></span>
                        </div>
                        <div style="display: flex; justify-content: space-between; padding: 8px 0;">
                            <span style="font-size: 14px; color: #64748b;">Hematocrit</span>
                            <span style="font-size: 14px; font-weight: 600; color: #1e293b;">42% <span style="color: #10b981; font-size: 12px;">✓ Normal</span></span>
                        </div>
                    </div>
                </div>
                
                <div style="background: #ecfdf5; border: 1px solid #10b981; border-radius: 8px; padding: 16px; margin-bottom: 20px;">
                    <h6 style="margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #10b981;"><i class="fa-solid fa-circle-check"></i> Interpretation</h6>
                    <p style="margin: 0; font-size: 14px; color: #064e3b;">All values are within normal range. No abnormalities detected.</p>
                </div>
                
                <p style="margin: 0; font-size: 12px; color: #94a3b8; font-style: italic;">
                    <i class="fa-solid fa-circle-info"></i> These results have been reviewed by ${doctor}. If you have any questions, please contact the clinic.
                </p>
            </div>
            <div style="padding: 16px 24px; background: #f8fafc; border-top: 1px solid #e2e8f0; display: flex; gap: 12px;">
                <button onclick="alert('Downloading ${testName} results...')" style="flex: 1; padding: 12px; background: #3b82f6; color: white; border: none; border-radius: 8px; font-weight: 600; cursor: pointer;">
                    <i class="fa-solid fa-download"></i> Download PDF
                </button>
                <button onclick="this.closest('div[style*=fixed]').remove()" style="flex: 1; padding: 12px; background: white; color: #64748b; border: 1px solid #e2e8f0; border-radius: 8px; font-weight: 600; cursor: pointer;">
                    Close
                </button>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    modal.addEventListener('click', (e) => {
        if (e.target === modal) modal.remove();
    });
}

function viewDocument(docName, date, doctor, docType) {
    const modal = document.createElement('div');
    modal.style.cssText = 'position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 10000;';
    
    let content = '';
    
    if (docType === 'certificate') {
        content = `
            <div style="text-align: center; padding: 40px; background: white; border: 2px solid #3b82f6; margin: 20px; border-radius: 8px;">
                <div style="margin-bottom: 20px;">
                    <h3 style="margin: 0 0 8px 0; color: #1e293b; font-size: 24px;">UniHealth Clinic</h3>
                    <p style="margin: 0; color: #64748b; font-size: 14px;">Employee Health Services</p>
                </div>
                
                <h4 style="margin: 24px 0; color: #3b82f6; font-size: 20px; font-weight: 600;">MEDICAL CERTIFICATE</h4>
                
                <div style="text-align: left; padding: 24px; background: #f8fafc; border-radius: 8px; margin: 20px 0;">
                    <p style="margin: 0 0 12px 0; font-size: 14px; color: #1e293b;">This is to certify that:</p>
                    <p style="margin: 0 0 20px 0; font-size: 16px; font-weight: 600; color: #1e293b;">${document.getElementById('user-display-name')?.textContent || 'Employee Name'}</p>
                    
                    <p style="margin: 0 0 12px 0; font-size: 14px; color: #1e293b;">Was examined on <strong>${date}</strong> and diagnosed with:</p>
                    <p style="margin: 0 0 20px 0; font-size: 15px; color: #1e293b; background: white; padding: 12px; border-radius: 4px; border-left: 3px solid #3b82f6;">Acute febrile illness (Fever)</p>
                    
                    <p style="margin: 0; font-size: 14px; color: #1e293b;">The patient is advised to take <strong>3 days rest</strong> from <strong>June 27-29, 2026</strong> to recover.</p>
                </div>
                
                <div style="margin-top: 40px; text-align: right;">
                    <p style="margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #1e293b;">${doctor}</p>
                    <p style="margin: 0; font-size: 13px; color: #64748b;">Company Physician</p>
                    <p style="margin: 8px 0 0 0; font-size: 12px; color: #94a3b8;">License No.: 12345678</p>
                </div>
            </div>
        `;
    } else if (docType === 'clearance') {
        content = `
            <div style="text-align: center; padding: 40px; background: white; border: 2px solid #10b981; margin: 20px; border-radius: 8px;">
                <div style="margin-bottom: 20px;">
                    <h3 style="margin: 0 0 8px 0; color: #1e293b; font-size: 24px;">UniHealth Clinic</h3>
                    <p style="margin: 0; color: #64748b; font-size: 14px;">Employee Health Services</p>
                </div>
                
                <h4 style="margin: 24px 0; color: #10b981; font-size: 20px; font-weight: 600;">FIT-TO-WORK CLEARANCE</h4>
                
                <div style="text-align: left; padding: 24px; background: #f8fafc; border-radius: 8px; margin: 20px 0;">
                    <p style="margin: 0 0 12px 0; font-size: 14px; color: #1e293b;">This is to certify that:</p>
                    <p style="margin: 0 0 20px 0; font-size: 16px; font-weight: 600; color: #1e293b;">${document.getElementById('user-display-name')?.textContent || 'Employee Name'}</p>
                    
                    <p style="margin: 0 0 12px 0; font-size: 14px; color: #1e293b;">Was examined on <strong>${date}</strong> and found to be:</p>
                    
                    <div style="background: #ecfdf5; border: 2px solid #10b981; border-radius: 8px; padding: 16px; margin: 12px 0;">
                        <p style="margin: 0; font-size: 16px; font-weight: 600; color: #10b981; text-align: center;">
                            <i class="fa-solid fa-circle-check"></i> FIT TO WORK
                        </p>
                    </div>
                    
                    <p style="margin: 20px 0 0 0; font-size: 14px; color: #1e293b;">The employee may resume regular work duties without restrictions effective <strong>${date}</strong>.</p>
                </div>
                
                <div style="margin-top: 40px; text-align: right;">
                    <p style="margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #1e293b;">${doctor}</p>
                    <p style="margin: 0; font-size: 13px; color: #64748b;">Company Physician</p>
                    <p style="margin: 8px 0 0 0; font-size: 12px; color: #94a3b8;">License No.: 12345678</p>
                </div>
            </div>
        `;
    } else {
        content = `
            <div style="text-align: center; padding: 40px; background: white; border: 2px solid #8b5cf6; margin: 20px; border-radius: 8px;">
                <div style="margin-bottom: 20px;">
                    <h3 style="margin: 0 0 8px 0; color: #1e293b; font-size: 24px;">UniHealth Clinic</h3>
                    <p style="margin: 0; color: #64748b; font-size: 14px;">Employee Health Services</p>
                </div>
                
                <h4 style="margin: 24px 0; color: #8b5cf6; font-size: 20px; font-weight: 600;">REFERRAL TO SPECIALIST</h4>
                
                <div style="text-align: left; padding: 24px; background: #f8fafc; border-radius: 8px; margin: 20px 0;">
                    <p style="margin: 0 0 12px 0; font-size: 14px; color: #1e293b;">Patient:</p>
                    <p style="margin: 0 0 20px 0; font-size: 16px; font-weight: 600; color: #1e293b;">${document.getElementById('user-display-name')?.textContent || 'Employee Name'}</p>
                    
                    <p style="margin: 0 0 8px 0; font-size: 14px; color: #1e293b;">Date: <strong>${date}</strong></p>
                    <p style="margin: 0 0 20px 0; font-size: 14px; color: #1e293b;">Referring to: <strong>Pulmonology Specialist</strong></p>
                    
                    <div style="background: white; border-left: 3px solid #8b5cf6; padding: 16px; margin: 12px 0;">
                        <p style="margin: 0 0 8px 0; font-size: 13px; font-weight: 600; color: #1e293b;">Reason for Referral:</p>
                        <p style="margin: 0; font-size: 14px; color: #1e293b;">Persistent cough for 3 weeks, requires specialist evaluation and management.</p>
                    </div>
                    
                    <div style="background: white; border-left: 3px solid #8b5cf6; padding: 16px; margin: 12px 0;">
                        <p style="margin: 0 0 8px 0; font-size: 13px; font-weight: 600; color: #1e293b;">Clinical Findings:</p>
                        <p style="margin: 0; font-size: 14px; color: #1e293b;">Patient presents with non-productive cough, no fever. Chest x-ray shows minor inflammation.</p>
                    </div>
                </div>
                
                <div style="margin-top: 40px; text-align: right;">
                    <p style="margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #1e293b;">${doctor}</p>
                    <p style="margin: 0; font-size: 13px; color: #64748b;">Company Physician</p>
                    <p style="margin: 8px 0 0 0; font-size: 12px; color: #94a3b8;">License No.: 12345678</p>
                </div>
            </div>
        `;
    }
    
    modal.innerHTML = `
        <div style="background: white; border-radius: 12px; max-width: 700px; width: 90%; max-height: 85vh; overflow-y: auto; box-shadow: 0 20px 60px rgba(0,0,0,0.3);">
            <div style="padding: 20px 24px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; position: sticky; top: 0; background: white; z-index: 1;">
                <h4 style="margin: 0; font-size: 18px; font-weight: 600; color: #1e293b;">${docName}</h4>
                <button onclick="this.closest('div[style*=fixed]').remove()" style="background: none; border: none; font-size: 24px; color: #94a3b8; cursor: pointer; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;">&times;</button>
            </div>
            <div style="padding: 0;">
                ${content}
            </div>
            <div style="padding: 16px 24px; background: #f8fafc; border-top: 1px solid #e2e8f0; display: flex; gap: 12px; position: sticky; bottom: 0;">
                <button onclick="alert('Downloading ${docName}...')" style="flex: 1; padding: 12px; background: #3b82f6; color: white; border: none; border-radius: 8px; font-weight: 600; cursor: pointer;">
                    <i class="fa-solid fa-download"></i> Download PDF
                </button>
                <button onclick="this.closest('div[style*=fixed]').remove()" style="flex: 1; padding: 12px; background: white; color: #64748b; border: 1px solid #e2e8f0; border-radius: 8px; font-weight: 600; cursor: pointer;">
                    Close
                </button>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    modal.addEventListener('click', (e) => {
        if (e.target === modal) modal.remove();
    });
}

function downloadLabResult(testName) {
    alert(`Downloading ${testName} results as PDF...\n\nThis is a prototype feature. In production, this would download an actual PDF report.`);
}

function downloadDocument(docName) {
    alert(`Downloading ${docName} as PDF...\n\nThis is a prototype feature. In production, this would download an actual PDF document.`);
}


// Toggle visibility for password fields in profile
function toggleVisibility(inputId, button) {
    const input = document.getElementById(inputId);
    const icon = button.querySelector('i');
    
    if (input && icon) {
        if (input.type === 'password') {
            input.type = 'text';
            icon.className = 'fa-regular fa-eye-slash';
        } else {
            input.type = 'password';
            icon.className = 'fa-regular fa-eye';
        }
    }
}

// Make toggleVisibility available globally
window.toggleVisibility = toggleVisibility;

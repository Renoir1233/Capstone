// Wellness Program Management
// Handles creating, managing, and tracking wellness programs

const WellnessManager = {
    programs: [],
    currentProgram: null,

    // Initialize
    async init() {
        await this.loadPrograms();
    },

    // Load all wellness programs
    async loadPrograms(status = 'all') {
        try {
            const response = await fetch(`/api/wellness/programs?status=${status}`);
            if (!response.ok) throw new Error('Failed to load programs');
            
            this.programs = await response.json();
            this.renderProgramsTable();
        } catch (error) {
            console.error('Error loading programs:', error);
            alert('Failed to load wellness programs');
        }
    },

    // Render programs table
    renderProgramsTable() {
        const tbody = document.getElementById('wellness-programs-tbody');
        if (!tbody) return;

        if (this.programs.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;color:#94a3b8;padding:20px;">No wellness programs found</td></tr>';
            return;
        }

        tbody.innerHTML = this.programs.map(p => `
            <tr>
                <td style="font-weight: 600;">${this.escapeHtml(p.title)}</td>
                <td>${this.escapeHtml(p.category)}</td>
                <td>${this.escapeHtml(p.targetDepartment)}</td>
                <td>${this.formatDate(p.programDate)}</td>
                <td>${p.registeredCount} / ${p.participantCapacity} Employees</td>
                <td>${this.getStatusBadge(p.status)}</td>
                <td>
                    <button class="btn-table-action-view" onclick="WellnessManager.viewProgram(${p.id})">
                        <i class="fa-solid fa-eye"></i> View
                    </button>
                    ${p.status === 'Open' || p.status === 'Scheduled' ? `
                        <button class="btn-table-action-edit" onclick="WellnessManager.editProgram(${p.id})">
                            <i class="fa-solid fa-edit"></i>
                        </button>
                    ` : ''}
                </td>
            </tr>
        `).join('');
    },

    // Get status badge HTML
    getStatusBadge(status) {
        const badges = {
            'Scheduled': '<span class="status-pill active">Scheduled</span>',
            'Open': '<span class="status-pill active">Open</span>',
            'Ongoing': '<span class="status-pill" style="background:#fef3c7;color:#d97706;">Ongoing</span>',
            'Completed': '<span class="status-pill" style="background:#d1fae5;color:#059669;">Completed</span>',
            'Cancelled': '<span class="status-pill inactive">Cancelled</span>'
        };
        return badges[status] || status;
    },

    // View program details
    async viewProgram(id) {
        try {
            const response = await fetch(`/api/wellness/programs/${id}`);
            if (!response.ok) throw new Error('Failed to load program');
            
            const program = await response.json();
            this.currentProgram = program;
            this.showProgramDetailsModal(program);
        } catch (error) {
            console.error('Error loading program:', error);
            alert('Failed to load program details');
        }
    },

    // Show program details modal
    showProgramDetailsModal(program) {
        // Implementation depends on your modal structure
        console.log('Show program details', program);
        // You can populate a modal here with program details and registration list
    },

    // Open create/edit program modal
    openProgramModal(programId = null) {
        if (programId) {
            // Load and populate for edit
            this.viewProgram(programId);
        } else {
            // Clear form for new program
            document.getElementById('program-form')?.reset();
        }
        document.getElementById('program-modal')?.classList.add('active');
    },

    // Submit program (create or update)
    async submitProgram(programData) {
        try {
            const url = programData.id 
                ? `/api/wellness/programs/${programData.id}`
                : '/api/wellness/programs';
            
            const method = programData.id ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(programData)
            });

            if (!response.ok) throw new Error('Failed to save program');

            await this.loadPrograms();
            return true;
        } catch (error) {
            console.error('Error saving program:', error);
            throw error;
        }
    },

    // Register employee for program
    async registerEmployee(programId, employeeId) {
        try {
            const response = await fetch(`/api/wellness/programs/${programId}/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ employeeId })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Registration failed');
            }

            return true;
        } catch (error) {
            console.error('Error registering employee:', error);
            throw error;
        }
    },

    // Mark attendance
    async markAttendance(registrationId, attended, notes = '') {
        try {
            const response = await fetch(`/api/wellness/registrations/${registrationId}/attendance`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ attended, notes })
            });

            if (!response.ok) throw new Error('Failed to mark attendance');
            return true;
        } catch (error) {
            console.error('Error marking attendance:', error);
            throw error;
        }
    },

    // Complete program
    async completeProgram(programId) {
        if (!confirm('Are you sure you want to mark this program as completed?')) {
            return;
        }

        try {
            const response = await fetch(`/api/wellness/programs/${programId}/complete`, {
                method: 'POST'
            });

            if (!response.ok) throw new Error('Failed to complete program');
            
            await this.loadPrograms();
            alert('Program marked as completed!');
        } catch (error) {
            console.error('Error completing program:', error);
            alert('Error: ' + error.message);
        }
    },

    // Get program report
    async getProgramReport(programId) {
        try {
            const response = await fetch(`/api/wellness/programs/${programId}/report`);
            if (!response.ok) throw new Error('Failed to generate report');
            
            const report = await response.json();
            this.displayReport(report);
        } catch (error) {
            console.error('Error generating report:', error);
            alert('Failed to generate report');
        }
    },

    // Display report
    displayReport(report) {
        console.log('Program Report:', report);
        // Implementation for displaying the report in a modal or separate view
    },

    // Format date
    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    },

    // Escape HTML
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('wellness-programs-tbody')) {
        WellnessManager.init();
    }
});

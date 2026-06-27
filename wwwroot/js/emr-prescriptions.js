// EMR Prescription Management
// This file handles adding, updating, and managing prescriptions in the EMR view

const PrescriptionManager = {
    currentVisitId: null,
    currentVisitStatus: null,
    prescriptions: [],
    availableMedicines: [],

    // Initialize the prescription manager
    init(visitId, visitStatus = null) {
        this.currentVisitId = visitId;
        this.currentVisitStatus = visitStatus;
        this.loadAvailableMedicines();
        this.loadPrescriptions();
        this.attachEventListeners();
        this.updateUIBasedOnStatus();
    },

    // Update UI based on visit status
    updateUIBasedOnStatus() {
        const addButton = document.querySelector('[onclick*="showAddModal"]');
        const statusMessage = document.getElementById('prescription-status-message');

        if (this.currentVisitStatus === 'Waiting') {
            if (addButton) {
                addButton.disabled = true;
                addButton.title = 'Patient must be called in first';
            }
            if (statusMessage) {
                statusMessage.innerHTML = '<div class="alert alert-info"><i class="bi bi-info-circle"></i> Call in the patient to add prescriptions.</div>';
            }
        } else if (this.currentVisitStatus === 'Cleared') {
            if (addButton) {
                addButton.disabled = true;
                addButton.title = 'Visit already cleared';
            }
            if (statusMessage) {
                statusMessage.innerHTML = '<div class="alert alert-secondary"><i class="bi bi-check-circle"></i> Visit completed. Prescriptions are locked.</div>';
            }
        } else if (this.currentVisitStatus === 'WithDoctor') {
            if (addButton) {
                addButton.disabled = false;
            }
            if (statusMessage) {
                statusMessage.innerHTML = '';
            }
        }
    },

    // Load prescriptions for current visit
    async loadPrescriptions() {
        try {
            const response = await fetch(`/api/emr/visit/${this.currentVisitId}`);
            if (!response.ok) throw new Error('Failed to load visit data');
            
            const visitData = await response.json();
            this.prescriptions = visitData.prescriptions || [];
            
            // Update visit status if not set
            if (!this.currentVisitStatus && visitData.queueStatus) {
                this.currentVisitStatus = visitData.queueStatus;
                this.updateUIBasedOnStatus();
            }
            
            this.renderPrescriptions();
        } catch (error) {
            console.error('Error loading prescriptions:', error);
            alert('Failed to load prescriptions');
        }
    },

    // Load all available medicines from pharmacy stock
    async loadAvailableMedicines() {
        try {
            const response = await fetch('/api/emr/medicines');
            if (!response.ok) throw new Error('Failed to load medicines');
            
            this.availableMedicines = await response.json();
            this.populateMedicineDropdown();
        } catch (error) {
            console.error('Error loading medicines:', error);
            alert('Failed to load pharmacy stock');
        }
    },

    // Populate medicine dropdown with available stock
    populateMedicineDropdown() {
        const select = document.getElementById('medicine-select');
        if (!select) return;

        // Clear existing options except the first one
        select.innerHTML = '<option value="">-- Select Medicine --</option>';

        // Add medicines grouped by category
        const categories = {};
        this.availableMedicines.forEach(med => {
            const category = med.category || 'Other';
            if (!categories[category]) {
                categories[category] = [];
            }
            categories[category].push(med);
        });

        // Add options grouped by category
        Object.keys(categories).sort().forEach(category => {
            const optgroup = document.createElement('optgroup');
            optgroup.label = category;
            
            categories[category].forEach(med => {
                const option = document.createElement('option');
                option.value = med.id;
                option.textContent = `${med.name} (Stock: ${med.currentStock} ${med.unit})`;
                option.dataset.stock = med.currentStock;
                option.dataset.unit = med.unit;
                option.dataset.name = med.name;
                option.dataset.stockStatus = med.stockStatus;
                
                // Disable if out of stock
                if (med.currentStock <= 0) {
                    option.disabled = true;
                    option.textContent += ' - OUT OF STOCK';
                }
                
                optgroup.appendChild(option);
            });
            
            select.appendChild(optgroup);
        });
    },

    // Handle medicine selection change
    onMedicineSelected() {
        const select = document.getElementById('medicine-select');
        const selectedOption = select.options[select.selectedIndex];
        const quantityInput = document.getElementById('quantity');
        const stockInfo = document.getElementById('stock-info');

        if (selectedOption.value) {
            const stock = parseInt(selectedOption.dataset.stock);
            const unit = selectedOption.dataset.unit;
            const stockStatus = selectedOption.dataset.stockStatus;

            // Update quantity max attribute
            quantityInput.max = stock;
            
            // Show stock information
            const statusClass = stockStatus === 'Good' ? 'success' : stockStatus === 'Low' ? 'warning' : 'danger';
            stockInfo.innerHTML = `
                <small class="text-muted">
                    Available: <strong>${stock} ${unit}</strong>
                    <span class="badge bg-${statusClass}">${stockStatus}</span>
                </small>
            `;
            stockInfo.style.display = 'block';
        } else {
            quantityInput.max = '';
            stockInfo.style.display = 'none';
        }
    },

    // Add a new prescription
    async addPrescription(prescriptionData) {
        try {
            const response = await fetch(`/api/emr/visit/${this.currentVisitId}/prescriptions`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(prescriptionData)
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || 'Failed to add prescription');
            }

            const newPrescription = await response.json();
            this.prescriptions.push(newPrescription);
            this.renderPrescriptions();
            return newPrescription;
        } catch (error) {
            console.error('Error adding prescription:', error);
            throw error;
        }
    },

    // Update an existing prescription
    async updatePrescription(prescriptionId, prescriptionData) {
        try {
            const response = await fetch(`/api/emr/prescriptions/${prescriptionId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(prescriptionData)
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || 'Failed to update prescription');
            }

            const updatedPrescription = await response.json();
            const index = this.prescriptions.findIndex(p => p.id === prescriptionId);
            if (index !== -1) {
                this.prescriptions[index] = updatedPrescription;
                this.renderPrescriptions();
            }
            return updatedPrescription;
        } catch (error) {
            console.error('Error updating prescription:', error);
            throw error;
        }
    },

    // Delete a prescription
    async deletePrescription(prescriptionId) {
        if (!confirm('Are you sure you want to delete this prescription?')) {
            return;
        }

        try {
            const response = await fetch(`/api/emr/prescriptions/${prescriptionId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || 'Failed to delete prescription');
            }

            this.prescriptions = this.prescriptions.filter(p => p.id !== prescriptionId);
            this.renderPrescriptions();
        } catch (error) {
            console.error('Error deleting prescription:', error);
            alert('Error: ' + error.message);
        }
    },

    // Dispense a prescription
    async dispensePrescription(prescriptionId, dispensedBy) {
        try {
            const response = await fetch(`/api/emr/prescriptions/${prescriptionId}/dispense`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ dispensedBy })
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || 'Failed to dispense prescription');
            }

            const result = await response.json();
            const index = this.prescriptions.findIndex(p => p.id === prescriptionId);
            if (index !== -1) {
                this.prescriptions[index].isDispensed = true;
                this.prescriptions[index].dispensedDate = result.dispensedDate;
                this.renderPrescriptions();
            }
            return result;
        } catch (error) {
            console.error('Error dispensing prescription:', error);
            throw error;
        }
    },

    // Render prescriptions table
    renderPrescriptions() {
        const container = document.getElementById('prescriptions-list');
        if (!container) return;

        if (this.prescriptions.length === 0) {
            container.innerHTML = '<p class="text-muted">No prescriptions added yet.</p>';
            return;
        }

        const html = `
            <table class="table table-bordered">
                <thead>
                    <tr>
                        <th>Medicine</th>
                        <th>Quantity</th>
                        <th>Dosage</th>
                        <th>Frequency</th>
                        <th>Duration</th>
                        <th>Instructions</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${this.prescriptions.map(p => `
                        <tr>
                            <td>${this.escapeHtml(p.medicineName)}</td>
                            <td>${p.quantity}</td>
                            <td>${this.escapeHtml(p.dosage)}</td>
                            <td>${this.escapeHtml(p.frequency)}</td>
                            <td>${this.escapeHtml(p.duration)}</td>
                            <td>${this.escapeHtml(p.instructions)}</td>
                            <td>
                                ${p.isDispensed 
                                    ? '<span class="badge bg-success">Dispensed</span>' 
                                    : '<span class="badge bg-warning">Pending</span>'}
                            </td>
                            <td>
                                ${!p.isDispensed && this.currentVisitStatus !== 'Cleared' ? `
                                    <button class="btn btn-sm btn-primary" onclick="PrescriptionManager.showEditModal(${p.id})">
                                        Edit
                                    </button>
                                    <button class="btn btn-sm btn-danger" onclick="PrescriptionManager.deletePrescription(${p.id})">
                                        Delete
                                    </button>
                                ` : p.isDispensed ? `
                                    <small class="text-muted">
                                        ${new Date(p.dispensedDate).toLocaleDateString()}
                                    </small>
                                ` : `
                                    <small class="text-muted">Locked</small>
                                `}
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        `;

        container.innerHTML = html;
    },

    // Show modal to add new prescription
    showAddModal() {
        // Check if visit status allows adding prescriptions
        if (this.currentVisitStatus !== 'WithDoctor') {
            if (this.currentVisitStatus === 'Waiting') {
                alert('Please call in the patient first before adding prescriptions.');
            } else if (this.currentVisitStatus === 'Cleared') {
                alert('Cannot add prescriptions to a completed visit.');
            } else {
                alert('Cannot add prescriptions at this time.');
            }
            return;
        }

        // Reset form
        document.getElementById('prescription-form').reset();
        document.getElementById('prescription-modal-title').textContent = 'Add Prescription';
        document.getElementById('prescription-id').value = '';
        document.getElementById('stock-info').style.display = 'none';
        
        // Reload medicines to get latest stock
        this.loadAvailableMedicines();
        
        // Show modal (Bootstrap 5)
        const modal = new bootstrap.Modal(document.getElementById('prescription-modal'));
        modal.show();
    },

    // Show modal to edit existing prescription
    showEditModal(prescriptionId) {
        // Check if visit status allows editing
        if (this.currentVisitStatus === 'Cleared') {
            alert('Cannot edit prescriptions for a completed visit.');
            return;
        }

        const prescription = this.prescriptions.find(p => p.id === prescriptionId);
        if (!prescription) return;

        if (prescription.isDispensed) {
            alert('Cannot edit a dispensed prescription.');
            return;
        }

        document.getElementById('prescription-modal-title').textContent = 'Edit Prescription';
        document.getElementById('prescription-id').value = prescription.id;
        document.getElementById('medicine-select').value = prescription.medicineId;
        document.getElementById('quantity').value = prescription.quantity;
        document.getElementById('dosage').value = prescription.dosage;
        document.getElementById('frequency').value = prescription.frequency;
        document.getElementById('duration').value = prescription.duration;
        document.getElementById('instructions').value = prescription.instructions;

        // Trigger medicine selection to show stock info
        this.onMedicineSelected();

        const modal = new bootstrap.Modal(document.getElementById('prescription-modal'));
        modal.show();
    },

    // Handle prescription form submission
    async handlePrescriptionSubmit(event) {
        event.preventDefault();

        const prescriptionId = document.getElementById('prescription-id').value;
        const medicineSelect = document.getElementById('medicine-select');
        const selectedOption = medicineSelect.options[medicineSelect.selectedIndex];

        if (!selectedOption || !selectedOption.value) {
            alert('Please select a medicine');
            return;
        }

        const prescriptionData = {
            medicineId: parseInt(selectedOption.value),
            quantity: parseInt(document.getElementById('quantity').value),
            dosage: document.getElementById('dosage').value,
            frequency: document.getElementById('frequency').value,
            duration: document.getElementById('duration').value,
            instructions: document.getElementById('instructions').value,
            prescribedBy: document.getElementById('prescribed-by').value || 'Doctor'
        };

        // Validate quantity against stock
        const availableStock = parseInt(selectedOption.dataset.stock);
        if (prescriptionData.quantity > availableStock) {
            alert(`Insufficient stock. Only ${availableStock} ${selectedOption.dataset.unit} available.`);
            return;
        }

        try {
            if (prescriptionId) {
                await this.updatePrescription(parseInt(prescriptionId), prescriptionData);
                alert('Prescription updated successfully');
            } else {
                await this.addPrescription(prescriptionData);
                alert('Prescription added successfully');
            }

            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('prescription-modal'));
            modal.hide();
        } catch (error) {
            alert('Error: ' + error.message);
        }
    },

    // Attach event listeners
    attachEventListeners() {
        const form = document.getElementById('prescription-form');
        if (form) {
            form.addEventListener('submit', (e) => this.handlePrescriptionSubmit(e));
        }

        const medicineSelect = document.getElementById('medicine-select');
        if (medicineSelect) {
            medicineSelect.addEventListener('change', () => this.onMedicineSelected());
        }
    },

    // Escape HTML to prevent XSS
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

// Auto-initialize if visitId is available
document.addEventListener('DOMContentLoaded', () => {
    const visitIdElement = document.getElementById('current-visit-id');
    const visitStatusElement = document.getElementById('current-visit-status');
    
    if (visitIdElement && visitIdElement.value) {
        const visitStatus = visitStatusElement ? visitStatusElement.value : null;
        PrescriptionManager.init(parseInt(visitIdElement.value), visitStatus);
    }
});

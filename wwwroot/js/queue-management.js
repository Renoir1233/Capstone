// Queue Management with Prescription Support
// This file handles the queue workflow including adding prescriptions during log visit

const QueueManager = {
    availableMedicines: [],
    prescriptions: [],

    // Initialize queue manager
    async init() {
        await this.loadAvailableMedicines();
    },

    // Load all available medicines from pharmacy
    async loadAvailableMedicines() {
        try {
            const response = await fetch('/api/emr/medicines');
            if (!response.ok) throw new Error('Failed to load medicines');
            
            this.availableMedicines = await response.json();
        } catch (error) {
            console.error('Error loading medicines:', error);
            alert('Failed to load pharmacy stock');
        }
    },

    // Populate medicine dropdown
    populateMedicineDropdown(selectElementId) {
        const select = document.getElementById(selectElementId);
        if (!select) return;

        select.innerHTML = '<option value="">-- Select Medicine --</option>';

        // Group medicines by category
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
                
                if (med.currentStock <= 0) {
                    option.disabled = true;
                    option.textContent += ' - OUT OF STOCK';
                }
                
                optgroup.appendChild(option);
            });
            
            select.appendChild(optgroup);
        });
    },

    // Add prescription row to the list
    addPrescriptionRow() {
        const medicineSelect = document.getElementById('medicine-select-logvisit');
        const quantityInput = document.getElementById('quantity-logvisit');
        const dosageInput = document.getElementById('dosage-logvisit');
        const frequencySelect = document.getElementById('frequency-logvisit');
        const durationInput = document.getElementById('duration-logvisit');
        const instructionsInput = document.getElementById('instructions-logvisit');

        const selectedOption = medicineSelect.options[medicineSelect.selectedIndex];
        
        if (!selectedOption || !selectedOption.value) {
            alert('Please select a medicine');
            return;
        }

        const quantity = parseInt(quantityInput.value);
        const stock = parseInt(selectedOption.dataset.stock);

        if (quantity > stock) {
            alert(`Insufficient stock. Only ${stock} ${selectedOption.dataset.unit} available.`);
            return;
        }

        if (!dosageInput.value || !frequencySelect.value || !durationInput.value) {
            alert('Please fill in all required fields');
            return;
        }

        const prescription = {
            medicineId: parseInt(selectedOption.value),
            medicineName: selectedOption.dataset.name,
            quantity: quantity,
            dosage: dosageInput.value,
            frequency: frequencySelect.value,
            duration: durationInput.value,
            instructions: instructionsInput.value,
            prescribedBy: document.getElementById('prescribed-by-logvisit')?.value || 'Doctor'
        };

        this.prescriptions.push(prescription);
        this.renderPrescriptionList();

        // Clear inputs
        medicineSelect.value = '';
        quantityInput.value = '';
        dosageInput.value = '';
        frequencySelect.value = '';
        durationInput.value = '';
        instructionsInput.value = '';
    },

    // Remove prescription from list
    removePrescription(index) {
        this.prescriptions.splice(index, 1);
        this.renderPrescriptionList();
    },

    // Render prescription list
    renderPrescriptionList() {
        const container = document.getElementById('prescription-list-logvisit');
        if (!container) return;

        if (this.prescriptions.length === 0) {
            container.innerHTML = '<p class="text-muted small" style="font-size:11px;margin:0;">No prescriptions added yet.</p>';
            return;
        }

        const html = `
            <div class="table-responsive">
                <table class="table table-sm table-bordered" style="font-size:11px;margin:0;">
                    <thead style="background:#f8fafc;">
                        <tr>
                            <th style="padding:6px 8px;">Medicine</th>
                            <th style="padding:6px 8px;">Qty</th>
                            <th style="padding:6px 8px;">Dosage</th>
                            <th style="padding:6px 8px;">Frequency</th>
                            <th style="padding:6px 8px;">Duration</th>
                            <th style="padding:6px 8px;width:60px;">Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${this.prescriptions.map((p, index) => `
                            <tr>
                                <td style="padding:6px 8px;">${this.escapeHtml(p.medicineName)}</td>
                                <td style="padding:6px 8px;">${p.quantity}</td>
                                <td style="padding:6px 8px;">${this.escapeHtml(p.dosage)}</td>
                                <td style="padding:6px 8px;">${this.escapeHtml(p.frequency)}</td>
                                <td style="padding:6px 8px;">${this.escapeHtml(p.duration)}</td>
                                <td style="padding:6px 8px;">
                                    <button type="button" class="btn btn-sm btn-danger" onclick="QueueManager.removePrescription(${index})" style="font-size:10px;padding:2px 8px;">
                                        Remove
                                    </button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;

        container.innerHTML = html;
    },

    // Submit log visit with prescriptions
    async submitLogVisit(visitId) {
        const outcomeRadio = document.querySelector('input[name="visit-outcome"]:checked');
        const outcome = outcomeRadio ? outcomeRadio.value : 'Fit to Work';
        const notes = document.getElementById('log-visit-notes')?.value.trim();
        const fullOutcome = notes ? `${outcome} — ${notes}` : outcome;

        const payload = {
            outcome: fullOutcome,
            prescriptions: this.prescriptions.map(p => ({
                medicineId: p.medicineId,
                quantity: p.quantity,
                dosage: p.dosage,
                frequency: p.frequency,
                duration: p.duration,
                instructions: p.instructions,
                prescribedBy: p.prescribedBy
            }))
        };

        try {
            const response = await fetch(`/api/queue/${visitId}/logvisit`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to log visit');
            }

            // Clear prescriptions
            this.prescriptions = [];
            
            return true;
        } catch (error) {
            console.error('Error logging visit:', error);
            throw error;
        }
    },

    // Escape HTML to prevent XSS
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    QueueManager.init();
});

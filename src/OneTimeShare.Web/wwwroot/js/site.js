// Write your JavaScript code here.

// Global utility functions
window.OneTimeShare = {
    // Format bytes to human readable format
    formatBytes: function(bytes, decimals = 2) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const dm = decimals < 0 ? 0 : decimals;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
    },

    // Copy text to clipboard
    copyToClipboard: function(text) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(text);
        } else {
            // Fallback for older browsers
            const textArea = document.createElement("textarea");
            textArea.value = text;
            textArea.style.position = "fixed";
            textArea.style.left = "-999999px";
            textArea.style.top = "-999999px";
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            return new Promise((resolve, reject) => {
                try {
                    document.execCommand('copy');
                    textArea.remove();
                    resolve();
                } catch (err) {
                    textArea.remove();
                    reject(err);
                }
            });
        }
    },

    // Show temporary success message
    showSuccessMessage: function(element, message, duration = 2000) {
        const originalContent = element.innerHTML;
        const originalClasses = element.className;
        
        element.innerHTML = message;
        element.className = element.className.replace(/btn-\w+/g, 'btn-success');
        
        setTimeout(() => {
            element.innerHTML = originalContent;
            element.className = originalClasses;
        }, duration);
    }
};

// Initialize Bootstrap tooltips if present
document.addEventListener('DOMContentLoaded', function() {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
});
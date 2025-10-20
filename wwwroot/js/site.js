// Site JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Close alerts automatically after 5 seconds
    var alerts = document.querySelectorAll('.alert');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});

// Confirm delete
function confirmDelete() {
    return confirm('Are you sure you want to delete this item?');
}

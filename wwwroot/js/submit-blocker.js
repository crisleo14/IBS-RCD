$(document).ready(function () {
    let isSubmitting = false;

    $('form').on('submit', function (e) {
        if (isSubmitting) {
            e.preventDefault(); // Prevent duplicate submissions
            return;
        }

        const submitButton = $(this).find('button[type="submit"]');
        isSubmitting = true;

        // Disable button and show processing text
        submitButton.prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...');

        // Store submission state in localStorage to persist across navigation
        localStorage.setItem('isSubmitting', 'true');
    });

    // On page load, reset button if it was stuck disabled
    $(window).on('pageshow', function () {
        localStorage.removeItem('isSubmitting');
        $('button[type="submit"]').prop('disabled', false).html('Create');
    });

    // Handle browser back/refresh case
    if (localStorage.getItem('isSubmitting') === 'true') {
        $('button[type="submit"]').prop('disabled', false).html('Create');
        localStorage.removeItem('isSubmitting');
    }
});
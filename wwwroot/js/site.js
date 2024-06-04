// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
//$(document).ready(function () {
//    $('select').each(function () {
//        $(this).select2({
//            placeholder: "Select an option",
//            allowClear: true
//        });
//    });
//});

//$(document).ready(function () {
//    $('.js-multiple-not-retain-value').select2({
//        placeholder: "Select an option",
//        width: 'resolve'
//    });
//    $('.js-multiple-not-retain-value').val([]).trigger('change');
//});

$(document).ready(function () {
    $('.js-multiple').select2({
        placeholder: "Select an option",
        allowClear: true,
        width: 'resolve'
    });
});

$(document).ready(function () {
    $('.js-select2').select2({
        placeholder: "Select an option",
        allowClear: true,
        width: 'resolve'
    });
});
$(document).ready(function () {
    // Initialize Select2
    $('.js-not-retain-value-select2').select2({
        placeholder: "Select an option",
        allowClear: true,
        width: 'resolve'
    });

    // Clear the selected value when the page is loaded
    $('.js-not-retain-value-select2').val(null).trigger('change');
});


//sorting and paginatio with search can used in all modules
$(document).ready(function () {
    $('#myTable').DataTable();
});

//myOwnTable in Print Invoice report
$(document).ready(function () {
    var table = $('#myOwnTable').DataTable({
        "stateSave": true,
        "autoWidth": false
    });
    table.order([0, 'desc']).draw();
});


//Money Input formatting
$(document).ready(function () {
    // Store the initial input value for each moneyInput field
    $(".moneyInput").each(function () {
        $(this).data("initialValue", $(this).val());
    });

    // Attach a focus event listener to the moneyInput fields
    $(".moneyInput").on("focus", function () {
        // Get the initial value
        var initialValue = $(this).data("initialValue");

        // If the current value is the initial value, clear it
        if (initialValue === '0.00' || initialValue === '0') {
            $(this).val('');
        }
    });

    // Attach a blur event listener to the moneyInput fields
    $(".moneyInput").on("blur", function () {
        // Get the initial value
        var initialValue = $(this).data("initialValue");

        // If the current value is empty or only whitespace, set it back to the initial value
        if (!$(this).val().trim()) {
            $(this).val(initialValue);
        }
    });

    // Attach an input event listener to the moneyInput field
    $(".moneyInput").on("input", function () {
        // Get the current value of the input
        var inputValue = $(this).val();

        // If the input starts with a dot, add '0' before it
        if (inputValue.charAt(0) === '.') {
            inputValue = '0' + inputValue;
        }

        // Remove non-numeric characters except for the decimal point
        var numericValue = inputValue.replace(/[^\d.]/g, '');

        // Format the numeric value with commas and two decimal places
        var formattedValue = numberWithCommasAndDecimals(numericValue);

        // Set the formatted value back to the input field
        $(this).val(formattedValue);
    });

    // Function to add commas and two decimal places to a number
    function numberWithCommasAndDecimals(number) {
        // Split the number into integer and decimal parts
        var parts = number.split('.');
        var integerPart = parts[0];
        var decimalPart = parts.length > 1 ? '.' + parts[1] : '';

        // Add commas to the integer part
        var numberWithCommas = integerPart.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");

        // Combine the integer and decimal parts
        return numberWithCommas + decimalPart;
    }
});

// Dynamic date to in books
document.addEventListener('DOMContentLoaded', function () {
    var dateFromInput = document.getElementById('DateFrom');
    var dateToInput = document.getElementById('DateTo');

    // Add an event listener to DateFrom input
    dateFromInput?.addEventListener('change', function () {
        // Set DateTo input value to DateFrom input value
        dateToInput.value = dateFromInput.value;
    });
});

document.addEventListener('DOMContentLoaded', function () {
    function formatNumberWithCommas(value) {
        return value.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
    }

    function removeCommas(value) {
        return value.replace(/,/g, '');
    }

    function handleFocus(event) {
        event.target.value = removeCommas(event.target.value);
    }

    function handleBlur(event) {
        const inputValue = parseFloat(removeCommas(event.target.value));
        if (isNaN(inputValue) || inputValue <= 0) {
            event.target.classList.add('error');
            event.target.nextElementSibling.textContent = 'Value must be greater than zero.';
        } else {
            event.target.classList.remove('error');
            event.target.nextElementSibling.textContent = '';
            event.target.value = formatNumberWithCommas(event.target.value);
        }
    }

    function handleSubmit(event) {
        document.querySelectorAll('.money').forEach(function (element) {
            element.value = removeCommas(element.value);
        });
    }

    document.querySelectorAll('.money').forEach(function (element) {
        element.addEventListener('focus', handleFocus);
        element.addEventListener('blur', handleBlur);
    });

    document.querySelector('form').addEventListener('submit', handleSubmit);
});


// Get the current date in the format "YYYY-MM-DD" (required for the date input)
var currentDate = new Date().toISOString().slice(0, 10);

// Set the default value of the input field
document.getElementById("TransactionDate").value = currentDate;
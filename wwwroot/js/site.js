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

$(document).ready(function () {
    $('.js-select2').select2({
        placeholder: "Select an option",
        allowClear: true,
        width: 'resolve'
    });
});

//sorting and paginatio with search can used in all modules
$(document).ready(function () {
    $('#myTable').DataTable();
});

//myOwnTable in Print Invoice report
$(document).ready(function () {
    var currentPage = 0; // Store the current page number

    var table = $('#myOwnTable').DataTable({
        "order": [[0, "desc"]],
        "rowId": "pk",
        "stateSave": true,
        "drawCallback": function (settings) {
            // Get the row identifiers of the rows that are currently visible
            var visibleRows = table.rows({ page: 'current' }).data().toArray();
            table.state.save();
            // If visibleRows is empty, it means the table is empty (no rows)
            if (visibleRows.length === 0) {
                currentPage = 0; // Reset current page when there are no rows
            } else {
                // Find the index of the first visible row's "pk" value in the dataset
                var indexOfFirstVisibleRow = table.rows({ page: 'current' }).indexes()[0];
                var firstVisibleRowData = table.row(indexOfFirstVisibleRow).data();
                currentPage = visibleRows.indexOf(firstVisibleRowData);
                table.state.load();
            }
        }
    });

    // Load your initial data here (e.g., using table.clear().rows.add() and table.draw())

    // You can also set an event handler to handle page changes
    table.on('page.dt', function () {
        currentPage = table.page.info().page;
    });
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

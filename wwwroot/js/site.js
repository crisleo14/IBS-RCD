// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $('select').each(function () {
        $(this).select2({
            placeholder: "Select an option",
            allowClear: true
        });
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
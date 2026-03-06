// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


// Prevent card click from firing when clicking buttons or links inside the card on the catalog page
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".card button, .card a.btn").forEach(el => {
        el.addEventListener("click", function (event) {
            event.stopPropagation();
        });
    });
});

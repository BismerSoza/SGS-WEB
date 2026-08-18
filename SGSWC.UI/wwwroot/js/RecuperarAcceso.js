// RecuperarAcceso.js

$("#FormRecuperarAcceso").on("submit", function (e) {
    e.preventDefault();

    const translations = window.translations || {
        successTitle: 'Éxito',
        errorTitle: 'Error'
    };

    $.ajax({
        url: '/Home/RecuperarAcceso',
        type: "POST",
        data: {
            correo: $("#correo").val()
        },
        beforeSend: function () {
            $("#btn-procesar").prop("disabled", true);
        },
        complete: function () {
            $("#btn-procesar").prop("disabled", false);
        },
        success: function (response) {
            Swal.fire({
                icon: "success",
                title: translations.successTitle,
                text: response
            }).then(() => {
                window.location.href = '/Home/Index';
            });
        },
        error: function (response) {
            Swal.fire({
                icon: "error",
                title: translations.errorTitle,
                text: response.responseText
            }).then(() => {
                window.location.href = '/Home/Index';
            });
        }
    });
});
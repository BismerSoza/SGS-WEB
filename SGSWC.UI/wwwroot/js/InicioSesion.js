$(function () {
    $("#FormInicioSesion").validate({
        rules: {
            CorreoElectronico: {
                required: true
            },
            Contrasenna: {
                required: true
            },
        },
        messages: {
            CorreoElectronico: {
                required: "* Requerido"
            },
            Contrasenna: {
                required: "* Requerido"
            }
        }
    });
});
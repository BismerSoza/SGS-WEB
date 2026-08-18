function ConsultarNombre() {

    let identificacion = $("#Identificacion").val();
    $("#Nombre").val("");

    if (identificacion.length >= 9) {

        $.ajax({
            type: 'GET',
            url: 'https://apis.gometa.org/cedulas/' + identificacion,
            dataType: 'json',
            success: function (data) {
                if (data.resultcount > 0) {
                    $("#Nombre").val(data.nombre);
                }
            }
        });

    }
}

$(function () {
    $("#FormRegistro").validate({
        rules: {
            Identificacion: {
                required: true
            },
            Nombre: {
                required: true
            },
            CorreoElectronico: {
                required: true
            },
            Contrasenna: {
                required: true
            },
        },
        messages: {
            Identificacion: {
                required: "* Requerido"
            },
            Nombre: {
                required: "* Requerido"
            },
            CorreoElectronico: {
                required: "* Requerido"
            },
            Contrasenna: {
                required: "* Requerido"
            }
        },
        submitHandler: function (form) {
            // Validación adicional de contraseña segura
            var pass = $("#Contrasenna").val();
            var especiales = /[!@#$%^&*()_\-=

            \[\]

            { }|; ':",.<>?]/;

            if (pass.length < 8) {
                $("#errorPassword").text("La contraseña debe tener mínimo 8 caracteres.");
                $("#errorPassword").show();
                return false;
            } else if (!especiales.test(pass)) {
                $("#errorPassword").text("La contraseña debe incluir al menos un carácter especial (!@#$%).");
                $("#errorPassword").show();
                return false;
            } else {
                $("#errorPassword").hide();
                form.submit();
            }
        }
    });
});

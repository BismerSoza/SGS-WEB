// Mostrar / ocultar contraseña
function togglePassword(inputId, iconId) {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(iconId);

    if (input.type === "password") {
        input.type = "text";
        icon.classList.replace("bx-hide", "bx-show");
    } else {
        input.type = "password";
        icon.classList.replace("bx-show", "bx-hide");
    }
}

// Validar coincidencia y requisitos en tiempo real
function validarCoincidencia() {
    const nueva = document.getElementById("nuevaContrasenia").value;
    const confirmar = document.getElementById("confirmarContrasenia").value;
    const mensaje = document.getElementById("mensajeCoincidencia");
    const btnGuardar = document.getElementById("btnGuardarContrasenia");

    // Validar requisitos
    const cumpleLongitud = nueva.length >= 8;
    const cumpleMayuscula = /[A-Z]/.test(nueva);
    const cumpleMinuscula = /[a-z]/.test(nueva);
    const cumpleEspecial = /[!@#$%^&*()_+\-=\[\]{}|;':",./<>?]/.test(nueva);

    // Colorear los requisitos dinámicamente
    document.getElementById("reqLongitud").style.color = cumpleLongitud ? "green" : "gray";
    document.getElementById("reqEspecial").style.color = cumpleEspecial ? "green" : "gray";
    document.getElementById("reqMayus").style.color = cumpleMayuscula ? "green" : "gray";
    document.getElementById("reqMinus").style.color = cumpleMinuscula ? "green" : "gray";

    // Validar coincidencia
    const coinciden = nueva === confirmar && confirmar !== "";

    if (confirmar === "") {
        mensaje.textContent = "";
    } else if (coinciden) {
        mensaje.textContent = "✔ Las contraseñas coinciden";
        mensaje.style.color = "green";
    } else {
        mensaje.textContent = "✖ Las contraseñas no coinciden";
        mensaje.style.color = "red";
    }

    // Habilitar el botón solo si todo está correcto
    btnGuardar.disabled = !(coinciden && cumpleLongitud && cumpleEspecial && cumpleMayuscula && cumpleMinuscula);
}

// Limpiar el modal al cerrarse
document.getElementById("modalCambiarContrasenia")
    .addEventListener("hidden.bs.modal", () => {
        document.getElementById("formCambiarContrasenia").reset();
        document.getElementById("mensajeCoincidencia").textContent = "";
        document.getElementById("btnGuardarContrasenia").disabled = true;
        document.getElementById("reqLongitud").style.color = "gray";
        document.getElementById("reqEspecial").style.color = "gray";
        document.getElementById("reqMayus").style.color = "gray";
        document.getElementById("reqMinus").style.color = "gray";

        // Restaurar todos los campos a tipo password
        ["contraseniaActual", "nuevaContrasenia", "confirmarContrasenia"].forEach(id => {
            document.getElementById(id).type = "password";
        });
        ["iconActual", "iconNueva", "iconConfirmar"].forEach(id => {
            const icon = document.getElementById(id);
            icon.classList.remove("bx-show");
            icon.classList.add("bx-hide");
        });
    });

//-----------------------------

$("#formCambiarContrasenia").on("submit", function (e) {
    e.preventDefault();
    $.ajax({
        url: '/Home/CambiarContrasenia',
        type: "PUT",
        data: {
            idUsuario: $("input[name='idUsuario']").val(),
            contraseniaActual: $("#contraseniaActual").val(),
            nuevaContrasenia: $("#nuevaContrasenia").val()
        },
        beforeSend: function () {
            $("#btnGuardar").prop("disabled", true);
        },
        complete: function () {
            $("#btnGuardar").prop("disabled", false);
        },
        success: function (response) {
            $("#modalCambiarContrasenia").modal("hide");
            Swal.fire({
                icon: "success",
                title: "Éxito",
                text: response
            });
        },
        error: function (jqXHR) {   // ← fix: declarar jqXHR como parámetro
            $("#modalCambiarContrasenia").modal("hide");
            Swal.fire({
                icon: "error",
                title: "Error",
                text: jqXHR.responseText || "No se pudo actualizar la contraseña"
            });
        }
    });
});

//------------------------------
// MODAL FORZAR CAMBIO DE CONTRASEÑA
//-------------------------------

function validarCoincidenciaForzar() {
    const nueva = document.getElementById("nuevaContraseniaForzar").value;
    const confirmar = document.getElementById("confirmarContraseniaForzar").value;
    const mensaje = document.getElementById("mensajeCoincidenciaForzar");
    const btnGuardar = document.getElementById("btnGuardarForzar");

    const cumpleLongitud = nueva.length >= 8 && nueva.length <= 12;
    const cumpleMayuscula = /[A-Z]/.test(nueva);
    const cumpleMinuscula = /[a-z]/.test(nueva);
    const cumpleEspecial = /[!@#$%^&*()_+\-=\[\]{}|;':",./<>?]/.test(nueva);

    document.getElementById("reqLongitudForzar").style.color = cumpleLongitud ? "green" : "gray";
    document.getElementById("reqEspecialForzar").style.color = cumpleEspecial ? "green" : "gray";
    document.getElementById("reqMayusForzar").style.color = cumpleMayuscula ? "green" : "gray";
    document.getElementById("reqMinusForzar").style.color = cumpleMinuscula ? "green" : "gray";

    const coinciden = nueva === confirmar && confirmar !== "";

    if (confirmar === "") {
        mensaje.textContent = "";
    } else if (coinciden) {
        mensaje.textContent = "✔ Las contraseñas coinciden";
        mensaje.style.color = "green";
    } else {
        mensaje.textContent = "✖ Las contraseñas no coinciden";
        mensaje.style.color = "red";
    }

    btnGuardar.disabled = !(coinciden &&
        cumpleLongitud &&
        cumpleEspecial &&
        cumpleMayuscula &&
        cumpleMinuscula);
}

document.getElementById("modalForzarContrasenia")
    .addEventListener("hidden.bs.modal", () => {

        document.getElementById("formForzarContrasenia").reset();
        document.getElementById("mensajeCoincidenciaForzar").textContent = "";
        document.getElementById("btnGuardarForzar").disabled = true;

        document.getElementById("reqLongitudForzar").style.color = "gray";
        document.getElementById("reqEspecialForzar").style.color = "gray";
        document.getElementById("reqMayusForzar").style.color = "gray";
        document.getElementById("reqMinusForzar").style.color = "gray";

        [
            "nuevaContraseniaForzar",
            "confirmarContraseniaForzar"
        ].forEach(id => {
            document.getElementById(id).type = "password";
        });

        [
            "iconNuevaForzar",
            "iconConfirmarForzar"
        ].forEach(id => {
            const icon = document.getElementById(id);
            icon.classList.remove("bx-show");
            icon.classList.add("bx-hide");
        });
    });

$("#formForzarContrasenia").on("submit", function (e) {
    e.preventDefault();

    $.ajax({
        url: '/Home/ActualizarContraseniaForzada',
        type: "PUT",
        data: {
            idUsuario: $("input[name='idUsuario']").val(),
            nuevaContrasenia: $("#nuevaContraseniaForzar").val()
        },
        beforeSend: function () {
            $("#btnGuardarForzar").prop("disabled", true);
        },
        complete: function () {
            $("#btnGuardarForzar").prop("disabled", false);
        },
        success: function (response) {

           /* const modal = bootstrap.Modal.getInstance(
                document.getElementById("modalForzarContrasenia")
            );

            if (modal) {
                modal.hide();
            }*/

            $("#modalForzarContrasenia").modal("hide");

            Swal.fire({
                icon: "success",
                title: "Éxito",
                text: response
            }).then(() => {
                location.reload();
            });
        },
        error: function (jqXHR) {
            $("#modalForzarContrasenia").modal("hide");
            Swal.fire({
                icon: "error",
                title: "Error",
                text: jqXHR.responseText || "No se pudo actualizar la contraseña"
            });
        }
    });
});
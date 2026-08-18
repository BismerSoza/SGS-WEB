// GestionUsuarios.js

$(document).ready(function () {

    const t = window.translations || {};

    // ── DataTable con idioma dinámico ──────────────────────────────
    const dataTableLang = t.dataTableLanguageUrl || '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json';
    $('#tablaUsuarios').DataTable({
        language: {
            url: dataTableLang
        },
        columnDefs: [
            { orderable: false, targets: 5 } // columna del switch
        ]
    });

    let toggleActual = null;
    let nuevoEstado = false;
    let idUsuario = 0;
    let nombreUsuario = '';

    // ── Toggle switch ────────────────────────────────────────────────
    $(document).on('change', '.toggle-estado', function () {

        toggleActual = this;
        nuevoEstado = $(this).is(':checked');
        idUsuario = $(this).data('id');
        nombreUsuario = $(this).data('nombre');

        // Revertir visualmente hasta confirmar
        $(this).prop('checked', !nuevoEstado);

        const modalTitulo = $('#modalTitulo');
        const modalCuerpo = $('#modalCuerpo');
        const btnConfirmar = $('#btnConfirmar');

        if (nuevoEstado) {
            modalTitulo.text(t.activateUser);
            const msg = (t.confirmChangeStatus || '¿Está seguro que desea {0} al usuario "{1}"?')
                .replace('{0}', t.activate.toLowerCase())
                .replace('{1}', nombreUsuario);
            modalCuerpo.text(msg);
            modalCuerpo.removeClass('text-danger').addClass('text-success');
            btnConfirmar.removeClass('btn-danger').addClass('btn-success');
            btnConfirmar.text(t.activate);
        } else {
            modalTitulo.text(t.deactivateUser);
            const msg = (t.confirmChangeStatus || '¿Está seguro que desea {0} al usuario "{1}"?')
                .replace('{0}', t.deactivate.toLowerCase())
                .replace('{1}', nombreUsuario);
            modalCuerpo.text(msg);
            modalCuerpo.removeClass('text-success').addClass('text-danger');
            btnConfirmar.removeClass('btn-success').addClass('btn-danger');
            btnConfirmar.text(t.deactivate);
        }

        new bootstrap.Modal(document.getElementById('modalConfirmar')).show();
    });

    // ── Confirmar cambio de estado ───────────────────────────────────
    $('#btnConfirmar').on('click', function () {
        $.ajax({
            url: '/CRM/CambiarEstadoUsuario',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                id_usuario: idUsuario,
                activo: nuevoEstado
            }),
            headers: {
                'RequestVerificationToken':
                    $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (respuesta) {
                if (respuesta.exito) {
                    $(toggleActual).prop('checked', nuevoEstado);
                    $(toggleActual).attr('title',
                        nuevoEstado ? t.deactivateTooltip : t.activateTooltip
                    );
                    const mensaje = nuevoEstado
                        ? (t.userActivated || 'Usuario "{0}" activado correctamente.').replace('{0}', nombreUsuario)
                        : (t.userDeactivated || 'Usuario "{0}" desactivado correctamente.').replace('{0}', nombreUsuario);
                    mostrarToast(mensaje, nuevoEstado ? 'success' : 'warning');
                } else {
                    mostrarToast(t.errorChangeStatus || 'No se pudo cambiar el estado. Intentá de nuevo.', 'danger');
                }
            },
            error: function () {
                mostrarToast(t.connectionError || 'Error de conexión con el servidor.', 'danger');
            },
            complete: function () {
                bootstrap.Modal.getInstance(
                    document.getElementById('modalConfirmar')
                ).hide();
            }
        });
    });

    // ── Cancelar ─────────────────────────────────────────────────────
    $('#btnCancelar').on('click', function () {
        toggleActual = null;
    });

});

// ── Toast ────────────────────────────────────────────────────────────
function mostrarToast(mensaje, tipo) {
    const toastHtml = `
        <div class="toast align-items-center text-bg-${tipo} border-0 show mb-2"
             role="alert" style="min-width:280px">
            <div class="d-flex">
                <div class="toast-body fw-bold">${mensaje}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto"
                        data-bs-dismiss="toast"></button>
            </div>
        </div>`;

    let contenedor = document.getElementById('toastContenedor');
    if (!contenedor) {
        contenedor = document.createElement('div');
        contenedor.id = 'toastContenedor';
        contenedor.style.cssText = 'position:fixed;bottom:1.5rem;right:1.5rem;z-index:9999;';
        document.body.appendChild(contenedor);
    }

    contenedor.insertAdjacentHTML('beforeend', toastHtml);

    setTimeout(() => {
        const toasts = contenedor.querySelectorAll('.toast');
        if (toasts.length) toasts[0].remove();
    }, 4000);
}
// GestionUsuarios.js

$(document).ready(function () {
    console.log('✅ GestionUsuarios.js cargado');

    const t = window.translations || {};

    // ── Inicializar DataTable solo una vez ──────────────────────────
    if (!$.fn.DataTable.isDataTable('#tablaUsuarios')) {
        const dataTableLang = t.dataTableLanguageUrl || '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json';
        $('#tablaUsuarios').DataTable({
            language: {
                url: dataTableLang
            },
            columnDefs: [
                { orderable: false, targets: 5 } // columna del switch
            ],
            order: [[0, 'asc']],
            pageLength: 10
        });
        console.log('✅ DataTable inicializada');
    } else {
        console.log('ℹ️ DataTable ya estaba inicializada');
    }

    let toggleActual = null;
    let nuevoEstado = false;
    let idUsuario = 0;
    let nombreUsuario = '';

    // ── Toggle switch ────────────────────────────────────────────────
    $(document).on('change', '.toggle-estado', function () {
        console.log('🔄 Toggle clickeado');

        toggleActual = this;
        nuevoEstado = $(this).is(':checked');
        idUsuario = $(this).data('id');
        nombreUsuario = $(this).data('nombre');

        console.log('📌 Usuario:', idUsuario, nombreUsuario, 'Nuevo estado:', nuevoEstado);

        // Revertir visualmente hasta confirmar
        $(this).prop('checked', !nuevoEstado);

        const modalTitulo = $('#modalTitulo');
        const modalCuerpo = $('#modalCuerpo');
        const btnConfirmar = $('#btnConfirmar');

        if (nuevoEstado) {
            modalTitulo.text(t.activateUser || 'Activar usuario');
            modalCuerpo.text(`¿Está seguro que desea activar al usuario "${nombreUsuario}"?`);
            modalCuerpo.removeClass('text-danger').addClass('text-success');
            btnConfirmar.removeClass('btn-danger').addClass('btn-success');
            btnConfirmar.text(t.activate || 'Activar');
        } else {
            modalTitulo.text(t.deactivateUser || 'Desactivar usuario');
            modalCuerpo.text(`¿Está seguro que desea desactivar al usuario "${nombreUsuario}"?`);
            modalCuerpo.removeClass('text-success').addClass('text-danger');
            btnConfirmar.removeClass('btn-success').addClass('btn-danger');
            btnConfirmar.text(t.deactivate || 'Desactivar');
        }

        new bootstrap.Modal(document.getElementById('modalConfirmar')).show();
        console.log('✅ Modal mostrado');
    });

    // ── Confirmar cambio de estado ───────────────────────────────────
    $('#btnConfirmar').on('click', function () {
        console.log('🔵 Botón Confirmar clickeado');

        $(this).prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status"></span> Procesando...');

        var data = {
            Id_Usuario: idUsuario,
            Activo: nuevoEstado
        };

        console.log('📤 Enviando a /CRM/CambiarEstadoUsuario:', data);

        $.ajax({
            url: '/CRM/CambiarEstadoUsuario',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (respuesta) {
                console.log('✅ Respuesta recibida:', respuesta);
                if (respuesta.exito) {
                    $(toggleActual).prop('checked', nuevoEstado);
                    $(toggleActual).attr('title',
                        nuevoEstado ? (t.deactivateTooltip || 'Desactivar usuario') : (t.activateTooltip || 'Activar usuario')
                    );

                    // 🔧 CORREGIDO: Mensaje con el nombre del usuario directamente
                    var mensaje = nuevoEstado
                        ? `Usuario "${nombreUsuario}" activado correctamente.`
                        : `Usuario "${nombreUsuario}" desactivado correctamente.`;

                    mostrarToast(mensaje, nuevoEstado ? 'success' : 'warning');
                } else {
                    $(toggleActual).prop('checked', !nuevoEstado);
                    mostrarToast(respuesta.mensaje || t.errorChangeStatus || 'No se pudo cambiar el estado.', 'danger');
                }
            },
            error: function (xhr) {
                console.error('❌ Error en AJAX:', xhr.status, xhr.responseText);
                $(toggleActual).prop('checked', !nuevoEstado);
                mostrarToast(t.connectionError || 'Error de conexión con el servidor.', 'danger');
            },
            complete: function () {
                console.log('🏁 Petición completada');
                $('#btnConfirmar').prop('disabled', false).text(
                    nuevoEstado ? (t.activate || 'Activar') : (t.deactivate || 'Desactivar')
                );
                bootstrap.Modal.getInstance(
                    document.getElementById('modalConfirmar')
                ).hide();
                toggleActual = null;
            }
        });
    });

    // ── Cancelar ─────────────────────────────────────────────────────
    $('#btnCancelar').on('click', function () {
        console.log('🔴 Cancelar clickeado');
        if (toggleActual) {
            $(toggleActual).prop('checked', !nuevoEstado);
        }
        toggleActual = null;
        $('#btnConfirmar').prop('disabled', false).text(
            nuevoEstado ? (t.activate || 'Activar') : (t.deactivate || 'Desactivar')
        );
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
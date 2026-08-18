$(document).ready(function () {
    // Inicializar DataTable
    $('#tablaHistorial').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'desc']], // Ordenar por fecha descendente
        pageLength: 5,
        lengthMenu: [[5, 10, 25, -1], [5, 10, 25, "Todos"]]
    });

    // Simular cambio de estado (solo UI, sin funcionalidad real)
    $('#estadoSelector').on('change', function () {
        const nuevoEstado = $(this).val();
        const estadoActual = $('.stepper-item.active .step-name').text().toLowerCase();

        // Actualizar visualmente el stepper
        $('.stepper-item').removeClass('completed active');

        if (nuevoEstado === 'pendiente') {
            $('.stepper-item').eq(0).addClass('completed');
            $('.stepper-item').eq(0).addClass('active');
            // Mostrar mensaje simulando cambio
            mostrarNotificacion('Estado cambiado a: Pendiente');
        } else if (nuevoEstado === 'proceso') {
            $('.stepper-item').eq(0).addClass('completed');
            $('.stepper-item').eq(1).addClass('active');
            mostrarNotificacion('Estado cambiado a: En proceso');
        } else if (nuevoEstado === 'finalizado') {
            $('.stepper-item').eq(0).addClass('completed');
            $('.stepper-item').eq(1).addClass('completed');
            $('.stepper-item').eq(2).addClass('active');
            mostrarNotificacion('Estado cambiado a: Finalizado');
        } else if (nuevoEstado === 'cancelado') {
            $('.stepper-item').eq(0).addClass('completed');
            $('.stepper-item').eq(3).addClass('active');
            mostrarNotificacion('Estado cambiado a: Cancelado');
        }
    });

    // Botón marcar como pagado (solo UI)
    $('#btnMarcarPagado').on('click', function () {
        const estadoActual = $('.stepper-item.active .step-name').text();
        if (estadoActual === 'Finalizado') {
            $('.card-header .badge').removeClass('bg-label-warning').addClass('bg-label-success').text('Pagado');
            mostrarNotificacion('Servicio marcado como pagado', 'success');
            $(this).prop('disabled', true).html('<i class="bx bx-check-circle me-1"></i> Pagado');
        } else {
            mostrarNotificacion('El servicio debe estar en estado "Finalizado" para marcarlo como pagado', 'warning');
        }
    });

    function mostrarNotificacion(mensaje, tipo = 'info') {
        // Simular notificación - puedes usar toast de SNEAT si está disponible
        alert(mensaje);
    }
});
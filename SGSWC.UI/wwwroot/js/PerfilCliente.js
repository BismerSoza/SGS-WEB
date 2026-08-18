$(document).ready(function () {
    // Inicializar DataTable
    const table = $('#tablaHistorialServicios').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[1, 'desc']], // Ordenar por fecha descendente
        pageLength: 10,
        lengthMenu: [[5, 10, 25, 50, -1], [5, 10, 25, 50, "Todos"]],
        footerCallback: function (row, data, start, end, display) {
            // Calcular total solo de los registros visibles
            let api = this.api();
            let intVal = function (i) {
                return typeof i === 'string' ?
                    i.replace(/[$,]/g, '') * 1 :
                    typeof i === 'number' ? i : 0;
            };

            let total = api.column(6, { page: 'current' }).data()
                .reduce(function (a, b) {
                    return intVal(a) + intVal(b);
                }, 0);

            $(api.column(6).footer()).html('$' + total.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ","));
        }
    });

    // Búsqueda personalizada
    $('#searchInput').on('keyup', function () {
        table.search(this.value).draw();
    });

    // Botón de exportar (simulación)
    $('#btnExportar').on('click', function () {
        alert('Función de exportación disponible próximamente');
        // Aquí se implementaría la exportación a Excel/PDF
    });
});
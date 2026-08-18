// Script para actualizar métricas en tiempo real (simulado)
document.addEventListener('DOMContentLoaded', function () {
    // Simular actualización de datos cada 30 segundos (solo UI)
    // En producción, aquí iría la lógica de SignalR o polling

    console.log('Panel de rendimiento cargado - Modo estático');

    // Ejemplo de cómo cambiar el estado general a rojo (condicional)
    // Se puede usar para mostrar estado degradado basado en alguna condición
    /*
    const estadoGeneral = document.querySelector('.badge.bg-label-success');
    if (estadoGeneral && someCondition) {
        estadoGeneral.className = 'badge bg-label-danger p-3 fs-5';
        estadoGeneral.innerHTML = '<i class="bx bx-error-circle me-1"></i> Mantenimiento';
    }
    */
});
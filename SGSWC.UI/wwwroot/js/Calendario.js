// calendario.js
(function () {
    'use strict';

    const config = window.__calendarioData || {};
    console.log('📦 Config recibida:', config);

    // Asegurar que servicios sea un array
    let serviciosData = config.servicios || [];
    if (typeof serviciosData === 'string') {
        try { serviciosData = JSON.parse(serviciosData); }
        catch (e) { console.error('Error parseando servicios:', e); serviciosData = []; }
    }
    const SERVICIOS = serviciosData;
    console.log('📋 SERVICIOS final:', SERVICIOS);

    const ANIO_INIT = config.anio || new Date().getFullYear();
    const MES_INIT = config.mes || (new Date().getMonth() + 1);
    const URL_BASE = config.urlBase || '/Crm/Calendario';
    const LOCALE = config.locale || 'es-CR';
    const t = config.translations || {};

    // Referencias DOM
    const monthEl = document.querySelector('.head-month');
    const dayEl = document.querySelector('.head-day');
    const calBody = document.querySelector('#calendar tbody');
    const prevBtn = document.querySelector('.pre-button');
    const nextBtn = document.querySelector('.next-button');

    let viewDate = new Date(ANIO_INIT, MES_INIT - 1, 1);
    const fmtMes = new Intl.DateTimeFormat(LOCALE, { month: 'long', year: 'numeric' });
    const fmtDia = new Intl.DateTimeFormat(LOCALE, { weekday: 'short', day: '2-digit' });

    let indiceServicios = {};

    // ── Exponer el índice para que el panel lateral pueda consultarlo ──
    window.__getServiciosPorFecha = function (fecha) {
        return indiceServicios[fecha] || [];
    };

    function indexar(lista) {
        indiceServicios = {};
        lista.forEach(s => {
            const item = {
                idReservacion: s.idReservacion || s.IdReservacion,
                fecha: s.fecha || s.Fecha,
                hora: s.hora || s.Hora,
                nombreCliente: s.nombreCliente || s.NombreCliente,
                nombreServicio: s.nombreServicio || s.NombreServicio,
                estado: s.estado || s.Estado,
                direccionServicio: s.direccionServicio || s.DireccionServicio,
                total: s.total || s.Total,
                duracionEstimadaMin: s.duracionEstimadaMin || s.DuracionEstimadaMin,
                observacionesCliente: s.observacionesCliente || s.ObservacionesCliente,
                nombreEmpleado: s.nombreEmpleado || s.NombreEmpleado,
                idEmpleado: s.idEmpleado || s.IdEmpleado
            };
            if (!indiceServicios[item.fecha]) indiceServicios[item.fecha] = [];
            indiceServicios[item.fecha].push(item);
        });
    }

    function renderHeader() {
        const hoy = new Date();
        monthEl.textContent = fmtMes.format(viewDate);
        // Mostrar sólo el número del día de HOY (más compacto)
        dayEl.textContent = hoy.getDate();
    }

    function renderCalendar(servicios) {
        indexar(servicios);

        const year = viewDate.getFullYear();
        const month = viewDate.getMonth();
        const firstDay = new Date(year, month, 1).getDay();
        const lastDay = new Date(year, month + 1, 0).getDate();
        const hoy = new Date();

        calBody.innerHTML = '';
        let dia = 1;

        for (let row = 0; row < 6; row++) {
            const tr = document.createElement('tr');
            for (let col = 0; col < 7; col++) {
                const td = document.createElement('td');
                if ((row === 0 && col < firstDay) || dia > lastDay) {
                    td.textContent = '';
                } else {
                    td.textContent = dia;
                    const fechaStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(dia).padStart(2, '0')}`;
                    const esMismoMes = year === hoy.getFullYear() && month === hoy.getMonth();
                    if (esMismoMes && dia === hoy.getDate()) td.classList.add('today');
                    td.dataset.fecha = fechaStr;

                    if (indiceServicios[fechaStr]) {
                        td.classList.add('has-events');
                        const count = indiceServicios[fechaStr].length;
                        td.title = `${count} ${t.services || 'servicio(s)'}`;
                        // El chip real lo inyecta el MutationObserver en el .cshtml
                    }
                    dia++;
                }
                tr.appendChild(td);
            }
            calBody.appendChild(tr);
            if (dia > lastDay) break;
        }

        // Delegación de click: siempre usar el panel lateral
        calBody.onclick = function (e) {
            const td = e.target.closest('td');
            if (!td) return;
            const fecha = td.dataset.fecha;
            if (fecha && indiceServicios[fecha]) {
                window.abrirPanel(fecha);
            }
        };
    }

    function cargarMes(anio, mes) {
        window.location.href = `${URL_BASE}?anio=${anio}&mes=${mes}`;
    }

    // ── Conflicto de horario (Escenario 3) ──
    window.verificarConflicto = async function (fecha, hora, idEmpleado, duracionMin, idReservacion = null) {
        try {
            let url = `/api/CRM/ValidarConflictoHorario?fecha=${fecha}&hora=${hora}&idEmpleado=${idEmpleado}&duracionNuevaMin=${duracionMin}`;
            if (idReservacion) url += `&idReservacion=${idReservacion}`;
            const token = document.querySelector('meta[name="auth-token"]')?.content ?? '';
            const resp = await fetch(url, { headers: { 'Authorization': `Bearer ${token}` } });
            if (!resp.ok) return false;
            const data = await resp.json();
            if (data.totalConflictos > 0) { mostrarAlertaConflicto(); return true; }
            return false;
        } catch { return false; }
    };

    function mostrarAlertaConflicto() {
        const alerta = document.getElementById('alertaConflicto');
        if (alerta) {
            alerta.style.display = 'block';
            setTimeout(() => { alerta.style.display = 'none'; }, 6000);
        }
    }
    window.ocultarAlertaConflicto = function () {
        const alerta = document.getElementById('alertaConflicto');
        if (alerta) alerta.style.display = 'none';
    };

    // ── Navegación ──
    if (prevBtn) {
        prevBtn.addEventListener('click', () => {
            const d = new Date(viewDate.getFullYear(), viewDate.getMonth() - 1, 1);
            cargarMes(d.getFullYear(), d.getMonth() + 1);
        });
    }
    if (nextBtn) {
        nextBtn.addEventListener('click', () => {
            const d = new Date(viewDate.getFullYear(), viewDate.getMonth() + 1, 1);
            cargarMes(d.getFullYear(), d.getMonth() + 1);
        });
    }

    // ── Inicialización ──
    renderHeader();
    renderCalendar(SERVICIOS);
    setInterval(renderHeader, 60000);
})();
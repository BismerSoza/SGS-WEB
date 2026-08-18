document.addEventListener('DOMContentLoaded', function () {

    if (typeof ApexCharts === 'undefined') {
        console.error('ApexCharts no está cargado.');
        return;
    }

    
    // ---------- Obtener traducciones y datos desde variables globales ----------
    const translations = window.translations || {
        services: 'Servicios',
        completed: 'Completados',
        pending: 'Pendientes',
        cancelled: 'Cancelados',
        total: 'Total',
        growth: 'Crecimiento',
        trend: 'Tendencia',
        tooltipServices: 'servicios'
    };

    const monthAbbr = window.monthAbbr || ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun',
        'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

    const donutTranslations = window.donutTranslations || {
        Completed: 'Completados',
        Pending: 'Pendientes',
        Cancelled: 'Cancelados',
        Total: 'Total'
    };


    // ---------- Configuración de colores ----------
    const getConfigValue = (key, defaultValue) => {
        if (typeof config !== 'undefined' && config.colors && config.colors[key])
            return config.colors[key];
        return defaultValue;
    };

    const primaryColor = getConfigValue('primary', '#696cff');
    const successColor = getConfigValue('success', '#71dd37');
    const warningColor = getConfigValue('warning', '#ffab00');
    const dangerColor = getConfigValue('danger', '#ff3e1d');
    const headingColor = getConfigValue('headingColor', '#566a7f');
    const axisColor = getConfigValue('axisColor', '#a1acb8');
    const borderColor = getConfigValue('borderColor', '#eceef1');

    // ---------- Gráfico de servicios por mes ----------
    const serviciosChartEl = document.querySelector('#serviciosChart');
    if (serviciosChartEl) {
        const rawData = serviciosChartEl.dataset.series;
        const fullData = rawData
            ? JSON.parse(rawData)
            : [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        const serviciosChartConfig = {
            series: [{ name: translations.services, data: fullData.slice() }],
            chart: {
                height: '85%',
                type: 'area',
                parentHeightOffset: 0,
                toolbar: { show: false }
            },
            dataLabels: { enabled: false },
            stroke: { curve: 'smooth', width: 2 },
            legend: { show: false },
            markers: {
                size: 4,
                colors: ['#ffffff'],
                strokeColors: [primaryColor],
                strokeWidth: 2
            },
            grid: {
                borderColor: borderColor,
                padding: { top: -10, bottom: 5, left: 10 },
                yaxis: { lines: { show: true } }
            },
            tooltip: {
                theme: 'dark',
                y: { formatter: val => val + ' ' + translations.tooltipServices }
            },
            colors: [primaryColor],
            fill: {
                type: 'gradient',
                gradient: {
                    shade: 'light', type: 'vertical',
                    shadeIntensity: 0.3,
                    opacityFrom: 0.6, opacityTo: 0.1,
                    stops: [0, 100]
                }
            },
            xaxis: {
                categories: monthAbbr,
                axisBorder: { show: false },
                axisTicks: { show: false },
                labels: { style: { colors: axisColor, fontSize: '13px' } }
            },
            yaxis: {
                labels: { style: { colors: axisColor, fontSize: '13px' } },
                title: {
                    text: translations.services, // o "Cantidad de servicios" si prefieres
                    style: { color: headingColor }
                }
            }
        };

        const serviciosChart = new ApexCharts(serviciosChartEl, serviciosChartConfig);
        serviciosChart.render();

        // ── Lógica de filtros ─────────────────────────────────────────────
        const yearSel = document.getElementById('yearSelector');
        const monthSel = document.getElementById('monthSelector');

        if (yearSel && monthSel) {
            async function fetchAndUpdate() {
                const anio = yearSel.value;
                const mes = monthSel.value;  // "0" = todos

                try {
                    const res = await fetch(`/Crm/ConsultarServiciosPorMes?anio=${anio}&mes=${mes}`);
                    const data = await res.json();  // array de 12 posiciones

                    serviciosChart.updateSeries([{ name: translations.services, data: data }]);

                } catch (e) {
                    console.error('Error al filtrar servicios:', e);
                }
            }

            yearSel.addEventListener('change', fetchAndUpdate);
            monthSel.addEventListener('change', fetchAndUpdate);

            // Sincronizar estado inicial
            if (monthSel.value !== '0') {
                fetchAndUpdate();
            }
        }
    }

    // ---------- Gráfico de distribución (donut) ----------
    const orderStatisticsChartEl = document.querySelector('#orderStatisticsChart');
    if (orderStatisticsChartEl) {
        const completados = parseInt(orderStatisticsChartEl.dataset.completados) || 0;
        const pendientes = parseInt(orderStatisticsChartEl.dataset.pendientes) || 0;
        const cancelados = parseInt(orderStatisticsChartEl.dataset.cancelados) || 0;
        const total = parseInt(orderStatisticsChartEl.dataset.total) || 0;

        const orderStatisticsChartConfig = {
            chart: { height: 145, width: 130, type: 'donut' },
            labels: [
                donutTranslations.Completed,
                donutTranslations.Pending,
                donutTranslations.Cancelled
            ],
            series: [completados, pendientes, cancelados],
            colors: [successColor, warningColor, dangerColor],
            stroke: { width: 0 },
            dataLabels: { enabled: false },
            legend: { show: false },
            tooltip: {
                y: { formatter: val => val + ' ' + translations.tooltipServices }
            },
            plotOptions: {
                pie: {
                    donut: {
                        size: '75%',
                        labels: {
                            show: true,
                            value: {
                                fontSize: '24px',
                                fontFamily: 'Public Sans',
                                color: headingColor,
                                formatter: val => val
                            },
                            name: { offsetY: -10 },
                            total: {
                                show: true,
                                label: donutTranslations.Total,
                                formatter: () => total.toString()
                            }
                        }
                    }
                }
            }
        };

        new ApexCharts(orderStatisticsChartEl, orderStatisticsChartConfig).render();
    }

    // ---------- Sparkline de crecimiento (inicial) ----------
    const growthChartEl = document.querySelector('#growthChart');
    if (growthChartEl) {
        const serviciosChartEl = document.querySelector('#serviciosChart');
        let realData = [0, 0, 0, 0, 0, 0];
        if (serviciosChartEl && serviciosChartEl.dataset.series) {
            try {
                const fullData = JSON.parse(serviciosChartEl.dataset.series);
                if (Array.isArray(fullData) && fullData.length >= 6) {
                    realData = fullData.slice(-6);
                } else if (Array.isArray(fullData)) {
                    realData = [...Array(6 - fullData.length).fill(0), ...fullData];
                }
            } catch (e) {
                console.error('Error al parsear data-series para sparkline:', e);
            }
        }

        const growthChartConfig = {
            series: [{ name: translations.growth, data: realData }],
            chart: {
                height: 120, type: 'line',
                parentHeightOffset: 0,
                toolbar: { show: false }
            },
            tooltip: { enabled: false },
            grid: {
                show: false,
                padding: { left: -10, top: -15, right: 0, bottom: -15 }
            },
            stroke: { width: 3, curve: 'smooth' },
            colors: [primaryColor],
            markers: { size: 0 },
            xaxis: {
                labels: { show: false },
                axisTicks: { show: false },
                axisBorder: { show: false }
            },
            yaxis: { show: false }
        };

        window.growthChartInstance = new ApexCharts(growthChartEl, growthChartConfig);
        window.growthChartInstance.render();
    }

    // ---------- Sparkline independiente (con filtros) ----------
    const trendYearSel = document.getElementById('trendYearSelector');
    const trendMonthSel = document.getElementById('trendMonthSelector');
    const labelEl = document.getElementById('growthChartLabel');

    // Usamos monthAbbr para los nombres de los meses en la etiqueta
    async function fetchAndUpdateSparkline() {
        const anio = trendYearSel.value;
        const mesNum = parseInt(trendMonthSel.value);

        try {
            const res = await fetch(`/Crm/ConsultarServiciosPorMes?anio=${anio}&mes=0`);
            const data = await res.json();  // array de 12

            let spark6;
            let labelText;

            if (mesNum === 0) {
                // "Últimos 6" → ventana hasta el último mes con datos
                const ultimoConDatos = data.reduceRight((acc, val, idx) => {
                    return acc === -1 && val > 0 ? idx : acc;
                }, -1);
                const hasta = ultimoConDatos !== -1 ? ultimoConDatos : 11;
                const desde = Math.max(0, hasta - 5);
                spark6 = data.slice(desde, hasta + 1);
                labelText = `${monthAbbr[desde]} – ${monthAbbr[hasta]} ${anio}`;
            } else {
                // Mes específico → 5 meses anteriores + ese mes
                const idx = mesNum - 1;
                const desde = Math.max(0, idx - 5);
                spark6 = data.slice(desde, idx + 1);
                labelText = `${monthAbbr[desde]} – ${monthAbbr[idx]} ${anio}`;
            }

            // Rellenar a la izquierda si hay menos de 6 puntos
            while (spark6.length < 6) spark6.unshift(0);

            if (window.growthChartInstance) {
                window.growthChartInstance.updateSeries([
                    { name: translations.trend, data: spark6 }
                ]);
            }

            if (labelEl) labelEl.textContent = labelText;

        } catch (e) {
            console.error('Error al actualizar sparkline:', e);
        }
    }

    if (trendYearSel && trendMonthSel) {
        trendYearSel.addEventListener('change', fetchAndUpdateSparkline);
        trendMonthSel.addEventListener('change', fetchAndUpdateSparkline);

        // Sincronizar estado inicial
        fetchAndUpdateSparkline();
    }
});
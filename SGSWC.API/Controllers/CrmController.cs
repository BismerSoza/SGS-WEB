using ClosedXML.Excel;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SGSWC.API.Models;
using SGSWC.API.Services;
using System.Data;

namespace SGSWC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CRMController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly MonitorRendimientoService _monitor;

        public CRMController(IConfiguration configuration, MonitorRendimientoService monitor)
        {
            _configuration = configuration;
            _monitor = monitor;
        }

        #region Dashboard

        [Authorize]
        [HttpGet]
        [Route("ConsultarDashboard")]
        public IActionResult ConsultarDashboard()
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var resultado = context.QueryFirstOrDefault<DashboardResponseModel>(
                "ConsultarDashboard",
                commandType: CommandType.StoredProcedure
            );

            if (resultado == null)
                return Ok(new DashboardResponseModel
                {
                    ServicioTop = "N/A",
                    PeriodoDesde = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    PeriodoHasta = DateTime.Now
                });

            return Ok(resultado);
        }

        #endregion

        #region Servicios por mes

        [Authorize]
        [HttpGet]
        [Route("ConsultarServiciosPorMes")]
        public IActionResult ConsultarServiciosPorMes(int? anio = null, int? mes = null)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var resultado = context.Query<ServiciosPorMesResponseModel>(
                "ConsultarServiciosPorMes",
                new { anio, mes },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return Ok(resultado);
        }

        #endregion

        #region Calendario (HU-GC-001)

        [Authorize]
        [HttpGet]
        [Route("ConsultarReservacionesPorMes")]
        public IActionResult ConsultarReservacionesPorMes(int anio, int mes)
        {
            if (anio == 0) anio = DateTime.Now.Year;
            if (mes == 0) mes = DateTime.Now.Month;

            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var resultado = context.Query<CalendarioReservaResponseModel>(
                "ConsultarReservacionesPorMes",
                new { anio, mes },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return Ok(resultado);
        }

        [Authorize]
        [HttpGet]
        [Route("ValidarConflictoHorario")]
        public IActionResult ValidarConflictoHorario(
            string fecha,
            string hora,
            int idEmpleado,
            int? duracionNuevaMin = null,
            int? idReservacion = null)
        {
            if (!DateTime.TryParse(fecha, out _) || !TimeSpan.TryParse(hora, out _))
                return BadRequest(new { mensaje = "Datos inválidos." });

            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            if (!duracionNuevaMin.HasValue && idReservacion.HasValue)
            {
                duracionNuevaMin = context.QueryFirstOrDefault<int?>(
                    "SELECT ISNULL(SUM(S.duracion_estimada_min), 60) " +
                    "FROM ReservaServicios RS " +
                    "INNER JOIN Servicios S ON RS.id_servicio = S.id_servicio " +
                    "WHERE RS.id_reservacion = @id",
                    new { id = idReservacion.Value });
            }

            var resultado = context.QueryFirstOrDefault<ConflictoHorarioResponseModel>(
                "ValidarConflictoHorario",
                new
                {
                    fecha = DateTime.Parse(fecha).Date,
                    hora = TimeSpan.Parse(hora),
                    id_empleado = idEmpleado,
                    duracion_nueva_min = duracionNuevaMin ?? 60,
                    id_reservacion = idReservacion
                },
                commandType: CommandType.StoredProcedure
            );

            return Ok(resultado ?? new ConflictoHorarioResponseModel());
        }

        [Authorize]
        [HttpGet]
        [Route("ObtenerEmpleadosConDisponibilidad")]
        public IActionResult ObtenerEmpleadosConDisponibilidad(
            string fecha,
            string hora,
            int duracionMin = 60,
            int? idReservacion = null)
        {
            if (!DateTime.TryParse(fecha, out _) || !TimeSpan.TryParse(hora, out _))
                return BadRequest(new { mensaje = "Datos inválidos." });

            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var resultado = context.Query<EmpleadoDisponibilidadModel>(
                "ObtenerEmpleadosConDisponibilidad",
                new
                {
                    fecha = DateTime.Parse(fecha).Date,
                    hora = TimeSpan.Parse(hora),
                    duracion_min = duracionMin,
                    id_reservacion = idReservacion
                },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return Ok(resultado);
        }

        [Authorize]
        [HttpPost]
        [Route("AsignarEmpleado")]
        public IActionResult AsignarEmpleado([FromBody] AsignarEmpleadoRequest req)
        {
            var idSesion = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");

            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            TimeSpan? horaInicio = null;
            if (!string.IsNullOrEmpty(req.HoraInicio))
                horaInicio = TimeSpan.Parse(req.HoraInicio);

            var parametros = new DynamicParameters();
            parametros.Add("@id_reservacion", req.IdReservacion);
            parametros.Add("@id_empleado", req.IdEmpleado);
            parametros.Add("@id_usuario_sesion", idSesion);
            parametros.Add("@hora_inicio", horaInicio);
            parametros.Add("@duracion_min", req.DuracionMin);
            parametros.Add("@estado_asignacion", req.EstadoAsignacion ?? "pendiente");
            parametros.Add("@observaciones", req.Observaciones);

            var resultado = context.ExecuteScalar<int>(
                "AsignarEmpleadoReservacion",
                parametros,
                commandType: CommandType.StoredProcedure
            );

            return resultado switch
            {
                1 => Ok(new { mensaje = "Empleado asignado correctamente." }),
                2 => Ok(new { mensaje = "El empleado ya estaba asignado a esta reserva." }),
                _ => StatusCode(500, new { mensaje = "Error al asignar." })
            };
        }

        #endregion

        #region HU-RE-004 - Exportar Reportes

        private List<ReporteServiciosResponseModel> ObtenerDatosReporte(string? fechaDesde, string? fechaHasta)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            DateTime? desde = string.IsNullOrEmpty(fechaDesde) ? null : DateTime.Parse(fechaDesde);
            DateTime? hasta = string.IsNullOrEmpty(fechaHasta) ? null : DateTime.Parse(fechaHasta);

            return context.Query<ReporteServiciosResponseModel>(
                "ExportarReporteServicios",
                new { FechaDesde = desde, FechaHasta = hasta },
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarReporteExcel")]
        public IActionResult ExportarReporteExcel(string? fechaDesde = null, string? fechaHasta = null)
        {
            try
            {
                var datos = ObtenerDatosReporte(fechaDesde, fechaHasta);

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos para exportar en el rango seleccionado." });

                using var workbook = new XLWorkbook();
                var hoja = workbook.Worksheets.Add("Reporte de Servicios");

                hoja.Range("A1:N1").Merge().Value = "Reporte de Servicios - SGS Web Clean";
                hoja.Range("A1:N1").Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(14)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#4f46e5"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var etiquetaFecha = $"Período: {fechaDesde ?? "inicio"} al {fechaHasta ?? "hoy"}";
                hoja.Range("A2:N2").Merge().Value = etiquetaFecha;
                hoja.Range("A2:N2").Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var cabeceras = new[]
                {
                    "# Reserva", "Fecha", "Hora", "Cliente", "Correo", "Teléfono",
                    "Servicio", "Precio Base", "Total", "Dirección", "Estado",
                    "Observaciones", "Empleado", "F. Creación"
                };

                for (int i = 0; i < cabeceras.Length; i++)
                {
                    var celda = hoja.Cell(3, i + 1);
                    celda.Value = cabeceras[i];
                    celda.Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#e0e7ff"))
                        .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                int fila = 4;
                foreach (var item in datos)
                {
                    hoja.Cell(fila, 1).Value = item.IdReservacion;
                    hoja.Cell(fila, 2).Value = item.FechaReserva;
                    hoja.Cell(fila, 3).Value = item.HoraReserva;
                    hoja.Cell(fila, 4).Value = item.NombreCliente;
                    hoja.Cell(fila, 5).Value = item.CorreoCliente;
                    hoja.Cell(fila, 6).Value = item.TelefonoCliente;
                    hoja.Cell(fila, 7).Value = item.NombreServicio;
                    hoja.Cell(fila, 8).Value = (double)item.PrecioBase;
                    hoja.Cell(fila, 8).Style.NumberFormat.Format = "₡#,##0.00";
                    hoja.Cell(fila, 9).Value = (double)item.TotalReservacion;
                    hoja.Cell(fila, 9).Style.NumberFormat.Format = "₡#,##0.00";
                    hoja.Cell(fila, 10).Value = item.DireccionServicio;
                    hoja.Cell(fila, 11).Value = item.EstadoReservacion;
                    hoja.Cell(fila, 12).Value = item.Observaciones;
                    hoja.Cell(fila, 13).Value = item.NombreEmpleado;
                    hoja.Cell(fila, 14).Value = item.FechaCreacion;

                    if (fila % 2 == 0)
                        hoja.Row(fila).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f8f9ff"));

                    fila++;
                }

                var filaTotal = fila;
                hoja.Cell(filaTotal, 8).Value = datos.Sum(d => (double)d.PrecioBase);
                hoja.Cell(filaTotal, 8).Style.NumberFormat.Format = "₡#,##0.00";
                hoja.Cell(filaTotal, 9).Value = datos.Sum(d => (double)d.TotalReservacion);
                hoja.Cell(filaTotal, 9).Style.NumberFormat.Format = "₡#,##0.00";
                hoja.Range(filaTotal, 1, filaTotal, 14).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#c7d2fe"));
                hoja.Cell(filaTotal, 7).Value = "TOTAL:";

                hoja.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nombreArchivo = $"Reporte_Servicios_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al generar Excel: {ex.Message}" });
            }
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarReportePDF")]
        public IActionResult ExportarReportePDF(string? fechaDesde = null, string? fechaHasta = null)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var datos = ObtenerDatosReporte(fechaDesde, fechaHasta);

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos para exportar en el rango seleccionado." });

                var totalIngresos = datos.Sum(d => d.TotalReservacion);
                var periodoLabel = $"Período: {fechaDesde ?? "inicio"} al {fechaHasta ?? "hoy"}";

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(s => s.FontSize(8).FontFamily("Arial"));

                        page.Header().Element(header =>
                        {
                            header.Column(col =>
                            {
                                col.Item().Background("#4f46e5").Padding(10).Row(row =>
                                {
                                    row.RelativeItem().Text("SGS Web Clean — Reporte de Servicios")
                                        .FontSize(16).FontColor("#ffffff").Bold();
                                    row.ConstantItem(200).AlignRight().Text(periodoLabel)
                                        .FontSize(9).FontColor("#c7d2fe");
                                });
                                col.Item().Height(4);
                            });
                        });

                        page.Content().Element(content =>
                        {
                            content.Column(col =>
                            {
                                col.Item().Padding(8).Row(row =>
                                {
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Total Reservaciones").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text(datos.Count.ToString()).FontSize(18).Bold().FontColor("#4f46e5");
                                        });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Total Ingresos").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text($"₡{totalIngresos:N2}").FontSize(18).Bold().FontColor("#059669");
                                        });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Generado").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(12).Bold();
                                        });
                                });

                                col.Item().Table(tabla =>
                                {
                                    tabla.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(40);
                                        cols.ConstantColumn(60);
                                        cols.ConstantColumn(40);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                        cols.ConstantColumn(65);
                                        cols.ConstantColumn(65);
                                        cols.ConstantColumn(50);
                                    });

                                    static IContainer CeldaEncabezado(IContainer c) =>
                                        c.Background("#e0e7ff").Padding(4).AlignCenter();

                                    tabla.Header(h =>
                                    {
                                        h.Cell().Element(CeldaEncabezado).Text("# Res.").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Fecha").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Hora").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Cliente").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Servicio").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Total").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Estado").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Empleado").Bold();
                                    });

                                    for (int i = 0; i < datos.Count; i++)
                                    {
                                        var item = datos[i];
                                        var bgColor = i % 2 == 0 ? "#ffffff" : "#f8f9ff";

                                        static IContainer Celda(IContainer c, string bg) =>
                                            c.Background(bg).BorderBottom(1).BorderColor("#e5e7eb").Padding(3);

                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter()
                                            .Text(item.IdReservacion.ToString());
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter()
                                            .Text(item.FechaReserva);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter()
                                            .Text(item.HoraReserva);
                                        tabla.Cell().Element(c => Celda(c, bgColor))
                                            .Text(item.NombreCliente);
                                        tabla.Cell().Element(c => Celda(c, bgColor))
                                            .Text(item.NombreServicio);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignRight()
                                            .Text($"₡{item.TotalReservacion:N2}");
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter()
                                            .Text(item.EstadoReservacion);
                                        tabla.Cell().Element(c => Celda(c, bgColor))
                                            .Text(item.NombreEmpleado);
                                    }

                                    tabla.Cell().ColumnSpan(5).Background("#c7d2fe").Padding(4)
                                        .AlignRight().Text("TOTAL:").Bold();
                                    tabla.Cell().Background("#c7d2fe").Padding(4).AlignRight()
                                        .Text($"₡{totalIngresos:N2}").Bold();
                                    tabla.Cell().ColumnSpan(2).Background("#c7d2fe");
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(txt =>
                        {
                            txt.Span("SGS Web Clean — Página ");
                            txt.CurrentPageNumber();
                            txt.Span(" de ");
                            txt.TotalPages();
                        });
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var nombreArchivo = $"Reporte_Servicios_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al generar PDF: {ex.Message}" });
            }
        }

        #endregion

        #region HU-GC-004 Validar Cliente Frecuente

        [Authorize]
        [HttpGet]
        [Route("ValidarClienteFrecuente")]
        public IActionResult ValidarClienteFrecuente(int idUsuario)
        {
            if (idUsuario <= 0)
                return BadRequest(new { mensaje = "Id de usuario inválido." });

            try
            {
                using var context = new SqlConnection(
                    _configuration["ConnectionStrings:BDConnection"]);

                var resultado = context.QueryFirstOrDefault<ClienteFrecuenteEstadoResponseModel>(
                    "ValidarClienteFrecuente",
                    new { id_usuario = idUsuario },
                    commandType: CommandType.StoredProcedure
                );

                return Ok(resultado ?? new ClienteFrecuenteEstadoResponseModel { Id_Usuario = idUsuario });
            }
            catch
            {
                return StatusCode(500, new { mensaje = "Error al validar cliente frecuente." });
            }
        }

        [Authorize]
        [HttpGet]
        [Route("ConsultarClientePorId")]
        public IActionResult ConsultarClientePorId(int idUsuario)
        {
            if (idUsuario <= 0)
                return BadRequest(new { mensaje = "Id de usuario inválido." });

            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var resultado = context.QueryFirstOrDefault<ClienteDetalleResponseModel>(
                "ConsultarClientePorId",
                new { id_usuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );

            if (resultado == null)
                return NotFound(new { mensaje = "Cliente no encontrado." });

            return Ok(resultado);
        }

        [Authorize]
        [HttpGet]
        [Route("ConsultarHistorialServiciosCliente")]
        public IActionResult ConsultarHistorialServiciosCliente(int idUsuario)
        {
            if (idUsuario <= 0)
                return BadRequest(new { mensaje = "Id de usuario inválido." });

            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var resultado = context.Query<HistorialServicioClienteResponseModel>(
                "ObtenerReservacionesPorUsuario",
                new { id_usuario = idUsuario },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return Ok(resultado);
        }

        #endregion

        #region HU-RE-008 Clientes Frecuentes

        [Authorize]
        [HttpGet]
        [Route("ClientesFrecuentes")]
        public IActionResult ClientesFrecuentes(string? fechaDesde = null, string? fechaHasta = null)
        {
            try
            {
                using var context = new SqlConnection(
                    _configuration["ConnectionStrings:BDConnection"]);
                var parametros = new DynamicParameters();
                parametros.Add("@FechaDesde", string.IsNullOrEmpty(fechaDesde) ? (DateTime?)null : DateTime.Parse(fechaDesde));
                parametros.Add("@FechaHasta", string.IsNullOrEmpty(fechaHasta) ? (DateTime?)null : DateTime.Parse(fechaHasta));
                var resultado = context.Query<ClienteFrecuenteResponseModel>(
                    "ClientesFrecuentes", parametros,
                    commandType: CommandType.StoredProcedure
                ).ToList();
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Error al procesar la información.");
            }
        }

        #endregion

        #region HU-RE-009 Servicios Pendientes

        [Authorize]
        [HttpGet]
        [Route("ConsultarServiciosPendientes")]
        public IActionResult ConsultarServiciosPendientes()
        {
            try
            {
                var resultado = ObtenerServiciosPendientes();
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Error al procesar la información.");
            }
        }

        private List<ServicioPendienteResponseModel> ObtenerServiciosPendientes()
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            return context.Query<ServicioPendienteResponseModel>(
                "ConsultarServiciosPendientes",
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarServiciosPendientesExcel")]
        public IActionResult ExportarServiciosPendientesExcel()
        {
            try
            {
                var datos = ObtenerServiciosPendientes();

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos disponibles para exportar." });

                using var workbook = new XLWorkbook();
                var hoja = workbook.Worksheets.Add("Servicios Pendientes");

                hoja.Range("A1:I1").Merge().Value = "Reporte de Servicios Pendientes - SGS Web Clean";
                hoja.Range("A1:I1").Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(14)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#f59e0b"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                hoja.Range("A2:I2").Merge().Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
                hoja.Range("A2:I2").Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var cabeceras = new[]
                {
                    "# Reserva", "Cliente", "Correo", "Teléfono", "Fecha", "Hora",
                    "Servicio", "Dirección", "Total"
                };

                for (int i = 0; i < cabeceras.Length; i++)
                {
                    var celda = hoja.Cell(3, i + 1);
                    celda.Value = cabeceras[i];
                    celda.Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#fef3c7"))
                        .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                int fila = 4;
                foreach (var item in datos)
                {
                    hoja.Cell(fila, 1).Value = item.Id_Reservacion;
                    hoja.Cell(fila, 2).Value = item.Nombre_Cliente;
                    hoja.Cell(fila, 3).Value = item.Correo;
                    hoja.Cell(fila, 4).Value = item.Telefono;
                    hoja.Cell(fila, 5).Value = item.Fecha;
                    hoja.Cell(fila, 6).Value = item.Hora;
                    hoja.Cell(fila, 7).Value = item.Nombre_Servicio;
                    hoja.Cell(fila, 8).Value = item.Direccion_Servicio;
                    hoja.Cell(fila, 9).Value = (double)item.Total;
                    hoja.Cell(fila, 9).Style.NumberFormat.Format = "₡#,##0.00";

                    if (fila % 2 == 0)
                        hoja.Row(fila).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#fffbeb"));

                    fila++;
                }

                hoja.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nombreArchivo = $"Servicios_Pendientes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al generar Excel: {ex.Message}" });
            }
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarServiciosPendientesPDF")]
        public IActionResult ExportarServiciosPendientesPDF()
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var datos = ObtenerServiciosPendientes();

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos disponibles para exportar." });

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(s => s.FontSize(8).FontFamily("Arial"));

                        page.Header().Element(header =>
                        {
                            header.Background("#f59e0b").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Text("SGS Web Clean — Servicios Pendientes")
                                    .FontSize(16).FontColor("#ffffff").Bold();
                                row.ConstantItem(200).AlignRight()
                                    .Text($"{datos.Count} pendiente(s) — {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor("#fef3c7");
                            });
                        });

                        page.Content().Element(content =>
                        {
                            content.Table(tabla =>
                            {
                                tabla.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(45);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.ConstantColumn(60);
                                    cols.ConstantColumn(45);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(3);
                                    cols.ConstantColumn(60);
                                });

                                static IContainer CeldaEncabezado(IContainer c) =>
                                    c.Background("#fef3c7").Padding(4).AlignCenter();

                                tabla.Header(h =>
                                {
                                    h.Cell().Element(CeldaEncabezado).Text("# Res.").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Cliente").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Correo").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Fecha").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Hora").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Servicio").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Dirección").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Total").Bold();
                                });

                                for (int i = 0; i < datos.Count; i++)
                                {
                                    var item = datos[i];
                                    var bgColor = i % 2 == 0 ? "#ffffff" : "#fffbeb";

                                    static IContainer Celda(IContainer c, string bg) =>
                                        c.Background(bg).BorderBottom(1).BorderColor("#e5e7eb").Padding(3);

                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Id_Reservacion.ToString());
                                    tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Nombre_Cliente);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Correo);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Fecha);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Hora);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Nombre_Servicio);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Direccion_Servicio);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignRight().Text($"₡{item.Total:N2}");
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(txt =>
                        {
                            txt.Span("SGS Web Clean — Página ");
                            txt.CurrentPageNumber();
                            txt.Span(" de ");
                            txt.TotalPages();
                        });
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var nombreArchivo = $"Servicios_Pendientes_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al generar PDF: {ex.Message}" });
            }
        }

        #endregion

        #region HU-RE-012 Horarios Más Solicitados

        [Authorize]
        [HttpGet]
        [Route("HorariosMasSolicitados")]
        public IActionResult HorariosMasSolicitados(string? fechaDesde = null, string? fechaHasta = null)
        {
            try
            {
                using var context = new SqlConnection(
                    _configuration["ConnectionStrings:BDConnection"]);
                var parametros = new DynamicParameters();
                parametros.Add("@FechaDesde", string.IsNullOrEmpty(fechaDesde) ? (DateTime?)null : DateTime.Parse(fechaDesde));
                parametros.Add("@FechaHasta", string.IsNullOrEmpty(fechaHasta) ? (DateTime?)null : DateTime.Parse(fechaHasta));
                var resultado = context.Query<HorarioSolicitadoResponseModel>(
                    "HorariosMasSolicitados", parametros,
                    commandType: CommandType.StoredProcedure
                ).ToList();
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Error al procesar la información.");
            }
        }

        #endregion

        #region HU-RE-006 Reporte Personalizado

        [Authorize]
        [HttpGet]
        [Route("ReportePersonalizado")]
        public IActionResult ReportePersonalizado(
            string? fechaDesde = null,
            string? fechaHasta = null,
            int? idServicio = null,
            string? estadoPago = null,
            int? idEstado = null)
        {
            try
            {
                using var context = new SqlConnection(
                    _configuration["ConnectionStrings:BDConnection"]);
                DateTime? desde = string.IsNullOrEmpty(fechaDesde) ? null : DateTime.Parse(fechaDesde);
                DateTime? hasta = string.IsNullOrEmpty(fechaHasta) ? null : DateTime.Parse(fechaHasta);
                var resultado = context.Query<ReporteServiciosResponseModel>(
                    "ExportarReportePersonalizado",
                    new { FechaDesde = desde, FechaHasta = hasta, IdServicio = idServicio, EstadoPago = estadoPago, IdEstado = idEstado },
                    commandType: CommandType.StoredProcedure
                ).ToList();
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Error al procesar el reporte.");
            }
        }

        #endregion

        #region HU-RE-005 Métricas de Desempeño

        [Authorize]
        [HttpGet]
        [Route("ConsultarMetricasDesempeno")]
        public IActionResult ConsultarMetricasDesempeno(int? anio = null, int? mes = null)
        {
            try
            {
                using var context = new SqlConnection(
                    _configuration["ConnectionStrings:BDConnection"]);

                var resultado = context.QueryFirstOrDefault<MetricasDesempenoResponseModel>(
                    "ConsultarMetricasDesempeno",
                    new { anio, mes },
                    commandType: CommandType.StoredProcedure
                );

                if (resultado == null)
                    return Ok(new MetricasDesempenoResponseModel { DatosDisponibles = false });

                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, new { mensaje = "Error al procesar las métricas." });
            }
        }

        #endregion

        #region HU-GC-005 Analizar Historial de Cliente

        [Authorize]
        [HttpGet]
        [Route("AnalizarHistorialCliente")]
        public IActionResult AnalizarHistorialCliente(int idUsuario)
        {
            if (idUsuario <= 0)
                return BadRequest(new { mensaje = "Cliente no válido." });

            try
            {
                using var context = new SqlConnection(
                    _configuration["ConnectionStrings:BDConnection"]);

                var resultado = context.QueryFirstOrDefault<AnalisisHistorialClienteResponseModel>(
                    "AnalizarHistorialClienteServicios",
                    new { id_usuario = idUsuario },
                    commandType: CommandType.StoredProcedure
                );

                if (resultado == null)
                    return Ok(new AnalisisHistorialClienteResponseModel
                    {
                        Id_Usuario = idUsuario,
                        DatosSuficientes = false,
                        Mensaje = "No hay suficientes datos para análisis"
                    });

                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, new { mensaje = "Error al procesar el análisis del historial." });
            }
        }

        #endregion

        #region HU-RE-001 Reporte Mensual

        private List<ReporteMensualResponseModel> ObtenerDatosReporteMensual(int anio, int mes)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            return context.Query<ReporteMensualResponseModel>(
                "ConsultarReporteMensual",
                new { anio, mes },
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarReporteMensualPDF")]
        public IActionResult ExportarReporteMensualPDF(int anio, int mes)
        {
            try
            {
                if (anio <= 0 || mes < 1 || mes > 12)
                    return BadRequest(new { mensaje = "Período inválido." });

                QuestPDF.Settings.License = LicenseType.Community;

                var datos = ObtenerDatosReporteMensual(anio, mes);

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos disponibles" });

                var nombreMes = new DateTime(anio, mes, 1).ToString("MMMM yyyy",
                    new System.Globalization.CultureInfo("es-CR"));
                var totalIngresos = datos.Sum(d => d.TotalReservacion);
                var totalPagado = datos.Sum(d => d.MontoPagado);

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(s => s.FontSize(8).FontFamily("Arial"));

                        page.Header().Element(header =>
                        {
                            header.Background("#0ea5e9").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Text($"SGS Web Clean — Reporte Mensual ({nombreMes})")
                                    .FontSize(16).FontColor("#ffffff").Bold();
                                row.ConstantItem(200).AlignRight()
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor("#e0f2fe");
                            });
                        });

                        page.Content().Element(content =>
                        {
                            content.Column(col =>
                            {
                                col.Item().Padding(8).Row(row =>
                                {
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Servicios del Mes").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text(datos.Count.ToString()).FontSize(18).Bold().FontColor("#0ea5e9");
                                        });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Ingresos Facturados").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text($"₡{totalIngresos:N2}").FontSize(18).Bold().FontColor("#059669");
                                        });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Total Pagado").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text($"₡{totalPagado:N2}").FontSize(18).Bold().FontColor("#7c3aed");
                                        });
                                });

                                col.Item().Table(tabla =>
                                {
                                    tabla.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(40);
                                        cols.ConstantColumn(55);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                        cols.ConstantColumn(60);
                                        cols.ConstantColumn(55);
                                        cols.ConstantColumn(60);
                                        cols.ConstantColumn(55);
                                    });

                                    static IContainer CeldaEncabezado(IContainer c) =>
                                        c.Background("#e0f2fe").Padding(4).AlignCenter();

                                    tabla.Header(h =>
                                    {
                                        h.Cell().Element(CeldaEncabezado).Text("# Res.").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Fecha").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Cliente").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Servicio").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Total").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Estado").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Pago").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Monto Pag.").Bold();
                                    });

                                    for (int i = 0; i < datos.Count; i++)
                                    {
                                        var item = datos[i];
                                        var bgColor = i % 2 == 0 ? "#ffffff" : "#f0f9ff";

                                        static IContainer Celda(IContainer c, string bg) =>
                                            c.Background(bg).BorderBottom(1).BorderColor("#e5e7eb").Padding(3);

                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.IdReservacion.ToString());
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.FechaReserva);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.NombreCliente);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.NombreServicio);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignRight().Text($"₡{item.TotalReservacion:N2}");
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.EstadoReservacion);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.EstadoPago);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignRight().Text($"₡{item.MontoPagado:N2}");
                                    }
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(txt =>
                        {
                            txt.Span("SGS Web Clean — Página ");
                            txt.CurrentPageNumber();
                            txt.Span(" de ");
                            txt.TotalPages();
                        });
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var nombreArchivo = $"Reporte_Mensual_{anio}_{mes:D2}.pdf";
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al procesar el reporte." });
            }
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarReporteMensualExcel")]
        public IActionResult ExportarReporteMensualExcel(int anio, int mes)
        {
            try
            {
                if (anio <= 0 || mes < 1 || mes > 12)
                    return BadRequest(new { mensaje = "Período inválido." });

                var datos = ObtenerDatosReporteMensual(anio, mes);

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos disponibles" });

                using var workbook = new XLWorkbook();
                var hoja = workbook.Worksheets.Add("Reporte Mensual");

                var nombreMes = new DateTime(anio, mes, 1).ToString("MMMM yyyy",
                    new System.Globalization.CultureInfo("es-CR"));

                hoja.Range("A1:P1").Merge().Value = $"Reporte Mensual — {nombreMes} — SGS Web Clean";
                hoja.Range("A1:P1").Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(14)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#0ea5e9"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var cabeceras = new[]
                {
                    "# Reserva", "Fecha", "Hora", "Cliente", "Correo", "Teléfono",
                    "Servicio", "Precio Base", "Total", "Dirección", "Estado",
                    "Empleado", "Estado Pago", "Método de Pago", "Monto Pagado", "F. Creación"
                };

                for (int i = 0; i < cabeceras.Length; i++)
                {
                    var celda = hoja.Cell(2, i + 1);
                    celda.Value = cabeceras[i];
                    celda.Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#e0f2fe"))
                        .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                int fila = 3;
                foreach (var item in datos)
                {
                    hoja.Cell(fila, 1).Value = item.IdReservacion;
                    hoja.Cell(fila, 2).Value = item.FechaReserva;
                    hoja.Cell(fila, 3).Value = item.HoraReserva;
                    hoja.Cell(fila, 4).Value = item.NombreCliente;
                    hoja.Cell(fila, 5).Value = item.CorreoCliente;
                    hoja.Cell(fila, 6).Value = item.TelefonoCliente;
                    hoja.Cell(fila, 7).Value = item.NombreServicio;
                    hoja.Cell(fila, 8).Value = (double)item.PrecioBase;
                    hoja.Cell(fila, 8).Style.NumberFormat.Format = "₡#,##0.00";
                    hoja.Cell(fila, 9).Value = (double)item.TotalReservacion;
                    hoja.Cell(fila, 9).Style.NumberFormat.Format = "₡#,##0.00";
                    hoja.Cell(fila, 10).Value = item.DireccionServicio;
                    hoja.Cell(fila, 11).Value = item.EstadoReservacion;
                    hoja.Cell(fila, 12).Value = item.NombreEmpleado;
                    hoja.Cell(fila, 13).Value = item.EstadoPago;
                    hoja.Cell(fila, 14).Value = item.MetodoPago;
                    hoja.Cell(fila, 15).Value = (double)item.MontoPagado;
                    hoja.Cell(fila, 15).Style.NumberFormat.Format = "₡#,##0.00";
                    hoja.Cell(fila, 16).Value = item.FechaCreacion;

                    if (fila % 2 == 0)
                        hoja.Row(fila).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f0f9ff"));

                    fila++;
                }

                var filaTotal = fila;
                hoja.Cell(filaTotal, 7).Value = "TOTAL:";
                hoja.Cell(filaTotal, 9).Value = datos.Sum(d => (double)d.TotalReservacion);
                hoja.Cell(filaTotal, 9).Style.NumberFormat.Format = "₡#,##0.00";
                hoja.Cell(filaTotal, 15).Value = datos.Sum(d => (double)d.MontoPagado);
                hoja.Cell(filaTotal, 15).Style.NumberFormat.Format = "₡#,##0.00";
                hoja.Range(filaTotal, 1, filaTotal, 16).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#bae6fd"));

                hoja.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nombreArchivo = $"Reporte_Mensual_{anio}_{mes:D2}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al procesar el reporte." });
            }
        }

        #endregion

        #region HU-RE-011 - Reporte Clientes Nuevos

        [Authorize]
        [HttpGet("ReporteClientesNuevos")]
        public IActionResult ReporteClientesNuevos(
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();
            p.Add("@fecha_inicio", string.IsNullOrEmpty(fechaInicio) ? null : fechaInicio);
            p.Add("@fecha_fin", string.IsNullOrEmpty(fechaFin) ? null : fechaFin);

            using var multi = context.QueryMultiple("ReporteClientesNuevos", p,
                commandType: CommandType.StoredProcedure);

            var kpis = multi.ReadFirstOrDefault<ReporteClientesNuevosKpiModel>();
            var clientes = multi.Read<ClienteNuevoDetalleModel>().ToList();

            return Ok(new ReporteClientesNuevosResponseModel
            {
                Kpis = kpis ?? new(),
                Clientes = clientes
            });
        }

        #endregion

        #region HU-CR-002 - MonitorRendimiento

        [Authorize]
        [HttpGet]
        [Route("EstadoSistema")]
        public IActionResult EstadoSistema()
        {
            try
            {
                var resultado = ObtenerEstadoSistema();
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Error al procesar la información.");
            }
        }

        private EstadoSistemaResponseModel ObtenerEstadoSistema()
        {
            var promedio = _monitor.ObtenerPromedio();
            var umbral = _monitor.ObtenerUmbral(_configuration);
            var activas = _monitor.ObtenerSolicitudesActivas();

            return new EstadoSistemaResponseModel
            {
                TiempoRespuestaPromedioMs = promedio,
                SolicitudesActivas = activas,
                UmbralMs = umbral,
                EstadoSistema = promedio > umbral ? "Degradado" : "Normal",
                FechaConsulta = DateTime.Now
            };
        }

        #endregion

        #region HU-RE-007 - Reporte Financiero (Ingresos y Egresos)

        private List<ReporteFinancieroResponseModel> ObtenerDatosReporteFinanciero(string? fechaDesde, string? fechaHasta)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var desde = string.IsNullOrEmpty(fechaDesde) ? DateTime.Now.ToString("yyyy-MM-01") : fechaDesde;
            var hasta = string.IsNullOrEmpty(fechaHasta) ? DateTime.Now.ToString("yyyy-MM-dd") : fechaHasta;

            return context.Query<ReporteFinancieroResponseModel>(
                "ConsultarReporteFinanciero",
                new { fechaDesde = desde, fechaHasta = hasta },
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarReporteFinancieroExcel")]
        public IActionResult ExportarReporteFinancieroExcel(string? fechaDesde = null, string? fechaHasta = null, bool simularError = false)
        {
            try
            {
                if (simularError)
                    throw new Exception("Error simulado para pruebas.");
                var datos = ObtenerDatosReporteFinanciero(fechaDesde, fechaHasta);

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos disponibles" });

                using var workbook = new XLWorkbook();
                var hoja = workbook.Worksheets.Add("Reporte Financiero");

                var etiquetaFecha = $"Período: {fechaDesde ?? "inicio"} al {fechaHasta ?? "hoy"}";

                hoja.Range("A1:F1").Merge().Value = "Reporte Financiero (Ingresos y Egresos) - SGS Web Clean";
                hoja.Range("A1:F1").Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(14)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#0ea5e9"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                hoja.Range("A2:F2").Merge().Value = etiquetaFecha;
                hoja.Range("A2:F2").Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var cabeceras = new[] { "Tipo", "Fecha", "Descripción", "Categoría", "Monto", "Referencia" };
                for (int i = 0; i < cabeceras.Length; i++)
                {
                    var celda = hoja.Cell(4, i + 1);
                    celda.Value = cabeceras[i];
                    celda.Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#e0f2fe"))
                        .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                int fila = 5;
                foreach (var item in datos)
                {
                    hoja.Cell(fila, 1).Value = item.Tipo;
                    hoja.Cell(fila, 2).Value = item.Fecha;
                    hoja.Cell(fila, 3).Value = item.Descripcion;
                    hoja.Cell(fila, 4).Value = item.Categoria;
                    hoja.Cell(fila, 5).Value = (double)item.Monto;
                    hoja.Cell(fila, 5).Style.NumberFormat.Format = "₡#,##0.00";
                    hoja.Cell(fila, 5).Style.Font.SetFontColor(
                        XLColor.FromHtml(item.Tipo == "Egreso" ? "#dc2626" : "#059669"));
                    hoja.Cell(fila, 6).Value = item.Referencia;

                    if (fila % 2 == 0)
                        hoja.Row(fila).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f0f9ff"));

                    fila++;
                }

                var totalIngresos = datos.Where(d => d.Tipo == "Ingreso").Sum(d => d.Monto);
                var totalEgresos = datos.Where(d => d.Tipo == "Egreso").Sum(d => d.Monto);
                var utilidad = totalIngresos - totalEgresos;

                var filaResumen = fila + 1;
                hoja.Cell(filaResumen, 4).Value = "Total Ingresos:";
                hoja.Cell(filaResumen, 5).Value = (double)totalIngresos;
                hoja.Cell(filaResumen, 5).Style.NumberFormat.Format = "₡#,##0.00";
                hoja.Cell(filaResumen, 5).Style.Font.SetFontColor(XLColor.FromHtml("#059669")).Font.SetBold(true);

                hoja.Cell(filaResumen + 1, 4).Value = "Total Egresos:";
                hoja.Cell(filaResumen + 1, 5).Value = (double)totalEgresos;
                hoja.Cell(filaResumen + 1, 5).Style.NumberFormat.Format = "₡#,##0.00";
                hoja.Cell(filaResumen + 1, 5).Style.Font.SetFontColor(XLColor.FromHtml("#dc2626")).Font.SetBold(true);

                hoja.Cell(filaResumen + 2, 4).Value = "Utilidad Neta (Rentabilidad):";
                hoja.Cell(filaResumen + 2, 5).Value = (double)utilidad;
                hoja.Cell(filaResumen + 2, 5).Style.NumberFormat.Format = "₡#,##0.00";
                hoja.Cell(filaResumen + 2, 5).Style.Font
                    .SetFontColor(XLColor.FromHtml(utilidad >= 0 ? "#059669" : "#dc2626")).Font.SetBold(true);
                hoja.Range(filaResumen, 4, filaResumen + 2, 5).Style.Border.SetTopBorder(XLBorderStyleValues.Thin);

                hoja.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nombreArchivo = $"Reporte_Financiero_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al procesar el reporte." });
            }
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarReporteFinancieroPDF")]
        public IActionResult ExportarReporteFinancieroPDF(string? fechaDesde = null, string? fechaHasta = null, bool simularError = false)
        {
            try
            {
                if (simularError)
                    throw new Exception("Error simulado para pruebas.");
                QuestPDF.Settings.License = LicenseType.Community;

                var datos = ObtenerDatosReporteFinanciero(fechaDesde, fechaHasta);

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos disponibles" });

                var totalIngresos = datos.Where(d => d.Tipo == "Ingreso").Sum(d => d.Monto);
                var totalEgresos = datos.Where(d => d.Tipo == "Egreso").Sum(d => d.Monto);
                var utilidad = totalIngresos - totalEgresos;
                var etiquetaFecha = $"Período: {fechaDesde ?? "inicio"} al {fechaHasta ?? "hoy"}";

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(s => s.FontSize(9).FontFamily("Arial"));

                        page.Header().Element(header =>
                        {
                            header.Background("#0ea5e9").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("SGS Web Clean — Reporte Financiero")
                                        .FontSize(16).FontColor("#ffffff").Bold();
                                    c.Item().Text(etiquetaFecha).FontSize(9).FontColor("#e0f2fe");
                                });
                                row.ConstantItem(160).AlignRight()
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor("#e0f2fe");
                            });
                        });

                        page.Content().Element(content =>
                        {
                            content.Column(col =>
                            {
                                col.Item().Padding(8).Row(row =>
                                {
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Total Ingresos").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text($"₡{totalIngresos:N2}").FontSize(16).Bold().FontColor("#059669");
                                        });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Total Egresos").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text($"₡{totalEgresos:N2}").FontSize(16).Bold().FontColor("#dc2626");
                                        });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Border(1).BorderColor("#e0e7ff")
                                        .Padding(8).Column(c =>
                                        {
                                            c.Item().Text("Utilidad Neta (Rentabilidad)").FontSize(9).FontColor("#6b7280");
                                            c.Item().Text($"₡{utilidad:N2}").FontSize(16).Bold()
                                                .FontColor(utilidad >= 0 ? "#059669" : "#dc2626");
                                        });
                                });

                                col.Item().Table(tabla =>
                                {
                                    tabla.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(50);
                                        cols.ConstantColumn(55);
                                        cols.RelativeColumn(3);
                                        cols.RelativeColumn(2);
                                        cols.ConstantColumn(70);
                                        cols.RelativeColumn(2);
                                    });

                                    static IContainer CeldaEncabezado(IContainer c) =>
                                        c.Background("#e0f2fe").Padding(4).AlignCenter();

                                    tabla.Header(h =>
                                    {
                                        h.Cell().Element(CeldaEncabezado).Text("Tipo").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Fecha").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Descripción").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Categoría").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Monto").Bold();
                                        h.Cell().Element(CeldaEncabezado).Text("Referencia").Bold();
                                    });

                                    for (int i = 0; i < datos.Count; i++)
                                    {
                                        var item = datos[i];
                                        var bgColor = i % 2 == 0 ? "#ffffff" : "#f0f9ff";
                                        var colorMonto = item.Tipo == "Egreso" ? "#dc2626" : "#059669";

                                        static IContainer Celda(IContainer c, string bg) =>
                                            c.Background(bg).BorderBottom(1).BorderColor("#e5e7eb").Padding(3);

                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Tipo);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Fecha);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Descripcion);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Categoria);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).AlignRight()
                                            .Text($"₡{item.Monto:N2}").FontColor(colorMonto);
                                        tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Referencia);
                                    }
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(txt =>
                        {
                            txt.Span("SGS Web Clean — Página ");
                            txt.CurrentPageNumber();
                            txt.Span(" de ");
                            txt.TotalPages();
                        });
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var nombreArchivo = $"Reporte_Financiero_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al procesar el reporte." });
            }
        }

        #endregion

        #region HU-RE-013 - Reporte Calificaciones Bajas

        [Authorize]
        [HttpGet("ReporteCalificacionesBajas")]
        public IActionResult ReporteCalificacionesBajas(
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin,
            [FromQuery] int umbral = 3)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();
            p.Add("@fecha_inicio", string.IsNullOrEmpty(fechaInicio) ? null : fechaInicio);
            p.Add("@fecha_fin", string.IsNullOrEmpty(fechaFin) ? null : fechaFin);
            p.Add("@umbral", umbral);

            using var multi = context.QueryMultiple(
                "ReporteCalificacionesBajas", p,
                commandType: CommandType.StoredProcedure);

            var kpis = multi.ReadFirstOrDefault<ReporteCalificacionesBajasKpiModel>();
            var servicios = multi.Read<ServicioConQuejaModel>().ToList();
            var detalle = multi.Read<CalificacionBajaDetalleModel>().ToList();

            return Ok(new ReporteCalificacionesBajasResponseModel
            {
                Kpis = kpis ?? new(),
                Servicios = servicios,
                Detalle = detalle
            });
        }

        #endregion

        #region HU-RE-002 - Reporte Estadísticas de Satisfacción

        [Authorize]
        [HttpGet("ReporteEstadisticasSatisfaccion")]
        public IActionResult ReporteEstadisticasSatisfaccion(
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();
            p.Add("@fecha_inicio", string.IsNullOrEmpty(fechaInicio) ? null : fechaInicio);
            p.Add("@fecha_fin", string.IsNullOrEmpty(fechaFin) ? null : fechaFin);

            using var multi = context.QueryMultiple(
                "ReporteEstadisticasSatisfaccion", p,
                commandType: CommandType.StoredProcedure);

            var kpis = multi.ReadFirstOrDefault<ReporteEstadisticasKpiModel>();
            var distribucion = multi.Read<DistribucionCalificacionModel>().ToList();
            var resenas = multi.Read<ResenaDetalleModel>().ToList();
            var servicios = multi.Read<ServicioContratadoModel>().ToList();

            return Ok(new ReporteEstadisticasSatisfaccionResponseModel
            {
                Kpis = kpis ?? new(),
                Distribucion = distribucion,
                Resenas = resenas,
                Servicios = servicios
            });
        }

        #endregion


        #region HU-PG-001 Gestión de Pagos (Escenarios 1 y 2)

        [Authorize]
        [HttpGet]
        [Route("ConsultarPagos")]
        public IActionResult ConsultarPagos(string? estadoPago = null)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var resultado = context.Query<PagoResponseModel>(
                "ConsultarPagosReservaciones",
                new { estado_pago = estadoPago },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return Ok(resultado);
        }

        [Authorize]
        [HttpPut]
        [Route("CambiarEstadoPago")]
        public IActionResult CambiarEstadoPago([FromBody] CambiarEstadoPagoRequest request)
        {
            var idSesion = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");

            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var parametros = new DynamicParameters();
            parametros.Add("@id_reservacion", request.Id_Reservacion);
            parametros.Add("@estado_pago_nuevo", request.Estado_Pago_Nuevo);
            parametros.Add("@id_metodo", request.Id_Metodo);
            parametros.Add("@referencia_externa", request.Referencia_Externa);
            parametros.Add("@id_usuario_sesion", idSesion);

            var resultado = context.QueryFirstOrDefault<CambiarEstadoPagoResultModel>(
                "CambiarEstadoPagoReservacion",
                parametros,
                commandType: CommandType.StoredProcedure
            );

            if (resultado == null)
                return StatusCode(500, new { mensaje = "Error al procesar el cambio de estado." });

            return resultado.Resultado switch
            {
                1 => Ok(resultado),        // Escenario 1: pago registrado / estado actualizado
                2 => Conflict(resultado),  // Escenario 2: doble pago -> acción bloqueada
                0 => NotFound(resultado),  // La reservación no existe
                _ => StatusCode(500, resultado)
            };
        }

        #endregion

        #region HU-PG-002 Reporte de Pagos (Escenario 3)

        private List<ReportePagoResponseModel> ObtenerDatosReportePagos(string? fechaDesde, string? fechaHasta)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            DateTime? desde = string.IsNullOrEmpty(fechaDesde) ? null : DateTime.Parse(fechaDesde);
            DateTime? hasta = string.IsNullOrEmpty(fechaHasta) ? null : DateTime.Parse(fechaHasta);

            return context.Query<ReportePagoResponseModel>(
                "ConsultarReportePagos",
                new { fechaDesde = desde, fechaHasta = hasta },
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        [Authorize]
        [HttpGet]
        [Route("ExportarEstadoPagosExcel")]
        public IActionResult ExportarEstadoPagosExcel()
        {
            try
            {
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
                var datos = context.Query<PagoResponseModel>(
                    "ConsultarPagosReservaciones",
                    new { estado_pago = (string?)null }, // Trae todos (pendientes y pagados)
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos para exportar." });

                using var workbook = new XLWorkbook();
                var hoja = workbook.Worksheets.Add("Estado de Pagos");

                // Encabezado
                hoja.Range("A1:I1").Merge().Value = "Estado de Pagos - SGS Web Clean";
                hoja.Range("A1:I1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#059669"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var cabeceras = new[] {
            "# Reserva", "Cliente", "Correo", "Servicio",
            "Fecha", "Hora", "Total", "Estado Pago", "Estado Reserva"
        };

                for (int i = 0; i < cabeceras.Length; i++)
                {
                    var celda = hoja.Cell(3, i + 1);
                    celda.Value = cabeceras[i];
                    celda.Style.Font.SetBold(true)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#d1fae5"))
                        .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                int fila = 4;
                foreach (var item in datos)
                {
                    hoja.Cell(fila, 1).Value = item.Id_Reservacion;
                    hoja.Cell(fila, 2).Value = item.Nombre_Cliente;
                    hoja.Cell(fila, 3).Value = item.Correo;
                    hoja.Cell(fila, 4).Value = item.Nombre_Servicio;
                    hoja.Cell(fila, 5).Value = item.Fecha;
                    hoja.Cell(fila, 6).Value = item.Hora;
                    hoja.Cell(fila, 7).Value = (double)item.Total;
                    hoja.Cell(fila, 7).Style.NumberFormat.Format = "₡#,##0.00";
                    hoja.Cell(fila, 8).Value = item.Estado_Pago;
                    hoja.Cell(fila, 9).Value = item.Estado_Reservacion;
                    if (fila % 2 == 0)
                        hoja.Row(fila).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f0fdf4"));
                    fila++;
                }

                hoja.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nombreArchivo = $"Estado_Pagos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al generar Excel: {ex.Message}" });
            }
        }

        // Exportar PDF con el listado completo de pagos (UI)
        [Authorize]
        [HttpGet]
        [Route("ExportarEstadoPagosPDF")]
        public IActionResult ExportarEstadoPagosPDF()
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
                var datos = context.Query<PagoResponseModel>(
                    "ConsultarPagosReservaciones",
                    new { estado_pago = (string?)null },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (!datos.Any())
                    return BadRequest(new { mensaje = "No hay datos para exportar." });

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(s => s.FontSize(8).FontFamily("Arial"));

                        page.Header().Element(header =>
                        {
                            header.Background("#059669").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Text("SGS Web Clean — Estado de Pagos")
                                    .FontSize(16).FontColor("#ffffff").Bold();
                                row.ConstantItem(220).AlignRight()
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor("#d1fae5");
                            });
                        });

                        page.Content().Element(content =>
                        {
                            content.Table(tabla =>
                            {
                                tabla.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(40);  // #Reserva
                                    cols.RelativeColumn(2);   // Cliente
                                    cols.RelativeColumn(2);   // Correo
                                    cols.RelativeColumn(2);   // Servicio
                                    cols.ConstantColumn(65);  // Fecha
                                    cols.ConstantColumn(45);  // Hora
                                    cols.ConstantColumn(65);  // Total
                                    cols.ConstantColumn(60);  // Estado Pago
                                    cols.ConstantColumn(60);  // Estado Reserva
                                });

                                static IContainer CeldaEncabezado(IContainer c) =>
                                    c.Background("#d1fae5").Padding(4).AlignCenter();

                                tabla.Header(h =>
                                {
                                    h.Cell().Element(CeldaEncabezado).Text("# Res.").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Cliente").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Correo").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Servicio").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Fecha").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Hora").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Total").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Estado Pago").Bold();
                                    h.Cell().Element(CeldaEncabezado).Text("Estado Reserva").Bold();
                                });

                                for (int i = 0; i < datos.Count; i++)
                                {
                                    var item = datos[i];
                                    var bgColor = i % 2 == 0 ? "#ffffff" : "#f0fdf4";

                                    static IContainer Celda(IContainer c, string bg) =>
                                        c.Background(bg).BorderBottom(1).BorderColor("#e5e7eb").Padding(3);

                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Id_Reservacion.ToString());
                                    tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Nombre_Cliente);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Correo);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).Text(item.Nombre_Servicio);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Fecha);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Hora);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignRight().Text($"₡{item.Total:N2}");
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Estado_Pago);
                                    tabla.Cell().Element(c => Celda(c, bgColor)).AlignCenter().Text(item.Estado_Reservacion);
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(txt =>
                        {
                            txt.Span("SGS Web Clean — Página ");
                            txt.CurrentPageNumber();
                            txt.Span(" de ");
                            txt.TotalPages();
                        });
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var nombreArchivo = $"Estado_Pagos_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al generar PDF: {ex.Message}" });
            }
        }

        #endregion
    }
}
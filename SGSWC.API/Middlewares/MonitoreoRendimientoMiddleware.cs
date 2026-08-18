using Dapper;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using SGSWC.API.Services;

namespace SGSWC.API.Middlewares
{
    public class MonitoreoRendimientoMiddleware
    {
        private readonly RequestDelegate _next;

        public MonitoreoRendimientoMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, MonitorRendimientoService monitor, IConfiguration configuration)
        {
            monitor.IncrementarActivas();
            var cronometro = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                cronometro.Stop();
                monitor.DecrementarActivas();

                var tiempoMs = cronometro.Elapsed.TotalMilliseconds;
                monitor.RegistrarTiempo(tiempoMs);

                var umbral = monitor.ObtenerUmbral(configuration);
                if (tiempoMs > umbral)
                {
                    RegistrarIncidente(context, configuration, context.Request.Path, tiempoMs, umbral);
                }
            }
        }

        private void RegistrarIncidente(HttpContext context, IConfiguration configuration, string ruta, double tiempoMs, double umbral)
        {
            try
            {
                using var conexion = new SqlConnection(
                    configuration["ConnectionStrings:BDConnection"]);

                // Intenta extraer el usuario si la request ya fue autenticada (JWT)
                int? idUsuario = null;
                var claimUsuario = context.User?.FindFirst("IdUsuario")?.Value;
                if (int.TryParse(claimUsuario, out var idParsed))
                    idUsuario = idParsed;

                var ip = context.Connection.RemoteIpAddress?.ToString();

                conexion.Execute(
                    @"INSERT INTO Bitacora
                (id_usuario, accion, tabla_afectada, descripcion_cambio, fecha,
                 direccion_ip, valor_anterior, valor_nuevo, tipo_evento, modulo)
              VALUES
                (@idUsuario, @accion, @tabla, @descripcion, GETDATE(),
                 @ip, @valorAnterior, @valorNuevo, @tipoEvento, @modulo)",
                    new
                    {
                        idUsuario,
                        accion = "ALERTA",
                        tabla = "Sistema",
                        descripcion = $"Ruta {ruta} respondió en {tiempoMs:N0}ms (umbral: {umbral:N0}ms)",
                        ip,
                        valorAnterior = $"{umbral:N0}ms (umbral)",
                        valorNuevo = $"{tiempoMs:N0}ms (detectado)",
                        tipoEvento = "ALERTA_RENDIMIENTO",
                        modulo = "Monitoreo"
                    });
            }
            catch { }
        }
    }
}
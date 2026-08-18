using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;

namespace SGSWC.API.Services
{
    public class MonitorRendimientoService
    {
        private readonly ConcurrentQueue<double> _tiempos = new();
        private const int VentanaMaxima = 100;
        private int _solicitudesActivas = 0;

        private double _umbralCacheado = 800;
        private DateTime _umbralActualizado = DateTime.MinValue;
        private readonly TimeSpan _intervaloRefresco = TimeSpan.FromMinutes(1);
        private readonly object _candado = new();

        public void IncrementarActivas() => Interlocked.Increment(ref _solicitudesActivas);
        public void DecrementarActivas() => Interlocked.Decrement(ref _solicitudesActivas);
        public int ObtenerSolicitudesActivas() => _solicitudesActivas;

        public void RegistrarTiempo(double ms)
        {
            _tiempos.Enqueue(ms);
            while (_tiempos.Count > VentanaMaxima)
                _tiempos.TryDequeue(out _);
        }

        public double ObtenerPromedio()
        {
            var snapshot = _tiempos.ToArray();
            return snapshot.Length == 0 ? 0 : Math.Round(snapshot.Average(), 2);
        }

        public double ObtenerUmbral(IConfiguration configuration)
        {
            lock (_candado)
            {
                if (DateTime.Now - _umbralActualizado < _intervaloRefresco)
                    return _umbralCacheado;
            }

            try
            {
                using var context = new SqlConnection(
                    configuration["ConnectionStrings:BDConnection"]);

                var valor = context.QueryFirstOrDefault<string>(
                    "SELECT valor FROM ConfiguracionSistema WHERE clave = 'UmbralRendimientoMs'");

                if (valor != null && double.TryParse(valor, out var umbral))
                {
                    lock (_candado)
                    {
                        _umbralCacheado = umbral;
                        _umbralActualizado = DateTime.Now;
                    }
                }
            }
            catch { }

            return _umbralCacheado;
        }
    }
}
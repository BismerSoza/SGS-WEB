using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGSWC.API.Models;
using System.Data;

namespace SGSWC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicioController : ControllerBase
    {

        private readonly IConfiguration _configuration;

        public ServicioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPut]
        [Route("CambiarEstadoServicio")]
        public IActionResult CambiarEstadoServicio(CambiarEstadoServicioRequestModel model)
        {
            using (var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();

                parametros.Add("@id_reservacion", model.IdReservacion);
                parametros.Add("@id_estado_nuevo", model.IdEstadoNuevo);
                parametros.Add("@id_usuario", model.IdUsuarioResponsable);

                int resultado = context.ExecuteScalar<int>(
                    "CambiarEstadoServicio",
                    parametros,
                    commandType: CommandType.StoredProcedure);

                return Ok(resultado);
            }
        }

        [HttpGet]
        [Route("ConsultarServiciosConEstado")]
        public IActionResult ConsultarServiciosConEstado()
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var datos = context.Query<EstadoServicioResponseModel>(
                    "ConsultarServiciosConEstado",
                    commandType: CommandType.StoredProcedure);

                return Ok(datos);
            }
        }

        [HttpGet]
        [Route("ConsultarHistorialEstado")]
        public IActionResult ConsultarHistorialEstado(int idReservacion)
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();

                parametros.Add("@id_reservacion", idReservacion);

                var datos = context.Query<HistorialEstadoServicioResponseModel>(
                    "ConsultarHistorialEstadoServicio",
                    parametros,
                    commandType: CommandType.StoredProcedure);

                return Ok(datos);
            }
        }

        [HttpGet]
        [Route("ConsultarEstadosPermitidos")]
        public IActionResult ConsultarEstadosPermitidos(int idReservacion)
        {
            using (var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@id_reservacion", idReservacion);

                var datos = context.Query<EstadoReservaResponseModel>(
                    "ConsultarEstadosPermitidos",
                    parametros,
                    commandType: CommandType.StoredProcedure);

                return Ok(datos);
            }
        }
    }
}


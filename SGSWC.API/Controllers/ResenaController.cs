using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGSWC.API.Models;

namespace SGSWC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResenaController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ResenaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// HU-C-004 - Escenario 1, 2: registra una reseña si el comentario y la
        /// calificación son válidos y la reserva está Completada.
        /// Escenario 3 (no autenticado): cubierto por [Authorize] — sin un JWT
        /// válido, esta acción nunca se ejecuta y el cliente recibe 401.
        /// </summary>
        [Authorize]
        [HttpPost("RegistrarResena")]
        public IActionResult RegistrarResena(RegistrarResenaRequestModel modelo)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_reservacion", modelo.Id_Reservacion);
            p.Add("@id_usuario", modelo.Id_Usuario);
            p.Add("@calificacion", modelo.Calificacion);
            p.Add("@comentario", modelo.Comentario);

            var resultado = context.QueryFirstOrDefault<RegistrarResenaResultModel>(
                "RegistrarResena", p, commandType: System.Data.CommandType.StoredProcedure);

            if (resultado == null)
                return BadRequest("No se pudo procesar la reseña.");

            return resultado.Resultado switch
            {
                1 => Ok(resultado.Mensaje),
                -1 => BadRequest(resultado.Mensaje),   // Escenario 2: campos inválidos
                0 => NotFound(resultado.Mensaje),      // Reserva inexistente o ajena
                2 => BadRequest(resultado.Mensaje),    // No está Completada
                3 => Conflict(resultado.Mensaje),      // Ya existe reseña
                _ => BadRequest(resultado.Mensaje)
            };
        }

        [Authorize]
        [HttpGet("ConsultarPorServicio")]
        public IActionResult ConsultarPorServicio(int idServicio)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_servicio", idServicio);
            var resultado = context.Query<ResenaPorServicioResponseModel>(
                "ConsultarResenasPorServicio", p, commandType: System.Data.CommandType.StoredProcedure).ToList();
            return Ok(resultado);
        }

        [Authorize]
        [HttpGet("ConsultarPorUsuario")]
        public IActionResult ConsultarPorUsuario(int idUsuario)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_usuario", idUsuario);
            var resultado = context.Query<ResenaPorUsuarioResponseModel>(
                "ConsultarResenasPorUsuario", p, commandType: System.Data.CommandType.StoredProcedure).ToList();
            return Ok(resultado);
        }
    }
}
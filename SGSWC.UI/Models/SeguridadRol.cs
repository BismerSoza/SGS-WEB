using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SGSWC.UI.Models
{
    public class SeguridadRol : ActionFilterAttribute
    {
        private readonly int[] _rolesPermitidos;

        public SeguridadRol(params int[] roles)
        {
            _rolesPermitidos = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var idUsuario = context.HttpContext.Session.GetInt32("Id_Usuario");
            var idRol = context.HttpContext.Session.GetInt32("Id_Rol");

            if (idUsuario == null)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            else if (_rolesPermitidos.Length > 0 && !_rolesPermitidos.Contains(idRol ?? 0))
            {
                context.Result = new RedirectToActionResult("Index", "Inicio", null);
            }
            else
            {
                base.OnActionExecuting(context);
            }
        }
    }
}

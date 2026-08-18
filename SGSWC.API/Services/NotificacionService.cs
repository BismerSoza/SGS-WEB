using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class NotificacionService
{
    private readonly IConfiguration _configuration;

    public NotificacionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void EnviarNotificacionCambioEstado(
        string destinatario,
        string nombreCliente,
        string estadoAnterior,
        string estadoNuevo)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress("SGS Web Clean", _configuration["SMTP:Usuario"]));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = $"Actualización de tu servicio — {estadoNuevo}";

        mensaje.Body = new TextPart("html")
        {
            Text = ConstruirCuerpo(nombreCliente, estadoAnterior, estadoNuevo)
        };

        using var smtp = new SmtpClient();
        smtp.Connect(
            _configuration["SMTP:Host"],
            int.Parse(_configuration["SMTP:Port"]!),
            SecureSocketOptions.StartTls);
        smtp.Authenticate(_configuration["SMTP:Usuario"], _configuration["SMTP:Password"]);
        smtp.Send(mensaje);
        smtp.Disconnect(true);
    }

    private static string ConstruirCuerpo(string nombre, string estadoAnterior, string estadoNuevo)
    {
        string descripcion = estadoNuevo.ToLower() switch
        {
            "confirmada" => "Tu reservación ha sido <strong>confirmada</strong>. Nuestro equipo estará en tu domicilio en la fecha acordada.",
            "aceptado" => "Tu reservación ha sido <strong>aceptada</strong>. Pronto recibirás la confirmación final.",
            "completada" => "El servicio fue <strong>completado</strong> exitosamente. ¡Gracias por confiar en SGS Web Clean!",
            "cancelada" => "Tu reservación ha sido <strong>cancelada</strong>. Si tienes dudas, contáctanos.",
            "pendiente" => "Tu reservación está <strong>pendiente de revisión</strong>. Te notificaremos pronto.",
            _ => $"El estado de tu reservación cambió a <strong>{estadoNuevo}</strong>."
        };

        return $@"
<!DOCTYPE html>
<html lang='es'>
<body style='margin:0;padding:0;background:#f0f2f5;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr>
      <td align='center' style='padding:40px 20px;'>
        <table width='560' cellpadding='0' cellspacing='0'
               style='background:#fff;border-radius:10px;overflow:hidden;
                      box-shadow:0 2px 8px rgba(0,0,0,.08);'>
          <tr>
            <td style='background:#1565C0;padding:28px 32px;text-align:center;'>
              <h1 style='color:#fff;margin:0;font-size:22px;'>SGS Web Clean</h1>
              <p style='color:#90CAF9;margin:6px 0 0;font-size:13px;'>Sistema de Gestión de Servicios</p>
            </td>
          </tr>
          <tr>
            <td style='padding:32px;'>
              <p style='font-size:16px;color:#333;margin:0 0 12px;'>
                Hola, <strong>{nombre}</strong>:
              </p>
              <p style='font-size:15px;color:#555;line-height:1.6;margin:0 0 24px;'>
                {descripcion}
              </p>
              <table width='100%' cellpadding='0' cellspacing='0'
                     style='background:#F5F7FF;border-left:4px solid #1565C0;border-radius:4px;'>
                <tr>
                  <td style='padding:16px 20px;'>
                    <p style='margin:0 0 6px;font-size:13px;color:#666;'>
                      <strong>Estado anterior:</strong>&nbsp;{estadoAnterior}
                    </p>
                    <p style='margin:0;font-size:13px;color:#666;'>
                      <strong>Estado actual:</strong>&nbsp;{estadoNuevo}
                    </p>
                  </td>
                </tr>
              </table>
              <p style='font-size:13px;color:#999;margin:28px 0 0;'>
                Si tienes alguna pregunta, responde a este correo o comunícate con nosotros.
              </p>
            </td>
          </tr>
          <tr>
            <td style='background:#F5F5F5;padding:16px 32px;text-align:center;'>
              <p style='margin:0;font-size:12px;color:#BDBDBD;'>
                © 2026 SGS Web Clean — Todos los derechos reservados.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
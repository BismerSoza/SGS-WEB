using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void EnviarContrasenaTemporal(string destinatario, string contrasenaTemporal)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress("SGS WEB CLEAN", _configuration["SMTP:Usuario"]));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = "Tu contraseña temporal";

        mensaje.Body = new TextPart("html")
        {
            Text = $@"
                <div style='font-family:Arial; max-width:500px; margin:auto;'>
                    <h2 style='color:#007bff;'>Recuperación de contraseña</h2>
                    <p>Tu contraseña temporal es:</p>
                    <div style='background:#f4f4f4; padding:15px; font-size:24px; 
                                font-weight:bold; letter-spacing:3px; text-align:center;
                                border-radius:8px;'>
                        {contrasenaTemporal}
                    </div>
                    <p style='margin-top:20px;'>Al iniciar sesión se te pedirá 
                       <strong>cambiar tu contraseña</strong> inmediatamente.</p>
                    <p style='color:#999; font-size:12px;'>
                       Si no solicitaste esto, ignora este correo.
                    </p>
                </div>"
        };

        using var smtp = new SmtpClient();
        smtp.Connect(_configuration["SMTP:Host"],
                     int.Parse(_configuration["SMTP:Port"]),
                     SecureSocketOptions.StartTls);
        smtp.Authenticate(_configuration["SMTP:Usuario"],
                          _configuration["SMTP:Password"]);
        smtp.Send(mensaje);
        smtp.Disconnect(true);
    }
}
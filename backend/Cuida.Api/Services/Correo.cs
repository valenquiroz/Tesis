using System.Net;
using System.Net.Mail;

namespace Cuida.Api.Services;

// Envío de mails por SMTP. Se configura en appsettings.Local.json, sección "Correo":
// Servidor, Puerto, Usuario, Clave, Remitente y UsarSsl.
// Sin servidor configurado no se manda nada: en desarrollo el mensaje se escribe
// en la consola para poder probar el flujo sin una cuenta de correo.
public class Correo
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _entorno;
    private readonly ILogger<Correo> _logger;

    public Correo(IConfiguration configuration, IWebHostEnvironment entorno, ILogger<Correo> logger)
    {
        _configuration = configuration;
        _entorno = entorno;
        _logger = logger;
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_configuration["Correo:Servidor"]);

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        if (!Configurado)
        {
            if (_entorno.IsDevelopment())
            {
                _logger.LogWarning(
                    "No hay servidor de correo configurado. Mail para {Destinatario} - {Asunto}:\n{Cuerpo}",
                    destinatario, asunto, cuerpo);
            }
            else
            {
                _logger.LogError("No hay servidor de correo configurado: no se pudo enviar \"{Asunto}\".", asunto);
            }
            return;
        }

        var seccion = _configuration.GetSection("Correo");

        using var cliente = new SmtpClient(seccion["Servidor"], seccion.GetValue("Puerto", 587))
        {
            EnableSsl = seccion.GetValue("UsarSsl", true),
            Credentials = new NetworkCredential(seccion["Usuario"], seccion["Clave"]),
        };

        using var mensaje = new MailMessage(seccion["Remitente"] ?? seccion["Usuario"]!, destinatario, asunto, cuerpo);

        try
        {
            await cliente.SendMailAsync(mensaje);
        }
        catch (SmtpException ex)
        {
            // no se corta el pedido: al usuario se le responde igual
            _logger.LogError(ex, "No se pudo enviar el mail \"{Asunto}\".", asunto);
        }
    }
}

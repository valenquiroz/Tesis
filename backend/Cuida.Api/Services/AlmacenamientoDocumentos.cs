namespace Cuida.Api.Services;

// Si se pasa a Azure, esto se cambia por Blob Storage.
public class AlmacenamientoDocumentos
{
    public const long TamanoMaximoBytes = 10 * 1024 * 1024;

    // extensión → (tipo MIME, primeros bytes del archivo)
    private static readonly Dictionary<string, (string Tipo, byte[] Firma)> TiposPermitidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ("application/pdf", "%PDF"u8.ToArray()),
            [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
            [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
        };

    private readonly string _carpeta;
    private readonly ILogger<AlmacenamientoDocumentos> _logger;

    public AlmacenamientoDocumentos(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<AlmacenamientoDocumentos> logger)
    {
        var configurada = configuration["Almacenamiento:CarpetaDocumentos"];

        _carpeta = string.IsNullOrWhiteSpace(configurada)
            ? Path.Combine(environment.ContentRootPath, "Archivos", "Documentos")
            : Path.GetFullPath(configurada, environment.ContentRootPath);

        _logger = logger;
    }

    // devuelve el mensaje de error o null si está bien.
    // se revisan los primeros bytes porque el Content-Type lo manda el navegador
    public async Task<string?> ValidarAsync(IFormFile archivo)
    {
        if (archivo.Length == 0)
            return "El archivo está vacío.";

        if (archivo.Length > TamanoMaximoBytes)
            return "El archivo supera el tamaño máximo de 10 MB.";

        var extension = Path.GetExtension(archivo.FileName);

        if (!TiposPermitidos.TryGetValue(extension, out var tipo))
            return "Tipo de archivo no permitido. Solo se aceptan PDF, JPG y PNG.";

        var cabecera = new byte[tipo.Firma.Length];

        await using (var stream = archivo.OpenReadStream())
        {
            var leidos = await stream.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);

            if (leidos < cabecera.Length || !cabecera.SequenceEqual(tipo.Firma))
                return "El contenido del archivo no corresponde a un PDF, JPG o PNG válido.";
        }

        return null;
    }

    public static string TipoContenido(string nombreArchivo) =>
        TiposPermitidos.TryGetValue(Path.GetExtension(nombreArchivo), out var tipo)
            ? tipo.Tipo
            : "application/octet-stream";

    public async Task<(string NombreArchivo, string Ruta)> GuardarAsync(IFormFile archivo)
    {
        Directory.CreateDirectory(_carpeta);

        var nombreArchivo =
            $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName).ToLowerInvariant()}";

        var ruta = Path.Combine(_carpeta, nombreArchivo);

        await using var stream = new FileStream(ruta, FileMode.CreateNew);
        await archivo.CopyToAsync(stream);

        return (nombreArchivo, ruta);
    }

    public void Eliminar(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
            return;

        try
        {
            File.Delete(ruta);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar un archivo de documento.");
        }
    }
}

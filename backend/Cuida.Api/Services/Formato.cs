namespace Cuida.Api.Services;

public static class Formato
{
    // para que el JSON salga con Z
    public static DateTime Utc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

    public static DateTime? Utc(DateTime? fecha) => fecha.HasValue ? Utc(fecha.Value) : null;

    // "Lic. Mariana Ferreyra" → "Mariana Ferreyra"
    public static string SinTitulo(string nombre) =>
        System.Text.RegularExpressions.Regex.Replace(nombre.Trim(), @"^(Lic\.|Dra?\.|Prof\.)\s*", "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

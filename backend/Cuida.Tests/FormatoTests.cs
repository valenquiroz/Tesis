using Cuida.Api.Controllers;
using Cuida.Api.Data;
using Cuida.Api.Services;

namespace Cuida.Tests;

public class FormatoTests
{
    [Theory]
    [InlineData("8:00, 20:00 ,8:00", "08:00,20:00")]
    [InlineData("21:00 07:30", "07:30,21:00")]
    [InlineData("13:00", "13:00")]
    public void Normaliza_los_horarios(string entrada, string esperado)
    {
        Assert.Equal(esperado, MedicamentosController.NormalizarHorarios(entrada));
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("a la noche")]
    [InlineData("")]
    public void Rechaza_horarios_invalidos(string entrada)
    {
        Assert.Null(MedicamentosController.NormalizarHorarios(entrada));
    }

    [Theory]
    [InlineData("Lic. Mariana Ferreyra", "Mariana Ferreyra")]
    [InlineData("Dra. Silvina Etchegaray", "Silvina Etchegaray")]
    [InlineData("Dr. Ernesto Salvi", "Ernesto Salvi")]
    [InlineData("Graciela Ibarra", "Graciela Ibarra")]
    public void Quita_el_titulo_profesional(string nombre, string esperado)
    {
        Assert.Equal(esperado, Formato.SinTitulo(nombre));
    }

    [Theory]
    [InlineData("Natalia Benítez", "nataliabenitez@gmail.com", "Natalia123!")]
    [InlineData("Silvia Acuña", "silviaacuna@gmail.com", "Silvia123!")]
    [InlineData("Marta Suárez", "martasuarez@gmail.com", "Marta123!")]
    [InlineData("Lic. Andrea Molina", "andreamolina@gmail.com", "Andrea123!")]
    public void Las_cuentas_de_demo_siguen_la_regla(string nombre, string email, string clave)
    {
        Assert.Equal(email, DatosDemo.EmailDe(nombre));
        Assert.Equal(clave, DatosDemo.ClaveDe(nombre));
    }
}

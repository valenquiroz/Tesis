using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Cuida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly Correo _correo;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        Correo correo)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _correo = correo;
    }

    // Siempre responde lo mismo para no revelar qué correos están registrados
    [HttpPost("olvide")]
    public async Task<IActionResult> OlvideMiContrasena(OlvideRequest request)
    {
        var usuario = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (usuario != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var tokenUrl = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var frontend = (_configuration["Frontend:Url"] ?? "http://localhost:5173").TrimEnd('/');
            var enlace = $"{frontend}/?restablecer={tokenUrl}&email={Uri.EscapeDataString(usuario.Email!)}";

            await _correo.EnviarAsync(
                usuario.Email!,
                "Acompañar · Restablecer tu contraseña",
                $"Hola, {usuario.NombreCompleto}.\n\n" +
                $"Para elegir una contraseña nueva entrá a este enlace:\n{enlace}\n\n" +
                "El enlace vence en unas horas. Si no lo pediste vos, ignorá este mensaje: tu contraseña no cambia.");
        }

        return Ok(new
        {
            message = "Si el correo está registrado, te enviamos un enlace para restablecer la contraseña."
        });
    }

    [HttpPost("restablecer")]
    public async Task<IActionResult> Restablecer(RestablecerRequest request)
    {
        var usuario = await _userManager.FindByEmailAsync(request.Email.Trim());
        const string invalido = "El enlace no es válido o ya venció. Pedí uno nuevo.";

        if (usuario == null)
            return BadRequest(new { message = invalido });

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = invalido });
        }

        var resultado = await _userManager.ResetPasswordAsync(usuario, token, request.NuevaPassword);

        if (!resultado.Succeeded)
        {
            var esToken = resultado.Errors.Any(e => e.Code == "InvalidToken");
            return BadRequest(new
            {
                message = esToken ? invalido : "La contraseña no cumple los requisitos.",
                errors = esToken ? Array.Empty<string>() : resultado.Errors.Select(e => e.Description).ToArray()
            });
        }

        // si quedó bloqueada por intentos fallidos, se desbloquea
        await _userManager.SetLockoutEndDateAsync(usuario, null);
        await _userManager.ResetAccessFailedCountAsync(usuario);

        return Ok(new { message = "Listo, ya podés ingresar con tu contraseña nueva." });
    }

    [Authorize]
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarContrasena(CambiarPasswordRequest request)
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario == null) return Unauthorized();

        var resultado = await _userManager.ChangePasswordAsync(usuario, request.Actual, request.Nueva);

        if (!resultado.Succeeded)
        {
            var actualMal = resultado.Errors.Any(e => e.Code == "PasswordMismatch");
            return BadRequest(new
            {
                message = actualMal ? "La contraseña actual no es correcta." : "La contraseña nueva no cumple los requisitos.",
                errors = actualMal ? Array.Empty<string>() : resultado.Errors.Select(e => e.Description).ToArray()
            });
        }

        return Ok(new { message = "Contraseña actualizada." });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return BadRequest(new
            {
                message = "Ya existe un usuario con ese correo electrónico."
            });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NombreCompleto = request.NombreCompleto.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "No se pudo crear el usuario.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        await _userManager.AddToRoleAsync(user, "Cuidador");

        return Ok(new
        {
            message = "Usuario registrado correctamente."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }

        // con lockoutOnFailure se bloquea la cuenta tras 5 intentos
        var resultado = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            return Unauthorized(new
            {
                message = "La cuenta quedó bloqueada por varios intentos fallidos. Probá de nuevo en unos minutos."
            });
        }

        if (!resultado.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var token = GenerateToken(user, roles);

        return Ok(new
        {
            token,
            nombreCompleto = user.NombreCompleto,
            email = user.Email,
            rol = roles.Contains("Cuidador") ? "Cuidador"
                : roles.FirstOrDefault(),
            roles,
            plan = user.Plan,
            nombreGrupo = user.NombreGrupo
        });
    }

    private string GenerateToken(
        ApplicationUser user,
        IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.NombreCompleto)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiration = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue("Jwt:ExpirationMinutes", 60));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(
    [Required(ErrorMessage = "Ingresá tu nombre completo.")]
    [StringLength(150, ErrorMessage = "El nombre es demasiado largo.")]
    string NombreCompleto,

    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    string Email,

    [Required(ErrorMessage = "Ingresá una contraseña.")]
    string Password);

public record LoginRequest(
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    string Email,

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    string Password);

public record OlvideRequest(
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    string Email);

public record RestablecerRequest(
    [Required] string Email,
    [Required] string Token,
    [Required(ErrorMessage = "Ingresá la contraseña nueva.")] string NuevaPassword);

public record CambiarPasswordRequest(
    [Required(ErrorMessage = "Ingresá tu contraseña actual.")] string Actual,
    [Required(ErrorMessage = "Ingresá la contraseña nueva.")] string Nueva);

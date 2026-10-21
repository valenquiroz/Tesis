using Microsoft.AspNetCore.Identity;

namespace Cuida.Api.Services;

public class IdentityErroresEnEspanol : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Ocurrió un error inesperado.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), "Ya existe un usuario con ese correo electrónico.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), "Ya existe un usuario con ese correo electrónico.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), "El correo electrónico no es válido.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), "El nombre de usuario no es válido.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una minúscula.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una mayúscula.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric),
            "La contraseña debe incluir al menos un símbolo (por ejemplo ! # $ o .).");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars),
            $"La contraseña debe tener al menos {uniqueChars} caracteres distintos.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "La contraseña es incorrecta.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Error(nameof(UserAlreadyInRole), $"El usuario ya tiene el rol {role}.");

    public override IdentityError UserNotInRole(string role) =>
        Error(nameof(UserNotInRole), $"El usuario no tiene el rol {role}.");

    private static IdentityError Error(string codigo, string descripcion) =>
        new() { Code = codigo, Description = descripcion };
}

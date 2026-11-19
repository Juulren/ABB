using System.Globalization;
using Abb.Core.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Abb.Api.Controllers;

internal static class ValidacionValor
{
    /// <summary>Valida un valor recibido como texto. Devuelve un 400 con ProblemDetails si no es válido.</summary>
    public static ActionResult? Validar(ControllerBase c, string? texto, out int valor, string campo = "valor")
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto) ||
            !int.TryParse(texto, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out valor))
        {
            return c.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Valor no válido",
                detail: $"El {campo} debe ser un número entero.");
        }
        return Rango(c, valor, campo);
    }

    public static ActionResult? Validar(ControllerBase c, int? numero, out int valor, string campo = "value")
    {
        valor = numero ?? 0;
        if (numero is null)
        {
            return c.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Valor no válido",
                detail: $"Falta el campo '{campo}'.");
        }
        return Rango(c, valor, campo);
    }

    private static ActionResult? Rango(ControllerBase c, int valor, string campo) =>
        valor is < Limites.ValorMinimo or > Limites.ValorMaximo
            ? c.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Valor fuera de rango",
                detail: $"El {campo} debe estar entre {Limites.ValorMinimo} y {Limites.ValorMaximo}.")
            : null;
}

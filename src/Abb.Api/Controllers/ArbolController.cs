using Abb.Api.Dtos;
using Abb.Core.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Abb.Api.Controllers;

/// <summary>Consultas y acciones sobre el árbol completo.</summary>
[ApiController]
[Route("api/tree")]
public sealed class ArbolController : ControllerBase
{
    private readonly IArbolComandos _comandos;
    private readonly IArbolConsultas _consultas;

    public ArbolController(IArbolComandos comandos, IArbolConsultas consultas)
    {
        _comandos = comandos;
        _consultas = consultas;
    }

    // Operación 1
    [HttpGet]
    public ActionResult<TreeDto> Obtener() => Mapeo.Arbol(_consultas);

    [HttpGet("print")]
    public ActionResult<PrintResponse> Imprimir() => new PrintResponse(_consultas.Imprimir());

    // Operación 9
    [HttpGet("levels")]
    public ActionResult<LevelsResponse> Niveles()
    {
        var r = _consultas.Niveles();
        return new LevelsResponse(r.Niveles, r.PorNivel);
    }

    // Operación 11
    [HttpGet("extremes")]
    public ActionResult<ExtremesResponse> Extremos()
    {
        var r = _consultas.Extremos();
        return r is null
            ? Problem(statusCode: 404, title: "Árbol vacío", detail: "El árbol vacío no tiene mínimo ni máximo.")
            : new ExtremesResponse(r.Min, r.RutaMin, r.Max, r.RutaMax);
    }

    // Operación 12
    [HttpGet("stats")]
    public ActionResult<StatsResponse> Estadisticas()
    {
        var r = _consultas.Estadisticas();
        return new StatsResponse(r.Nodos, r.Hojas, r.Internos);
    }

    // Operación 14
    [HttpGet("balance")]
    public ActionResult<BalanceResponse> Balance()
    {
        var r = _consultas.Balance();
        return new BalanceResponse(r.Balanceado, r.Factores.Select(f => new BalanceFactorDto(f.Valor, f.Factor)).ToList());
    }

    // Operación 15
    [HttpGet("lca")]
    public ActionResult<LcaResponse> AncestroComun([FromQuery] string? a, [FromQuery] string? b)
    {
        if (ValidacionValor.Validar(this, a, out var va, "parámetro a") is { } e1) return e1;
        if (ValidacionValor.Validar(this, b, out var vb, "parámetro b") is { } e2) return e2;

        if (_consultas.Estadisticas().Nodos == 0)
            return Problem(statusCode: 404, title: "Árbol vacío", detail: "El árbol está vacío.");

        var r = _consultas.AncestroComun(va, vb);
        if (r.Valor is null)
            return Problem(statusCode: 404, title: "Nodo inexistente",
                detail: $"No existe en el árbol: {string.Join(", ", r.Faltantes)}.");
        return new LcaResponse(r.Valor.Value, r.RutaA, r.RutaB);
    }

    [HttpDelete]
    public ActionResult<TreeDto> Vaciar()
    {
        _comandos.Vaciar();
        return Mapeo.Arbol(_consultas);
    }

    [HttpPost("seed")]
    public ActionResult<TreeDto> Sembrar()
    {
        _comandos.Sembrar();
        return Mapeo.Arbol(_consultas);
    }
}

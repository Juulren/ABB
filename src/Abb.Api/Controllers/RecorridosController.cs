using Abb.Api.Dtos;
using Abb.Core.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Abb.Api.Controllers;

/// <summary>Recorridos (operaciones 5 a 8). Solo lee, así que depende únicamente de IArbolConsultas.</summary>
[ApiController]
[Route("api/tree/traversals")]
public sealed class RecorridosController : ControllerBase
{
    private readonly IArbolConsultas _consultas;

    public RecorridosController(IArbolConsultas consultas) => _consultas = consultas;

    [HttpGet("{nombre}")]
    public ActionResult<TraversalResponse> Recorrer(string nombre)
    {
        var valores = _consultas.Recorrer(nombre);
        return valores is null
            ? Problem(statusCode: 404, title: "Recorrido desconocido",
                detail: $"Recorridos disponibles: {string.Join(", ", _consultas.NombresRecorrido)}.")
            : new TraversalResponse(nombre.ToLowerInvariant(), valores);
    }
}

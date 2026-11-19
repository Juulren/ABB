using Abb.Api.Dtos;
using Abb.Core.Modelos;
using Abb.Core.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Abb.Api.Controllers;

/// <summary>CRUD de nodos y consultas sobre un nodo concreto.</summary>
[ApiController]
[Route("api/tree/nodes")]
public sealed class NodosController : ControllerBase
{
    private readonly IArbolComandos _comandos;
    private readonly IArbolConsultas _consultas;

    public NodosController(IArbolComandos comandos, IArbolConsultas consultas)
    {
        _comandos = comandos;
        _consultas = consultas;
    }

    // Create: operación 3
    [HttpPost]
    public ActionResult<InsertResponse> Insertar([FromBody] CreateNodeRequest peticion)
    {
        if (ValidacionValor.Validar(this, peticion.Value, out var valor) is { } error) return error;

        var r = _comandos.Insertar(valor);
        if (r.LimiteAlcanzado)
            return Problem(statusCode: 400, title: "Límite de nodos",
                detail: $"El árbol admite como máximo {Limites.MaximoNodos} nodos.");
        if (!r.Insertado)
            return Problem(statusCode: 409, title: "Valor duplicado", detail: $"El valor {valor} ya existe en el árbol.");

        return Created($"/api/tree/nodes/{valor}", new InsertResponse(Mapeo.Arbol(_consultas), r.Ruta));
    }

    // Read: operación 2
    [HttpGet("{valor}")]
    public ActionResult<SearchResponse> Buscar(string valor)
    {
        if (ValidacionValor.Validar(this, valor, out var v) is { } error) return error;
        var r = _consultas.Buscar(v);
        return new SearchResponse(r.Encontrado, r.Ruta, r.Nivel);
    }

    // Update: operación auxiliar para completar el CRUD
    [HttpPut("{valor}")]
    public ActionResult<UpdateResponse> Actualizar(string valor, [FromBody] UpdateNodeRequest peticion)
    {
        if (ValidacionValor.Validar(this, valor, out var actual) is { } e1) return e1;
        if (ValidacionValor.Validar(this, peticion.NewValue, out var nuevo, "newValue") is { } e2) return e2;

        return _comandos.Actualizar(actual, nuevo).Estado switch
        {
            EstadoActualizacion.NoExiste =>
                Problem(statusCode: 404, title: "Nodo inexistente", detail: $"El valor {actual} no existe en el árbol."),
            EstadoActualizacion.Duplicado =>
                Problem(statusCode: 409, title: "Valor duplicado", detail: $"El valor {nuevo} ya existe en el árbol."),
            _ => new UpdateResponse(Mapeo.Arbol(_consultas)),
        };
    }

    // Delete: operación 4
    [HttpDelete("{valor}")]
    public ActionResult<DeleteResponse> Borrar(string valor)
    {
        if (ValidacionValor.Validar(this, valor, out var v) is { } error) return error;
        var r = _comandos.Borrar(v);
        if (!r.Borrado)
            return Problem(statusCode: 404, title: "Nodo inexistente", detail: $"El valor {v} no existe en el árbol.");
        return new DeleteResponse(Mapeo.Arbol(_consultas), r.Caso, r.Sucesor, r.RutaSucesor, r.Ruta);
    }

    // Operación 10
    [HttpGet("{valor}/level")]
    public ActionResult<LevelResponse> Nivel(string valor)
    {
        if (ValidacionValor.Validar(this, valor, out var v) is { } error) return error;
        var nivel = _consultas.Nivel(v);
        return nivel is null
            ? Problem(statusCode: 404, title: "Nodo inexistente", detail: $"El valor {v} no existe en el árbol.")
            : new LevelResponse(v, nivel.Value);
    }

    // Operación 13
    [HttpGet("{valor}/ancestors")]
    public ActionResult<AncestorsResponse> Ancestros(string valor)
    {
        if (ValidacionValor.Validar(this, valor, out var v) is { } error) return error;
        var ancestros = _consultas.Ancestros(v);
        return ancestros is null
            ? Problem(statusCode: 404, title: "Nodo inexistente", detail: $"El valor {v} no existe en el árbol.")
            : new AncestorsResponse(v, ancestros);
    }
}

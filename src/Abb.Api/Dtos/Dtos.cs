using Abb.Core.Modelos;
using Abb.Core.Servicios;

namespace Abb.Api.Dtos;

// "Vista" del servidor: lo que sale al cliente. Los nombres JSON coinciden con el contrato del plan.

public sealed record NodeDto(int Value, int? Parent, string? Side, int Level, int InorderIndex);
public sealed record TreeDto(IReadOnlyList<NodeDto> Nodes, int Count);

public sealed record InsertResponse(TreeDto Tree, IReadOnlyList<int> Path);
public sealed record SearchResponse(bool Found, IReadOnlyList<int> Path, int? Level);
public sealed record DeleteResponse(
    TreeDto Tree, string Case, int? Successor, IReadOnlyList<int> SuccessorPath, IReadOnlyList<int> Path);
public sealed record UpdateResponse(TreeDto Tree);
public sealed record LevelsResponse(int Levels, IReadOnlyList<int> PerLevel);
public sealed record LevelResponse(int Value, int Level);
public sealed record ExtremesResponse(int Min, IReadOnlyList<int> MinPath, int Max, IReadOnlyList<int> MaxPath);
public sealed record StatsResponse(int Nodes, int Leaves, int InternalNodes);
public sealed record AncestorsResponse(int Value, IReadOnlyList<int> Ancestors);
public sealed record BalanceFactorDto(int Value, int Factor);
public sealed record BalanceResponse(bool Balanced, IReadOnlyList<BalanceFactorDto> Factors);
public sealed record LcaResponse(int Value, IReadOnlyList<int> PathA, IReadOnlyList<int> PathB);
public sealed record TraversalResponse(string Name, IReadOnlyList<int> Values);
public sealed record PrintResponse(string Text);

public sealed record CreateNodeRequest(int? Value);
public sealed record UpdateNodeRequest(int? NewValue);

internal static class Mapeo
{
    public static TreeDto Arbol(IArbolConsultas consultas)
    {
        var nodos = consultas.ObtenerNodos();
        return new TreeDto(
            nodos.Select(n => new NodeDto(n.Valor, n.Padre, n.Lado, n.Nivel, n.IndiceInorden)).ToList(),
            nodos.Count);
    }
}

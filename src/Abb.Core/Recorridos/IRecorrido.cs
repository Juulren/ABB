using Abb.Core.Modelos;

namespace Abb.Core.Recorridos;

/// <summary>Estrategia de recorrido. Un recorrido nuevo es una clase nueva, sin tocar las demás.</summary>
public interface IRecorrido
{
    string Nombre { get; }
    IReadOnlyList<int> Recorrer(Nodo? raiz);
}

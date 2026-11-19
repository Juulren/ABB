namespace Abb.Core.Modelos;

/// <summary>Nodo del árbol. Solo el modelo puede modificar sus enlaces.</summary>
public sealed class Nodo
{
    public int Valor { get; internal set; }
    public Nodo? Izquierdo { get; internal set; }
    public Nodo? Derecho { get; internal set; }

    internal Nodo(int valor) => Valor = valor;
}

using Abb.Core.Modelos;

namespace Abb.Core.Recorridos;

public sealed class Preorden : IRecorrido
{
    public string Nombre => "preorder";

    public IReadOnlyList<int> Recorrer(Nodo? raiz)
    {
        var resultado = new List<int>();
        var pila = new Stack<Nodo>();
        if (raiz is not null) pila.Push(raiz);
        while (pila.Count > 0)
        {
            var n = pila.Pop();
            resultado.Add(n.Valor);
            if (n.Derecho is not null) pila.Push(n.Derecho);
            if (n.Izquierdo is not null) pila.Push(n.Izquierdo);
        }
        return resultado;
    }
}

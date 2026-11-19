using Abb.Core.Modelos;

namespace Abb.Core.Recorridos;

public sealed class Postorden : IRecorrido
{
    public string Nombre => "postorder";

    public IReadOnlyList<int> Recorrer(Nodo? raiz)
    {
        // Raíz, derecho, izquierdo con una pila; al invertir queda izquierdo, derecho, raíz.
        var resultado = new List<int>();
        var pila = new Stack<Nodo>();
        if (raiz is not null) pila.Push(raiz);
        while (pila.Count > 0)
        {
            var n = pila.Pop();
            resultado.Add(n.Valor);
            if (n.Izquierdo is not null) pila.Push(n.Izquierdo);
            if (n.Derecho is not null) pila.Push(n.Derecho);
        }
        resultado.Reverse();
        return resultado;
    }
}

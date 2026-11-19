using Abb.Core.Modelos;

namespace Abb.Core.Recorridos;

public sealed class Inorden : IRecorrido
{
    public string Nombre => "inorder";

    public IReadOnlyList<int> Recorrer(Nodo? raiz)
    {
        var resultado = new List<int>();
        var pila = new Stack<Nodo>();
        var actual = raiz;
        while (actual is not null || pila.Count > 0)
        {
            while (actual is not null)
            {
                pila.Push(actual);
                actual = actual.Izquierdo;
            }
            actual = pila.Pop();
            resultado.Add(actual.Valor);
            actual = actual.Derecho;
        }
        return resultado;
    }
}

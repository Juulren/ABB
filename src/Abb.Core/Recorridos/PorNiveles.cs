using Abb.Core.Modelos;

namespace Abb.Core.Recorridos;

public sealed class PorNiveles : IRecorrido
{
    public string Nombre => "levelorder";

    public IReadOnlyList<int> Recorrer(Nodo? raiz)
    {
        var resultado = new List<int>();
        var cola = new Queue<Nodo>();
        if (raiz is not null) cola.Enqueue(raiz);
        while (cola.Count > 0)
        {
            var n = cola.Dequeue();
            resultado.Add(n.Valor);
            if (n.Izquierdo is not null) cola.Enqueue(n.Izquierdo);
            if (n.Derecho is not null) cola.Enqueue(n.Derecho);
        }
        return resultado;
    }
}

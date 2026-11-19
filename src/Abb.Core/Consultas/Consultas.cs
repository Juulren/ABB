using System.Text;
using Abb.Core.Modelos;

namespace Abb.Core.Consultas;

/// <summary>Cada consulta es una clase pequeña y sin estado que lee el árbol sin modificarlo.</summary>
internal sealed class NivelesConsulta
{
    public NivelesResultado Ejecutar(Nodo? raiz)
    {
        var porNivel = new List<int>();
        var actual = new List<Nodo>();
        if (raiz is not null) actual.Add(raiz);
        while (actual.Count > 0)
        {
            porNivel.Add(actual.Count);
            var siguiente = new List<Nodo>();
            foreach (var n in actual)
            {
                if (n.Izquierdo is not null) siguiente.Add(n.Izquierdo);
                if (n.Derecho is not null) siguiente.Add(n.Derecho);
            }
            actual = siguiente;
        }
        return new NivelesResultado(porNivel.Count, porNivel);
    }
}

internal sealed class ExtremosConsulta
{
    public ExtremosResultado? Ejecutar(Nodo? raiz)
    {
        if (raiz is null) return null;
        var rutaMin = new List<int>();
        var n = raiz;
        while (true)
        {
            rutaMin.Add(n.Valor);
            if (n.Izquierdo is null) break;
            n = n.Izquierdo;
        }
        var rutaMax = new List<int>();
        n = raiz;
        while (true)
        {
            rutaMax.Add(n.Valor);
            if (n.Derecho is null) break;
            n = n.Derecho;
        }
        return new ExtremosResultado(rutaMin[^1], rutaMin, rutaMax[^1], rutaMax);
    }
}

internal sealed class EstadisticasConsulta
{
    public EstadisticasResultado Ejecutar(Nodo? raiz)
    {
        int nodos = 0, hojas = 0;
        var pila = new Stack<Nodo>();
        if (raiz is not null) pila.Push(raiz);
        while (pila.Count > 0)
        {
            var n = pila.Pop();
            nodos++;
            if (n.Izquierdo is null && n.Derecho is null) hojas++;
            if (n.Izquierdo is not null) pila.Push(n.Izquierdo);
            if (n.Derecho is not null) pila.Push(n.Derecho);
        }
        return new EstadisticasResultado(nodos, hojas, nodos - hojas);
    }
}

internal sealed class AncestrosConsulta
{
    /// <summary>Camino de la raíz al padre. Nulo si el nodo no existe, vacío si es la raíz.</summary>
    public IReadOnlyList<int>? Ejecutar(Nodo? raiz, int valor)
    {
        var ruta = new List<int>();
        var actual = raiz;
        while (actual is not null)
        {
            if (actual.Valor == valor) return ruta;
            ruta.Add(actual.Valor);
            actual = valor < actual.Valor ? actual.Izquierdo : actual.Derecho;
        }
        return null;
    }
}

internal sealed class BalanceConsulta
{
    /// <summary>Definición AVL en un solo recorrido postorden: la diferencia de alturas es como máximo 1 en cada nodo.</summary>
    public BalanceResultado Ejecutar(Nodo? raiz)
    {
        var factores = new List<FactorNodo>();
        var balanceado = true;
        var alturas = new Dictionary<Nodo, int>();
        var pila = new Stack<(Nodo Nodo, bool Visitado)>();
        if (raiz is not null) pila.Push((raiz, false));
        while (pila.Count > 0)
        {
            var (n, visitado) = pila.Pop();
            if (!visitado)
            {
                pila.Push((n, true));
                if (n.Derecho is not null) pila.Push((n.Derecho, false));
                if (n.Izquierdo is not null) pila.Push((n.Izquierdo, false));
                continue;
            }
            var hi = n.Izquierdo is null ? 0 : alturas[n.Izquierdo];
            var hd = n.Derecho is null ? 0 : alturas[n.Derecho];
            alturas[n] = 1 + Math.Max(hi, hd);
            var factor = hi - hd;
            if (Math.Abs(factor) > 1) balanceado = false;
            factores.Add(new FactorNodo(n.Valor, factor));
        }
        return new BalanceResultado(balanceado, factores);
    }
}

internal sealed class LcaConsulta
{
    /// <param name="rutaDe">Devuelve el camino de la raíz al nodo (incluido), o nulo si no existe.</param>
    public LcaResultado Ejecutar(Nodo? raiz, int a, int b, Func<int, IReadOnlyList<int>?> rutaDe)
    {
        var rutaA = rutaDe(a);
        var rutaB = rutaDe(b);
        var faltantes = new List<int>();
        if (rutaA is null) faltantes.Add(a);
        if (rutaB is null && b != a) faltantes.Add(b);
        if (faltantes.Count > 0)
            return new LcaResultado(null, rutaA ?? Array.Empty<int>(), rutaB ?? Array.Empty<int>(), faltantes);

        var n = raiz!;
        while (true)
        {
            if (a < n.Valor && b < n.Valor && n.Izquierdo is not null) n = n.Izquierdo;
            else if (a > n.Valor && b > n.Valor && n.Derecho is not null) n = n.Derecho;
            else break;
        }
        return new LcaResultado(n.Valor, rutaA!, rutaB!, Array.Empty<int>());
    }
}

internal sealed class AplanadoConsulta
{
    public IReadOnlyList<NodoPlano> Ejecutar(Nodo? raiz)
    {
        var resultado = new List<NodoPlano>();
        var pila = new Stack<(Nodo Nodo, int? Padre, string? Lado, int Nivel)>();
        var actual = raiz;
        int? padre = null;
        string? lado = null;
        var nivel = 1;
        var indice = 0;
        while (actual is not null || pila.Count > 0)
        {
            while (actual is not null)
            {
                pila.Push((actual, padre, lado, nivel));
                padre = actual.Valor;
                lado = "left";
                nivel++;
                actual = actual.Izquierdo;
            }
            var (n, p, l, nv) = pila.Pop();
            resultado.Add(new NodoPlano(n.Valor, p, l, nv, indice++));
            actual = n.Derecho;
            padre = n.Valor;
            lado = "right";
            nivel = nv + 1;
        }
        return resultado;
    }
}

internal sealed class ImpresionConsulta
{
    /// <summary>Árbol girado a la izquierda: el hijo derecho arriba y el izquierdo abajo, con sangría por nivel.</summary>
    public string Ejecutar(Nodo? raiz)
    {
        if (raiz is null) return "(árbol vacío)";
        var sb = new StringBuilder();
        var pila = new Stack<(Nodo Nodo, int Nivel)>();
        var actual = raiz;
        var nivel = 0;
        while (actual is not null || pila.Count > 0)
        {
            while (actual is not null)
            {
                pila.Push((actual, nivel));
                actual = actual.Derecho;
                nivel++;
            }
            var (n, nv) = pila.Pop();
            sb.Append(' ', nv * 4).Append(n.Valor).AppendLine();
            actual = n.Izquierdo;
            nivel = nv + 1;
        }
        return sb.ToString().TrimEnd();
    }
}

using Abb.Core.Modelos;

namespace Abb.Core.Estructura;

/// <summary>
/// Mantiene solo la estructura del ABB: insertar, borrar, buscar y vaciar.
/// No tiene candados ni cálculos de consulta.
/// </summary>
public sealed class ArbolBinarioBusqueda
{
    internal Nodo? Raiz { get; private set; }
    public int Total { get; private set; }

    public InsercionResultado Insertar(int valor)
    {
        var ruta = new List<int>();
        if (Raiz is null)
        {
            Raiz = new Nodo(valor);
            Total++;
            ruta.Add(valor);
            return new InsercionResultado(true, ruta);
        }

        var actual = Raiz;
        while (true)
        {
            ruta.Add(actual.Valor);
            if (valor == actual.Valor)
                return new InsercionResultado(false, ruta);

            if (valor < actual.Valor)
            {
                if (actual.Izquierdo is null)
                {
                    actual.Izquierdo = new Nodo(valor);
                    break;
                }
                actual = actual.Izquierdo;
            }
            else
            {
                if (actual.Derecho is null)
                {
                    actual.Derecho = new Nodo(valor);
                    break;
                }
                actual = actual.Derecho;
            }
        }

        Total++;
        ruta.Add(valor);
        return new InsercionResultado(true, ruta);
    }

    public BusquedaResultado Buscar(int valor)
    {
        var ruta = new List<int>();
        var actual = Raiz;
        while (actual is not null)
        {
            ruta.Add(actual.Valor);
            if (valor == actual.Valor)
                return new BusquedaResultado(true, ruta, ruta.Count);
            actual = valor < actual.Valor ? actual.Izquierdo : actual.Derecho;
        }
        return new BusquedaResultado(false, ruta, null);
    }

    public BorradoResultado Borrar(int valor)
    {
        var ruta = new List<int>();
        Nodo? padre = null;
        var actual = Raiz;
        while (actual is not null && actual.Valor != valor)
        {
            ruta.Add(actual.Valor);
            padre = actual;
            actual = valor < actual.Valor ? actual.Izquierdo : actual.Derecho;
        }

        if (actual is null)
            return new BorradoResultado(false, "", null, Array.Empty<int>(), ruta);

        ruta.Add(actual.Valor);
        var vacia = Array.Empty<int>();

        if (actual.Izquierdo is not null && actual.Derecho is not null)
        {
            // Dos hijos: el sucesor inorden es el mínimo del subárbol derecho.
            var rutaSucesor = new List<int> { actual.Valor };
            var padreSucesor = actual;
            var sucesor = actual.Derecho;
            rutaSucesor.Add(sucesor.Valor);
            while (sucesor.Izquierdo is not null)
            {
                padreSucesor = sucesor;
                sucesor = sucesor.Izquierdo;
                rutaSucesor.Add(sucesor.Valor);
            }

            actual.Valor = sucesor.Valor;
            // El sucesor no tiene hijo izquierdo; su hijo derecho (si existe) sube a su lugar.
            if (padreSucesor == actual) padreSucesor.Derecho = sucesor.Derecho;
            else padreSucesor.Izquierdo = sucesor.Derecho;

            Total--;
            return new BorradoResultado(true, "dosHijos", actual.Valor, rutaSucesor, ruta);
        }

        var hijo = actual.Izquierdo ?? actual.Derecho;
        var caso = hijo is null ? "hoja" : "unHijo";
        if (padre is null) Raiz = hijo;
        else if (padre.Izquierdo == actual) padre.Izquierdo = hijo;
        else padre.Derecho = hijo;

        Total--;
        return new BorradoResultado(true, caso, null, vacia, ruta);
    }

    public void Vaciar()
    {
        Raiz = null;
        Total = 0;
    }
}

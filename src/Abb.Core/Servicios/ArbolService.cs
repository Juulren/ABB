using Abb.Core.Consultas;
using Abb.Core.Estructura;
using Abb.Core.Modelos;
using Abb.Core.Recorridos;

namespace Abb.Core.Servicios;

/// <summary>
/// Coordina el árbol, las estrategias de recorrido y la concurrencia.
/// Las lecturas toman el candado de lectura y las escrituras el de escritura.
/// </summary>
public sealed class ArbolService : IArbolComandos, IArbolConsultas
{
    private static readonly int[] Ejemplo = { 50, 30, 70, 20, 40, 60, 80, 35, 45, 65 };

    private readonly ArbolBinarioBusqueda _arbol;
    private readonly Dictionary<string, IRecorrido> _recorridos;
    private readonly ReaderWriterLockSlim _candado = new();

    private readonly NivelesConsulta _niveles = new();
    private readonly ExtremosConsulta _extremos = new();
    private readonly EstadisticasConsulta _estadisticas = new();
    private readonly AncestrosConsulta _ancestros = new();
    private readonly BalanceConsulta _balance = new();
    private readonly LcaConsulta _lca = new();
    private readonly AplanadoConsulta _aplanado = new();
    private readonly ImpresionConsulta _impresion = new();

    public ArbolService(ArbolBinarioBusqueda arbol, IEnumerable<IRecorrido> recorridos)
    {
        _arbol = arbol;
        _recorridos = recorridos.ToDictionary(r => r.Nombre, StringComparer.OrdinalIgnoreCase);
    }

    // ---- Comandos ----

    public InsercionResultado Insertar(int valor)
    {
        _candado.EnterWriteLock();
        try
        {
            if (_arbol.Total >= Limites.MaximoNodos && !_arbol.Buscar(valor).Encontrado)
                return new InsercionResultado(false, Array.Empty<int>(), LimiteAlcanzado: true);
            return _arbol.Insertar(valor);
        }
        finally { _candado.ExitWriteLock(); }
    }

    public BorradoResultado Borrar(int valor)
    {
        _candado.EnterWriteLock();
        try { return _arbol.Borrar(valor); }
        finally { _candado.ExitWriteLock(); }
    }

    public ActualizacionResultado Actualizar(int actual, int nuevo)
    {
        _candado.EnterWriteLock();
        try
        {
            if (!_arbol.Buscar(actual).Encontrado)
                return new ActualizacionResultado(EstadoActualizacion.NoExiste);
            if (actual == nuevo)
                return new ActualizacionResultado(EstadoActualizacion.Actualizado);
            if (_arbol.Buscar(nuevo).Encontrado)
                return new ActualizacionResultado(EstadoActualizacion.Duplicado);
            _arbol.Borrar(actual);
            _arbol.Insertar(nuevo);
            return new ActualizacionResultado(EstadoActualizacion.Actualizado);
        }
        finally { _candado.ExitWriteLock(); }
    }

    public void Vaciar()
    {
        _candado.EnterWriteLock();
        try { _arbol.Vaciar(); }
        finally { _candado.ExitWriteLock(); }
    }

    public void Sembrar()
    {
        _candado.EnterWriteLock();
        try
        {
            _arbol.Vaciar();
            foreach (var v in Ejemplo) _arbol.Insertar(v);
        }
        finally { _candado.ExitWriteLock(); }
    }

    // ---- Consultas ----

    public BusquedaResultado Buscar(int valor) => Leer(() => _arbol.Buscar(valor));

    public IReadOnlyList<string> NombresRecorrido => _recorridos.Keys.ToList();

    public IReadOnlyList<int>? Recorrer(string nombre) =>
        _recorridos.TryGetValue(nombre, out var r) ? Leer(() => r.Recorrer(_arbol.Raiz)) : null;

    public NivelesResultado Niveles() => Leer(() => _niveles.Ejecutar(_arbol.Raiz));

    public int? Nivel(int valor) => Leer(() => _arbol.Buscar(valor).Nivel);

    public ExtremosResultado? Extremos() => Leer(() => _extremos.Ejecutar(_arbol.Raiz));

    public EstadisticasResultado Estadisticas() => Leer(() => _estadisticas.Ejecutar(_arbol.Raiz));

    public IReadOnlyList<int>? Ancestros(int valor) => Leer(() => _ancestros.Ejecutar(_arbol.Raiz, valor));

    public BalanceResultado Balance() => Leer(() => _balance.Ejecutar(_arbol.Raiz));

    public LcaResultado AncestroComun(int a, int b) => Leer(() =>
        _lca.Ejecutar(_arbol.Raiz, a, b, v =>
        {
            var r = _arbol.Buscar(v);
            return r.Encontrado ? r.Ruta : null;
        }));

    public IReadOnlyList<NodoPlano> ObtenerNodos() => Leer(() => _aplanado.Ejecutar(_arbol.Raiz));

    public string Imprimir() => Leer(() => _impresion.Ejecutar(_arbol.Raiz));

    private T Leer<T>(Func<T> lectura)
    {
        _candado.EnterReadLock();
        try { return lectura(); }
        finally { _candado.ExitReadLock(); }
    }
}

namespace Abb.Core.Modelos;

public sealed record InsercionResultado(bool Insertado, IReadOnlyList<int> Ruta, bool LimiteAlcanzado = false);

public sealed record BusquedaResultado(bool Encontrado, IReadOnlyList<int> Ruta, int? Nivel);

/// <param name="Caso">"hoja", "unHijo" o "dosHijos"; vacío si el valor no existe.</param>
/// <param name="Sucesor">Valor del sucesor inorden cuando el caso es "dosHijos".</param>
/// <param name="RutaSucesor">Camino desde el nodo objetivo hasta el sucesor, ambos incluidos.</param>
public sealed record BorradoResultado(
    bool Borrado,
    string Caso,
    int? Sucesor,
    IReadOnlyList<int> RutaSucesor,
    IReadOnlyList<int> Ruta);

public enum EstadoActualizacion { Actualizado, NoExiste, Duplicado }

public sealed record ActualizacionResultado(EstadoActualizacion Estado);

public sealed record NivelesResultado(int Niveles, IReadOnlyList<int> PorNivel);

public sealed record ExtremosResultado(int Min, IReadOnlyList<int> RutaMin, int Max, IReadOnlyList<int> RutaMax);

public sealed record EstadisticasResultado(int Nodos, int Hojas, int Internos);

public sealed record FactorNodo(int Valor, int Factor);

public sealed record BalanceResultado(bool Balanceado, IReadOnlyList<FactorNodo> Factores);

/// <param name="Valor">Ancestro común más bajo; nulo si falta algún nodo.</param>
/// <param name="Faltantes">Valores que no existen en el árbol.</param>
public sealed record LcaResultado(
    int? Valor,
    IReadOnlyList<int> RutaA,
    IReadOnlyList<int> RutaB,
    IReadOnlyList<int> Faltantes);

/// <summary>Nodo en forma plana, listo para dibujar. El nivel de la raíz es 1.</summary>
public sealed record NodoPlano(int Valor, int? Padre, string? Lado, int Nivel, int IndiceInorden);

using Abb.Core.Estructura;
using Abb.Core.Modelos;
using Abb.Core.Recorridos;
using Abb.Core.Servicios;

namespace Abb.Tests;

internal static class Fabrica
{
    public static IRecorrido[] Recorridos() =>
        new IRecorrido[] { new Preorden(), new Inorden(), new Postorden(), new PorNiveles() };

    public static ArbolService Servicio() => new(new ArbolBinarioBusqueda(), Recorridos());

    /// <summary>Árbol de ejemplo: 50, 30, 70, 20, 40, 60, 80, 35, 45, 65.</summary>
    public static ArbolService Sembrado()
    {
        var s = Servicio();
        s.Sembrar();
        return s;
    }

    public static ArbolService Con(params int[] valores)
    {
        var s = Servicio();
        foreach (var v in valores) s.Insertar(v);
        return s;
    }
}

public class InsertarYBuscarTests
{
    [Fact]
    public void Insertar_devuelve_la_ruta_hasta_el_nuevo_nodo()
    {
        var s = Fabrica.Sembrado();
        var r = s.Insertar(55);
        Assert.True(r.Insertado);
        Assert.Equal(new[] { 50, 70, 60, 55 }, r.Ruta);
    }

    [Fact]
    public void Insertar_duplicado_no_cambia_el_arbol()
    {
        var s = Fabrica.Sembrado();
        var r = s.Insertar(40);
        Assert.False(r.Insertado);
        Assert.Equal(10, s.Estadisticas().Nodos);
    }

    [Fact]
    public void Buscar_existente_da_ruta_y_nivel()
    {
        var r = Fabrica.Sembrado().Buscar(45);
        Assert.True(r.Encontrado);
        Assert.Equal(new[] { 50, 30, 40, 45 }, r.Ruta);
        Assert.Equal(4, r.Nivel);
    }

    [Fact]
    public void Buscar_inexistente_da_la_ruta_recorrida_y_sin_nivel()
    {
        var r = Fabrica.Sembrado().Buscar(99);
        Assert.False(r.Encontrado);
        Assert.Null(r.Nivel);
        Assert.Equal(new[] { 50, 70, 80 }, r.Ruta);
    }

    [Fact]
    public void Arbol_vacio_busca_sin_error()
    {
        var r = Fabrica.Servicio().Buscar(1);
        Assert.False(r.Encontrado);
        Assert.Empty(r.Ruta);
    }

    [Fact]
    public void Se_rechaza_el_nodo_numero_1001()
    {
        var s = Fabrica.Servicio();
        for (var v = -499; v <= 500; v++) Assert.True(s.Insertar(v).Insertado);
        var r = s.Insertar(600);
        Assert.False(r.Insertado);
        Assert.True(r.LimiteAlcanzado);
    }
}

public class BorrarTests
{
    [Fact]
    public void Borrar_hoja()
    {
        var s = Fabrica.Sembrado();
        var r = s.Borrar(35);
        Assert.True(r.Borrado);
        Assert.Equal("hoja", r.Caso);
        Assert.DoesNotContain(35, s.Recorrer("inorder")!);
    }

    [Fact]
    public void Borrar_nodo_con_un_hijo()
    {
        var s = Fabrica.Sembrado();
        var r = s.Borrar(60);               // 60 tiene solo el hijo derecho 65
        Assert.Equal("unHijo", r.Caso);
        Assert.Equal(new[] { 20, 30, 35, 40, 45, 50, 65, 70, 80 }, s.Recorrer("inorder"));
    }

    [Fact]
    public void Borrar_nodo_con_dos_hijos_usa_el_sucesor_inorden()
    {
        var s = Fabrica.Sembrado();
        var r = s.Borrar(70);
        Assert.Equal("dosHijos", r.Caso);
        Assert.Equal(80, r.Sucesor);
        Assert.Equal(new[] { 70, 80 }, r.RutaSucesor);
        Assert.Equal(new[] { 20, 30, 35, 40, 45, 50, 60, 65, 80 }, s.Recorrer("inorder"));
    }

    [Fact]
    public void Borrar_la_raiz_con_dos_hijos()
    {
        var s = Fabrica.Sembrado();
        var r = s.Borrar(50);
        Assert.Equal("dosHijos", r.Caso);
        Assert.Equal(60, r.Sucesor);
        Assert.Equal(new[] { 50, 70, 60 }, r.RutaSucesor);
        // 60 tenia un hijo derecho (65) que debe subir.
        Assert.Equal(new[] { 20, 30, 35, 40, 45, 60, 65, 70, 80 }, s.Recorrer("inorder"));
        Assert.Equal(60, s.Recorrer("preorder")![0]);
    }

    [Fact]
    public void Borrar_cuando_el_sucesor_tiene_hijo_derecho_lo_reconecta()
    {
        var s = Fabrica.Sembrado();
        s.Borrar(50);                       // sucesor 60 con hijo derecho 65
        Assert.Equal(new[] { 60, 30, 20, 40, 35, 45, 70, 65, 80 }, s.Recorrer("preorder"));
    }

    [Fact]
    public void Borrar_la_raiz_unica_deja_el_arbol_vacio()
    {
        var s = Fabrica.Con(5);
        var r = s.Borrar(5);
        Assert.Equal("hoja", r.Caso);
        Assert.Equal(0, s.Niveles().Niveles);
    }

    [Fact]
    public void Borrar_inexistente_no_cambia_nada()
    {
        var s = Fabrica.Sembrado();
        var r = s.Borrar(999);
        Assert.False(r.Borrado);
        Assert.Equal(10, s.Estadisticas().Nodos);
    }
}

public class RecorridoTests
{
    [Fact]
    public void Preorden() =>
        Assert.Equal(new[] { 50, 30, 20, 40, 35, 45, 70, 60, 65, 80 }, Fabrica.Sembrado().Recorrer("preorder"));

    [Fact]
    public void Inorden_sale_ordenado() =>
        Assert.Equal(new[] { 20, 30, 35, 40, 45, 50, 60, 65, 70, 80 }, Fabrica.Sembrado().Recorrer("inorder"));

    [Fact]
    public void Postorden() =>
        Assert.Equal(new[] { 20, 35, 45, 40, 30, 65, 60, 80, 70, 50 }, Fabrica.Sembrado().Recorrer("postorder"));

    [Fact]
    public void Por_niveles() =>
        Assert.Equal(new[] { 50, 30, 70, 20, 40, 60, 80, 35, 45, 65 }, Fabrica.Sembrado().Recorrer("levelorder"));

    [Fact]
    public void Recorrido_desconocido_devuelve_nulo() =>
        Assert.Null(Fabrica.Sembrado().Recorrer("zigzag"));

    [Fact]
    public void Arbol_vacio_devuelve_lista_vacia()
    {
        foreach (var nombre in Fabrica.Servicio().NombresRecorrido)
            Assert.Empty(Fabrica.Servicio().Recorrer(nombre)!);
    }

    [Fact]
    public void Arbol_degenerado_de_1000_nodos_no_desborda()
    {
        var s = Fabrica.Servicio();
        for (var v = -499; v <= 500; v++) s.Insertar(v);
        Assert.Equal(1000, s.Recorrer("postorder")!.Count);
        Assert.Equal(1000, s.Niveles().Niveles);
        Assert.Equal(1000, s.ObtenerNodos().Count);
        Assert.False(s.Balance().Balanceado);
    }
}

public class ConsultaTests
{
    [Fact]
    public void Niveles_y_nodos_por_nivel()
    {
        var r = Fabrica.Sembrado().Niveles();
        Assert.Equal(4, r.Niveles);
        Assert.Equal(new[] { 1, 2, 4, 3 }, r.PorNivel);
    }

    [Fact]
    public void Niveles_de_arbol_vacio_es_cero()
    {
        var r = Fabrica.Servicio().Niveles();
        Assert.Equal(0, r.Niveles);
        Assert.Empty(r.PorNivel);
    }

    [Fact]
    public void Nivel_de_un_nodo()
    {
        var s = Fabrica.Sembrado();
        Assert.Equal(1, s.Nivel(50));
        Assert.Equal(4, s.Nivel(35));
        Assert.Null(s.Nivel(999));
    }

    [Fact]
    public void Extremos_con_sus_rutas()
    {
        var r = Fabrica.Sembrado().Extremos()!;
        Assert.Equal(20, r.Min);
        Assert.Equal(new[] { 50, 30, 20 }, r.RutaMin);
        Assert.Equal(80, r.Max);
        Assert.Equal(new[] { 50, 70, 80 }, r.RutaMax);
    }

    [Fact]
    public void Extremos_de_arbol_vacio_es_nulo() => Assert.Null(Fabrica.Servicio().Extremos());

    [Fact]
    public void Estadisticas_de_nodos_hojas_e_internos()
    {
        var r = Fabrica.Sembrado().Estadisticas();
        Assert.Equal(10, r.Nodos);
        Assert.Equal(5, r.Hojas);
        Assert.Equal(5, r.Internos);
    }

    [Fact]
    public void Estadisticas_de_arbol_vacio_son_cero()
    {
        var r = Fabrica.Servicio().Estadisticas();
        Assert.Equal(new EstadisticasResultado(0, 0, 0), r);
    }

    [Fact]
    public void Ancestros_de_un_nodo_van_de_la_raiz_al_padre() =>
        Assert.Equal(new[] { 50, 30, 40 }, Fabrica.Sembrado().Ancestros(35));

    [Fact]
    public void Ancestros_de_la_raiz_es_lista_vacia_y_no_nulo()
    {
        var r = Fabrica.Sembrado().Ancestros(50);
        Assert.NotNull(r);
        Assert.Empty(r!);
    }

    [Fact]
    public void Ancestros_de_nodo_inexistente_es_nulo() => Assert.Null(Fabrica.Sembrado().Ancestros(999));

    [Fact]
    public void Balance_del_arbol_de_ejemplo_es_verdadero()
    {
        var r = Fabrica.Sembrado().Balance();
        Assert.True(r.Balanceado);
        Assert.Equal(10, r.Factores.Count);
        Assert.Equal(0, r.Factores.Single(f => f.Valor == 50).Factor);
        Assert.Equal(-1, r.Factores.Single(f => f.Valor == 30).Factor);
    }

    [Fact]
    public void Balance_de_una_cadena_es_falso() =>
        Assert.False(Fabrica.Con(1, 2, 3).Balance().Balanceado);

    [Fact]
    public void Balance_de_arbol_vacio_es_verdadero_sin_factores()
    {
        var r = Fabrica.Servicio().Balance();
        Assert.True(r.Balanceado);
        Assert.Empty(r.Factores);
    }

    [Theory]
    [InlineData(35, 45, 40)]
    [InlineData(20, 45, 30)]
    [InlineData(35, 65, 50)]
    [InlineData(30, 35, 30)]   // uno es ancestro del otro
    [InlineData(50, 50, 50)]   // a == b
    public void Ancestro_comun_mas_bajo(int a, int b, int esperado)
    {
        var r = Fabrica.Sembrado().AncestroComun(a, b);
        Assert.Equal(esperado, r.Valor);
        Assert.Empty(r.Faltantes);
    }

    [Fact]
    public void Ancestro_comun_con_un_nodo_ausente_indica_cual_falta()
    {
        var r = Fabrica.Sembrado().AncestroComun(35, 999);
        Assert.Null(r.Valor);
        Assert.Equal(new[] { 999 }, r.Faltantes);
    }

    [Fact]
    public void Ancestro_comun_en_arbol_vacio_marca_ambos_ausentes()
    {
        var r = Fabrica.Servicio().AncestroComun(1, 2);
        Assert.Null(r.Valor);
        Assert.Equal(new[] { 1, 2 }, r.Faltantes);
    }

    [Fact]
    public void Aplanado_trae_padre_lado_nivel_e_indice()
    {
        var nodos = Fabrica.Sembrado().ObtenerNodos();
        Assert.Equal(10, nodos.Count);
        Assert.Equal(new NodoPlano(20, 30, "left", 3, 0), nodos[0]);
        Assert.Equal(new NodoPlano(50, null, null, 1, 5), nodos.Single(n => n.Valor == 50));
        Assert.Equal(new NodoPlano(65, 60, "right", 4, 7), nodos.Single(n => n.Valor == 65));
    }

    [Fact]
    public void Impresion_gira_el_arbol_con_sangria_por_nivel()
    {
        var lineas = Fabrica.Sembrado().Imprimir().Split(Environment.NewLine);
        Assert.Equal(10, lineas.Length);
        Assert.Equal("80", lineas[0].Trim());
        Assert.Equal(8, lineas[0].Length - lineas[0].TrimStart().Length);
        Assert.Equal("50", lineas[4]);
    }

    [Fact]
    public void Impresion_de_arbol_vacio() =>
        Assert.Equal("(árbol vacío)", Fabrica.Servicio().Imprimir());
}

public class ActualizarTests
{
    [Fact]
    public void Actualizar_cambia_el_valor_y_conserva_el_orden()
    {
        var s = Fabrica.Sembrado();
        var r = s.Actualizar(30, 33);
        Assert.Equal(EstadoActualizacion.Actualizado, r.Estado);
        Assert.Equal(new[] { 20, 33, 35, 40, 45, 50, 60, 65, 70, 80 }, s.Recorrer("inorder"));
    }

    [Fact]
    public void Actualizar_a_un_valor_existente_es_duplicado_y_no_cambia_nada()
    {
        var s = Fabrica.Sembrado();
        Assert.Equal(EstadoActualizacion.Duplicado, s.Actualizar(30, 70).Estado);
        Assert.Equal(10, s.Estadisticas().Nodos);
    }

    [Fact]
    public void Actualizar_inexistente() =>
        Assert.Equal(EstadoActualizacion.NoExiste, Fabrica.Sembrado().Actualizar(999, 1).Estado);

    [Fact]
    public void Vaciar_deja_el_arbol_sin_nodos()
    {
        var s = Fabrica.Sembrado();
        s.Vaciar();
        Assert.Equal(0, s.Estadisticas().Nodos);
    }
}

/// <summary>Pruebas de los principios O (abierto/cerrado) y L (sustitución).</summary>
public class SolidTests
{
    private sealed class Reverso : IRecorrido
    {
        public string Nombre => "reverse";

        public IReadOnlyList<int> Recorrer(Nodo? raiz) => new Inorden().Recorrer(raiz).Reverse().ToList();
    }

    [Fact]
    public void Un_recorrido_nuevo_se_agrega_sin_tocar_el_codigo_existente()
    {
        var recorridos = Fabrica.Recorridos().Append(new Reverso());
        var s = new ArbolService(new ArbolBinarioBusqueda(), recorridos);
        s.Sembrar();
        Assert.Equal(new[] { 80, 70, 65, 60, 50, 45, 40, 35, 30, 20 }, s.Recorrer("reverse"));
        Assert.Contains("reverse", s.NombresRecorrido);
    }

    [Fact]
    public void Cualquier_IRecorrido_es_sustituible_y_responde_a_la_misma_interfaz()
    {
        var s = Fabrica.Sembrado();
        foreach (var nombre in s.NombresRecorrido)
        {
            var lista = s.Recorrer(nombre);
            Assert.NotNull(lista);
            Assert.Equal(10, lista!.Count);
            Assert.Equal(lista.Order().ToList(), lista.Order().ToList());
        }
    }

    [Fact]
    public void El_servicio_se_usa_por_las_dos_interfaces_pequenas()
    {
        var s = Fabrica.Servicio();
        IArbolComandos comandos = s;
        IArbolConsultas consultas = s;
        comandos.Insertar(10);
        Assert.True(consultas.Buscar(10).Encontrado);
    }
}

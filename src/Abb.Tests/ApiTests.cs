using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Abb.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;

    public ApiTests(WebApplicationFactory<Program> fabrica) => _http = fabrica.CreateClient();

    private async Task Sembrar() => Assert.Equal(HttpStatusCode.OK, (await _http.PostAsync("/api/tree/seed", null)).StatusCode);

    private async Task Vaciar() => Assert.Equal(HttpStatusCode.OK, (await _http.DeleteAsync("/api/tree")).StatusCode);

    private static async Task<JsonElement> Json(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        return JsonDocument.Parse(texto).RootElement.Clone();
    }

    private static int[] Lista(JsonElement e) => e.EnumerateArray().Select(x => x.GetInt32()).ToArray();

    private static StringContent Cuerpo(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Operacion_1_obtener_el_arbol_en_forma_plana()
    {
        await Sembrar();
        var json = await Json(await _http.GetAsync("/api/tree"));
        Assert.Equal(10, json.GetProperty("count").GetInt32());
        var primero = json.GetProperty("nodes")[0];
        Assert.Equal(20, primero.GetProperty("value").GetInt32());
        Assert.Equal(30, primero.GetProperty("parent").GetInt32());
        Assert.Equal("left", primero.GetProperty("side").GetString());
        Assert.Equal(3, primero.GetProperty("level").GetInt32());
        Assert.Equal(0, primero.GetProperty("inorderIndex").GetInt32());
    }

    [Fact]
    public async Task Operacion_1_impresion_en_texto()
    {
        await Sembrar();
        var json = await Json(await _http.GetAsync("/api/tree/print"));
        Assert.Contains("50", json.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Operacion_2_buscar_encontrado_y_no_encontrado()
    {
        await Sembrar();
        var si = await Json(await _http.GetAsync("/api/tree/nodes/45"));
        Assert.True(si.GetProperty("found").GetBoolean());
        Assert.Equal(new[] { 50, 30, 40, 45 }, Lista(si.GetProperty("path")));
        Assert.Equal(4, si.GetProperty("level").GetInt32());

        var no = await _http.GetAsync("/api/tree/nodes/99");
        Assert.Equal(HttpStatusCode.OK, no.StatusCode);
        Assert.False((await Json(no)).GetProperty("found").GetBoolean());
    }

    [Fact]
    public async Task Operacion_3_insertar_responde_201_con_la_ruta()
    {
        await Sembrar();
        var r = await _http.PostAsJsonAsync("/api/tree/nodes", new { value = 55 });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var json = await Json(r);
        Assert.Equal(new[] { 50, 70, 60, 55 }, Lista(json.GetProperty("path")));
        Assert.Equal(11, json.GetProperty("tree").GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Insertar_duplicado_responde_409_con_problem_details()
    {
        await Sembrar();
        var r = await _http.PostAsJsonAsync("/api/tree/nodes", new { value = 40 });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("{}")]                       // falta el valor
    [InlineData("{\"value\":1.5}")]          // decimal
    [InlineData("{\"value\":99999999999}")]  // desbordado
    [InlineData("{\"value\":1000}")]         // fuera de rango
    [InlineData("{\"value\":-1000}")]        // fuera de rango
    [InlineData("{\"value\":\"abc\"}")]      // texto
    public async Task Insertar_con_valor_no_valido_responde_400(string cuerpo)
    {
        await Sembrar();
        var r = await _http.PostAsync("/api/tree/nodes", Cuerpo(cuerpo));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("99999999999")]
    [InlineData("5000")]
    public async Task Ruta_con_valor_no_entero_o_fuera_de_rango_responde_400(string valor)
    {
        await Sembrar();
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.GetAsync($"/api/tree/nodes/{valor}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.DeleteAsync($"/api/tree/nodes/{valor}")).StatusCode);
    }

    [Fact]
    public async Task Operacion_4_borrar_con_dos_hijos_informa_sucesor_y_ruta()
    {
        await Sembrar();
        var r = await _http.DeleteAsync("/api/tree/nodes/70");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var json = await Json(r);
        Assert.Equal("dosHijos", json.GetProperty("case").GetString());
        Assert.Equal(80, json.GetProperty("successor").GetInt32());
        Assert.Equal(new[] { 70, 80 }, Lista(json.GetProperty("successorPath")));
        Assert.Equal(9, json.GetProperty("tree").GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Borrar_inexistente_responde_404()
    {
        await Sembrar();
        Assert.Equal(HttpStatusCode.NotFound, (await _http.DeleteAsync("/api/tree/nodes/999")).StatusCode);
    }

    [Theory]
    [InlineData("preorder", new[] { 50, 30, 20, 40, 35, 45, 70, 60, 65, 80 })]
    [InlineData("inorder", new[] { 20, 30, 35, 40, 45, 50, 60, 65, 70, 80 })]
    [InlineData("postorder", new[] { 20, 35, 45, 40, 30, 65, 60, 80, 70, 50 })]
    [InlineData("levelorder", new[] { 50, 30, 70, 20, 40, 60, 80, 35, 45, 65 })]
    public async Task Operaciones_5_a_8_recorridos(string nombre, int[] esperado)
    {
        await Sembrar();
        var json = await Json(await _http.GetAsync($"/api/tree/traversals/{nombre}"));
        Assert.Equal(esperado, Lista(json.GetProperty("values")));
    }

    [Fact]
    public async Task Recorrido_desconocido_responde_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _http.GetAsync("/api/tree/traversals/zigzag")).StatusCode);
    }

    [Fact]
    public async Task Operacion_9_niveles_y_nodos_por_nivel()
    {
        await Sembrar();
        var json = await Json(await _http.GetAsync("/api/tree/levels"));
        Assert.Equal(4, json.GetProperty("levels").GetInt32());
        Assert.Equal(new[] { 1, 2, 4, 3 }, Lista(json.GetProperty("perLevel")));
    }

    [Fact]
    public async Task Operacion_10_nivel_de_un_nodo()
    {
        await Sembrar();
        Assert.Equal(4, (await Json(await _http.GetAsync("/api/tree/nodes/35/level"))).GetProperty("level").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await _http.GetAsync("/api/tree/nodes/999/level")).StatusCode);
    }

    [Fact]
    public async Task Operacion_11_extremos_y_arbol_vacio()
    {
        await Sembrar();
        var json = await Json(await _http.GetAsync("/api/tree/extremes"));
        Assert.Equal(20, json.GetProperty("min").GetInt32());
        Assert.Equal(80, json.GetProperty("max").GetInt32());
        await Vaciar();
        Assert.Equal(HttpStatusCode.NotFound, (await _http.GetAsync("/api/tree/extremes")).StatusCode);
    }

    [Fact]
    public async Task Operacion_12_estadisticas()
    {
        await Sembrar();
        var json = await Json(await _http.GetAsync("/api/tree/stats"));
        Assert.Equal(10, json.GetProperty("nodes").GetInt32());
        Assert.Equal(5, json.GetProperty("leaves").GetInt32());
        Assert.Equal(5, json.GetProperty("internalNodes").GetInt32());
    }

    [Fact]
    public async Task Operacion_13_ancestros_de_la_raiz_es_lista_vacia_y_de_inexistente_404()
    {
        await Sembrar();
        var raiz = await _http.GetAsync("/api/tree/nodes/50/ancestors");
        Assert.Equal(HttpStatusCode.OK, raiz.StatusCode);
        Assert.Empty(Lista((await Json(raiz)).GetProperty("ancestors")));
        Assert.Equal(new[] { 50, 30, 40 },
            Lista((await Json(await _http.GetAsync("/api/tree/nodes/35/ancestors"))).GetProperty("ancestors")));
        Assert.Equal(HttpStatusCode.NotFound, (await _http.GetAsync("/api/tree/nodes/999/ancestors")).StatusCode);
    }

    [Fact]
    public async Task Operacion_14_balance()
    {
        await Sembrar();
        Assert.True((await Json(await _http.GetAsync("/api/tree/balance"))).GetProperty("balanced").GetBoolean());
    }

    [Fact]
    public async Task Operacion_15_ancestro_comun_mas_bajo()
    {
        await Sembrar();
        var json = await Json(await _http.GetAsync("/api/tree/lca?a=35&b=45"));
        Assert.Equal(40, json.GetProperty("value").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await _http.GetAsync("/api/tree/lca?a=35&b=999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.GetAsync("/api/tree/lca?a=35")).StatusCode);
        await Vaciar();
        Assert.Equal(HttpStatusCode.NotFound, (await _http.GetAsync("/api/tree/lca?a=1&b=2")).StatusCode);
    }

    [Fact]
    public async Task Actualizar_cambia_el_valor_y_responde_los_errores_esperados()
    {
        await Sembrar();
        var ok = await _http.PutAsJsonAsync("/api/tree/nodes/30", new { newValue = 33 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _http.PutAsJsonAsync("/api/tree/nodes/33", new { newValue = 70 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _http.PutAsJsonAsync("/api/tree/nodes/999", new { newValue = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.PutAsync("/api/tree/nodes/33", Cuerpo("{}"))).StatusCode);
    }

    [Fact]
    public async Task Cadena_degenerada_de_mas_de_64_niveles_se_serializa()
    {
        await Vaciar();
        for (var v = -100; v < 100; v++)
            Assert.Equal(HttpStatusCode.Created, (await _http.PostAsJsonAsync("/api/tree/nodes", new { value = v })).StatusCode);
        var json = await Json(await _http.GetAsync("/api/tree"));
        Assert.Equal(200, json.GetProperty("count").GetInt32());
        Assert.Equal(200, (await Json(await _http.GetAsync("/api/tree/levels"))).GetProperty("levels").GetInt32());
    }

    [Fact]
    public async Task Vaciar_y_sembrar_devuelven_el_arbol()
    {
        await Vaciar();
        Assert.Equal(0, (await Json(await _http.GetAsync("/api/tree"))).GetProperty("count").GetInt32());
        var sembrado = await Json(await _http.PostAsync("/api/tree/seed", null));
        Assert.Equal(10, sembrado.GetProperty("count").GetInt32());
    }
}

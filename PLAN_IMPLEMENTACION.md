# Plan de implementación: Árbol Binario de Búsqueda (ABB) como aplicación web

Aplicación web dinámica con API REST en C# (ASP.NET Core) y frontend en JavaScript con componentes de Asciify.

---

## 1. Alcance y decisiones de partida

**Objetivo.** Implementar un ABB con representación gráfica y las 15 operaciones de la instrucción, más un documento final del proyecto con portada y desarrollo.

**Decisiones tomadas (revisables antes de empezar la Fase 1):**

| Tema | Decisión | Motivo |
|---|---|---|
| Backend | ASP.NET Core con controladores (`[ApiController]`) sobre .NET 10 (LTS vigente), proyecto `Abb.Api` y biblioteca `Abb.Core`. Organización MVC, con POO, SOLID y CRUD (ver sección 2) | La estructura de datos queda separada del transporte HTTP y se prueba sin levantar el servidor. El soporte de .NET 8 terminó el 10 de noviembre de 2026. Si el laboratorio solo tiene .NET 8, se usa ese y se anota el motivo |
| Frontend | JavaScript con módulos ES, empaquetado con Vite | Asciify se distribuye como paquete npm (`asciify-engine`), así que conviene un empaquetador |
| Entrega | El build de Vite se copia a `Abb.Api/wwwroot` y la API lo sirve con `UseStaticFiles`. En desarrollo, Vite usa `proxy` hacia la API | Con `dotnet run` basta para levantar todo. Quien califica no necesita Node. Se entrega un README con los pasos, un lockfile de npm y sin `node_modules` |
| Estado del árbol | Un solo árbol en memoria, registrado como singleton, protegido con `ReaderWriterLockSlim` (lecturas y recorridos con lock de lectura, inserción y borrado con lock de escritura) | La tarea no pide persistencia. Una base de datos añadiría trabajo sin aportar a lo evaluado. Un recorrido no debe correr mientras otra petición cambia el árbol |
| Tipo de dato | Enteros entre -999 y 999, sin duplicados (el duplicado responde 409) | Simplifica comparaciones y mantiene legible el ancho de cada nodo |
| Convención de niveles | La raíz está en el nivel 1. El número de niveles del árbol es igual a su altura en nodos | Hay que dejarlo escrito en el documento, porque otros textos usan nivel 0 |
| Dibujo del árbol | Nodos HTML posicionados sobre un SVG de líneas, con un cálculo de posiciones propio en píxeles | En la lista de 39 componentes de Asciify no aparece uno de árbol (a confirmar en la Fase 0). Los nodos HTML permiten animarlos con sus componentes |
| Repositorio | GitHub: https://github.com/Juulren/ABB.git, rama `master`. Un commit por fase terminada, con mensaje que nombra la fase. `node_modules/` y `dist/` quedan fuera con `.gitignore` | El historial de commits sirve de evidencia del avance por fases, y el documento final enlaza el repositorio |
| Formato final | Se redacta en Markdown y se exporta a .docx con pandoc y un `reference.docx` | La instrucción pide un documento con portada. Se confirma con el docente si espera .docx o PDF antes de la Fase 6 |

**Lo que no se sabe todavía.** La página de componentes de Asciify lista 39 elementos, pero no marca cuáles son gratuitos ni da descripciones. Tampoco documenta instalación con CDN. Las firmas de función de la sección 4 se leyeron de los ejemplos de la referencia de componentes y están marcadas como inferidas. La Fase 0 las comprueba contra el paquete real antes de escribir pantallas.

---

## 2. Arquitectura

```
ABB/
├─ src/
│  ├─ Abb.Core/            MODELO
│  │  ├─ Modelos/          Nodo y resultados (record)
│  │  ├─ Estructura/       ArbolBinarioBusqueda (insertar, borrar, buscar)
│  │  ├─ Recorridos/       IRecorrido, Preorden, Inorden, Postorden, PorNiveles
│  │  ├─ Consultas/        niveles, extremos, estadísticas, balance, ancestros, LCA
│  │  └─ Servicios/        IArbolComandos, IArbolConsultas, ArbolService
│  ├─ Abb.Api/             CONTROLADORES Y VISTA JSON
│  │  ├─ Controllers/      NodosController, ArbolController, RecorridosController
│  │  ├─ Dtos/             peticiones y respuestas
│  │  └─ Program.cs        inyección de dependencias, ProblemDetails, archivos estáticos, Swagger
│  └─ Abb.Tests/           xUnit: modelo, servicios y controladores con dobles
├─ web/                    Vite + JavaScript
│  ├─ index.html
│  └─ src/
│     ├─ model/            TreeModel.js (estado y suscriptores), ApiClient.js
│     ├─ view/             TreeView.js (posiciones y render), animations.js, paneles y componentes Asciify
│     ├─ controller/       TreeController.js (eventos, llamadas a la API, actualización del modelo)
│     └─ styles.css
├─ docs/
│  ├─ capturas/            imágenes del documento final
│  └─ Proyecto_ABB.md      documento del proyecto
└─ PLAN_IMPLEMENTACION.md
```

Flujo: la interfaz llama a la API por `fetch`, la API opera sobre el árbol en memoria y responde JSON. El frontend nunca calcula una operación del árbol. Solo calcula posiciones de dibujo, y eso evita que la vista y los datos se contradigan. Las respuestas de escritura (insertar, borrar, vaciar, sembrar) ya traen el árbol actualizado, así que no hay una segunda petición `GET /api/tree` después de cada una.

### Arquitectura MVC, POO, SOLID y CRUD

El código se organiza con el patrón Modelo-Vista-Controlador en las dos capas. El servidor entrega JSON y no páginas Razor, así que su "vista" es la representación JSON de los resultados, y la vista visible está en el cliente.

| Capa MVC | Servidor (C#) | Cliente (JavaScript) |
|---|---|---|
| Modelo | `Abb.Core`: `Nodo`, `ArbolBinarioBusqueda`, recorridos, consultas y `ArbolService` | `model/TreeModel.js`: árbol actual, operación en curso y lista de suscriptores |
| Vista | DTO de `Abb.Api/Dtos`, que definen qué datos salen al cliente | `view/`: `TreeView`, paneles, pestañas, avisos y contadores. Solo pinta y no decide nada |
| Controlador | `Abb.Api/Controllers`: reciben la petición, validan el valor, llaman al servicio y devuelven el DTO | `controller/TreeController.js`: escucha eventos de la interfaz, llama a `ApiClient` y actualiza el modelo |

En el cliente, la vista se redibuja cuando el modelo avisa de un cambio (patrón observador). Ninguna vista llama a otra y ninguna llama directamente a la API.

**Programación orientada a objetos.**

- Encapsulamiento: `Nodo` y la raíz del árbol no salen de `Abb.Core`. Los controladores solo reciben DTO inmutables (`record`).
- Abstracción: los controladores dependen de interfaces, nunca de clases concretas.
- Herencia y polimorfismo: los cuatro recorridos implementan `IRecorrido`, y `RecorridosController` elige uno por nombre sin una cadena de `if`.
- Composición: `ArbolService` contiene un `ArbolBinarioBusqueda` y un candado de lectura y escritura, en lugar de heredar de él.

**Principios SOLID.**

| Principio | Cómo se aplica | Cómo se comprueba |
|---|---|---|
| S, responsabilidad única | `ArbolBinarioBusqueda` solo mantiene la estructura (insertar, borrar, buscar, actualizar). Recorridos, estadísticas y balance viven en clases aparte. El candado de concurrencia está en `ArbolService`. Cada controlador atiende un grupo de rutas | Ninguna clase de `Abb.Core` mezcla estructura, cálculo de consultas y concurrencia |
| O, abierto y cerrado | Un recorrido nuevo se añade con otra clase `IRecorrido` registrada en `Program.cs`, sin tocar las clases existentes ni el controlador | Prueba que registra un recorrido de ejemplo y lo invoca por nombre |
| L, sustitución de Liskov | Cualquier `IRecorrido` y cualquier implementación de `IArbolComandos` o `IArbolConsultas` se puede sustituir sin cambiar el comportamiento esperado | La misma batería de pruebas corre contra `ArbolService` y contra un doble de prueba |
| I, segregación de interfaces | Interfaces pequeñas: `IArbolComandos` (insertar, actualizar, borrar, vaciar), `IArbolConsultas` (buscar, niveles, extremos, estadísticas, ancestros, balance, LCA) e `IRecorrido` | Un controlador de solo lectura recibe `IArbolConsultas` y no conoce los comandos |
| D, inversión de dependencias | Los controladores reciben las interfaces por constructor y `Program.cs` registra las implementaciones con inyección de dependencias | Las pruebas de controlador usan un doble en lugar del servicio real |

**CRUD.**

| Acción CRUD | Ruta | Operación de la instrucción |
|---|---|---|
| Create | `POST /api/tree/nodes` | 3, Insertar elemento |
| Read | Todas las rutas `GET` de la sección 3 | 1, 2 y 5 a 15 |
| Update | `PUT /api/tree/nodes/{valor}` con `{ "newValue": n }` | Ninguna. Se añade para completar el CRUD |
| Delete | `DELETE /api/tree/nodes/{valor}` | 4, Borrar elemento |

La instrucción no incluye una operación de actualización, así que `PUT` es un añadido de este plan y no cuenta entre las 15. En un ABB, cambiar un valor significa borrar el anterior e insertar el nuevo, porque la posición depende del valor. `ArbolService` lo hace dentro de un mismo candado de escritura. Si el valor nuevo ya existe responde 409 y el árbol no cambia. Si el valor actual no existe responde 404.

---

## 3. Operaciones y API REST

Las operaciones 1 a 10 vienen dadas. Las 11 a 15 son las libres. Cada una necesita un título propio en el menú, como pide la instrucción.

| # | Operación (título en el menú) | Método y ruta | Respuesta principal |
|---|---|---|---|
| 1 | Imprimir elementos | `GET /api/tree` y `GET /api/tree/print` | Árbol anidado y su impresión en texto con sangría |
| 2 | Buscar elemento | `GET /api/tree/nodes/{valor}` | `found`, `path` (valores visitados), `level` |
| 3 | Insertar elemento | `POST /api/tree/nodes` con `{ "value": n }` | Árbol actualizado y ruta de inserción. 409 si ya existe |
| 4 | Borrar elemento | `DELETE /api/tree/nodes/{valor}` | Árbol actualizado, `case` (hoja, un hijo, dos hijos), y con dos hijos también `successor` y `successorPath`. 404 si no existe |
| 5 | Recorrido por niveles | `GET /api/tree/traversals/levelorder` | Lista de valores en orden |
| 6 | Recorrido preorden | `GET /api/tree/traversals/preorder` | Ídem |
| 7 | Recorrido postorden | `GET /api/tree/traversals/postorder` | Ídem |
| 8 | Recorrido inorden | `GET /api/tree/traversals/inorder` | Ídem |
| 9 | Número de niveles | `GET /api/tree/levels` | `levels` (entero, 0 si está vacío) y `perLevel` (nodos por nivel, para la gráfica) |
| 10 | Nivel de un nodo | `GET /api/tree/nodes/{valor}/level` | Entero, o 404 |
| 11 | Mínimo y máximo | `GET /api/tree/extremes` | `min`, `max` y la ruta hacia cada uno |
| 12 | Contar nodos y hojas | `GET /api/tree/stats` | `nodes`, `leaves`, `internalNodes` (nodos menos hojas) |
| 13 | Ancestros de un nodo | `GET /api/tree/nodes/{valor}/ancestors` | Camino de la raíz al padre. Lista vacía si el nodo es la raíz |
| 14 | Verificar balance | `GET /api/tree/balance` | `balanced` y factor de equilibrio por nodo |
| 15 | Ancestro común más bajo | `GET /api/tree/lca?a={x}&b={y}` | Valor del ancestro y los dos caminos |

Rutas auxiliares que no cuentan como operaciones: `PUT /api/tree/nodes/{valor}` (actualizar, para completar el CRUD), `DELETE /api/tree` (vaciar) y `POST /api/tree/seed` (cargar un conjunto de ejemplo para las capturas).

**Formato del árbol en las respuestas.** Una lista plana de nodos `{ value, parent, side, level, inorderIndex }`, no un objeto anidado. El serializador de System.Text.Json limita la profundidad a 64 por defecto, y un árbol degenerado de 60 o más nodos rompería `GET /api/tree`. La lista plana evita el problema y además trae `level` e `inorderIndex`, que son los datos que necesita el dibujo.

**Semántica de casos límite (se escribe antes de la Fase 1 y cada punto lleva prueba):**

| Caso | Respuesta |
|---|---|
| Árbol vacío: niveles | `levels = 0`, `perLevel = []` |
| Árbol vacío: extremos, balance, LCA, recorridos | Extremos y LCA responden 404 con mensaje "árbol vacío". Balance responde `balanced = true` con lista vacía. Recorridos responden lista vacía |
| Ancestros de la raíz | 200 con lista vacía (distinto de nodo inexistente, que es 404) |
| LCA con `a == b` | El mismo valor, si existe |
| LCA con un nodo ausente | 404 que indica cuál de los dos falta |
| Balance | Definición AVL: en cada nodo, `abs(altura(izq) - altura(der)) <= 1`. Se calcula en un solo recorrido postorden, no recalculando alturas por nodo (sería O(n²)) |

**Contrato de errores.** Respuestas con `ProblemDetails` (`application/problem+json`) mediante `AddProblemDetails()` y un manejador de excepciones y de códigos de estado: 400 para valor ausente, decimal, fuera de rango o desbordado, 404 para nodo inexistente, 409 para duplicado. Las rutas toman `{valor}` como cadena y la validan a mano. Con la restricción `{valor:int}` un texto no numérico devolvería 404 en lugar de 400. El frontend muestra los errores como avisos y nunca deja la pantalla en blanco.

**Boceto del modelo (Abb.Core).**

```csharp
public sealed class Nodo                                 // Modelos/
{
    public int Valor { get; internal set; }
    public Nodo? Izquierdo { get; internal set; }
    public Nodo? Derecho { get; internal set; }
}

public sealed class ArbolBinarioBusqueda                 // Estructura/: solo la estructura
{
    internal Nodo? Raiz { get; private set; }

    public InsercionResultado Insertar(int valor);      // iterativo, devuelve ruta
    public BorradoResultado Borrar(int valor);          // 3 casos, sucesor inorden
    public BusquedaResultado Buscar(int valor);         // ruta y nivel
    public void Vaciar();
}

public interface IRecorrido                              // Recorridos/: estrategia
{
    string Nombre { get; }                               // "preorder", "inorder", ...
    IReadOnlyList<int> Recorrer(Nodo? raiz);
}

public interface IArbolComandos                          // Servicios/: escritura
{
    InsercionResultado Insertar(int valor);
    BorradoResultado Borrar(int valor);
    ActualizacionResultado Actualizar(int actual, int nuevo);
    void Vaciar();
}

public interface IArbolConsultas                         // Servicios/: lectura
{
    BusquedaResultado Buscar(int valor);
    IReadOnlyList<int> Recorrer(string nombre);          // delega en IRecorrido
    NivelesResultado Niveles();                          // total y nodos por nivel
    int? Nivel(int valor);
    ExtremosResultado? Extremos();                       // min, max y sus rutas
    EstadisticasResultado Estadisticas();                // nodos, hojas, internos
    IReadOnlyList<int>? Ancestros(int valor);            // null = no existe, vacía = es la raíz
    BalanceResultado Balance();                          // un solo postorden
    LcaResultado? AncestroComun(int a, int b);           // valor y ambos caminos
}

// ArbolService implementa IArbolComandos e IArbolConsultas, contiene
// un ArbolBinarioBusqueda y el ReaderWriterLockSlim.
```

Puntos que cuidar:

- El borrado con dos hijos copia el valor del sucesor inorden (mínimo del subárbol derecho) en el nodo objetivo y elimina el nodo del sucesor. Ese sucesor puede tener un hijo derecho, que hay que reconectar a su padre. `BorradoResultado` devuelve `Caso`, `Sucesor` y `RutaSucesor` para que el frontend anime la secuencia correcta.
- La profundidad real de recursión no es problema: 1000 llamadas caben sobre la pila de .NET. El límite que sí importa es la profundidad del JSON, resuelta con la lista plana. Se mantiene un tope de 1000 nodos solo por legibilidad del dibujo, y se alinea con el umbral de desplazamiento del frontend.
- Los resultados son tipos propios (`record`) y los endpoints no exponen la clase `Nodo`, que tiene referencias entre nodos.

---

## 4. Frontend y uso de componentes de Asciify

**Paquete (inferido del sitio, sin verificar).** `npm i asciify-engine`. Los componentes se importan desde `asciify-engine/kit`. Licencia MIT según el sitio. Algunas funciones requieren la versión 4.12 o superior y la 4.33.0 es la que muestra el sitio. Si el paquete no existe en npm con ese nombre o no coincide con el sitio, la Fase 0 lo detecta y se activa el plan de reserva (ver Fase 0).

**Asignación de componentes.** Los nombres de función salen de los ejemplos de la referencia y no de una firma completa. La columna "Verificar" se resuelve en la Fase 0.

| Componente Asciify | Función inferida | Dónde se usa | Verificar |
|---|---|---|---|
| Command palette | `commandPalette(panel, { trigger, onRun })` | Menú de las 15 operaciones, con atajo Ctrl+K. El valor numérico se captura en un campo aparte del panel lateral, porque la paleta probablemente no recibe texto libre | Formato de las filas y si admite un argumento |
| Button loading | `buttonLoading(button, { announce })` con `.start/.succeed/.fail` | Botones Insertar, Buscar, Borrar | Ancho fijo del botón |
| Toast stack | `toastStack(container, { max, duration })` y `.push(...)` | Resultados y errores de la API | Estilos por defecto |
| Decode headline | `decode(element, { duration, settle })` | Título de la aplicación y de cada sección | Accesibilidad del texto real |
| Glyph odometer | `odometer(element, { value, format })` y `.set(n)` | Contadores de nodos, hojas y niveles | Cambio de dígitos al insertar |
| Block chart | `blockChart(plot, { values, labels, look, onHover })` y `.set(...)` | Nodos por nivel (operación 9 y 12) | Etiquetas por nivel |
| Character tabs | `tabs(tablist, { panels, onChange })` y `.select(i)` | Selector de recorrido (pre, in, post, niveles) | Posición de la pestaña activa |
| Badges and pills | `pill(elements)` y `.count(n)` | Chips con el resultado de cada recorrido | Atributos `data-*` |
| Status orb | `statusOrb(slot, { ring, colourByState })` | Estado de conexión con la API | Cuatro estados |
| Skeleton loader | `skeleton(card, { shimmer, stagger })` y `.resolve()` | Carga inicial del árbol | Marcado `data-block` |
| Delete into characters | `disintegrate(element, { duration })` (promesa) | Animación al borrar un nodo, antes de quitarlo del DOM | Si funciona sobre SVG o solo sobre HTML. Se supone que solo HTML, por eso los nodos son `div` |
| Page transition | `pageDissolve(view).swap(cb, origen)` | Cambio entre vista del árbol y vista de recorridos | Origen de la onda |
| Scrambling links | `decode(el, { scramble: true, trigger: 'manual' })` | Enlaces del encabezado | Solo al pasar el puntero |
| Background e Interactive hero background | `background(el, { preset })` con `swell`, `drift`, `halo`, `caustics`, `estuary`, `capillary` o `shockwave` | Fondo de la aplicación | Costo de GPU en equipos modestos. Plan B: fondo CSS estático |

Si alguno de estos componentes resulta de pago, no se usa y se sustituye por CSS propio. El documento final debe decir cuáles se emplearon. "Scrambling links" parece ser el mismo `decode` con `scramble: true`, y también está por confirmar.

**Dibujo del árbol (clase TreeView, en view/TreeView.js).**

1. Recibir la lista plana de nodos (`value`, `parent`, `side`, `level`, `inorderIndex`).
2. Calcular `x` con `inorderIndex` y `y` con `level`, ambos multiplicados por una separación fija. No cruza líneas.
3. Trabajar con un solo sistema de coordenadas en píxeles. Las aristas son `<line>` dentro de un `<svg>` y los nodos son `<div class="nodo">` absolutos, y ambos usan los mismos números. Un `viewBox` escalaría solo el SVG y los `div` quedarían desalineados.
4. Para que un árbol ancho quepa en pantallas angostas, el SVG y los `div` viven en un único contenedor y se escalan juntos con un solo `transform: scale()`. Con más de 40 nodos se activa desplazamiento controlado, en línea con el tope de 1000 nodos de la API.
5. Los valores se escriben con `textContent`, nunca con `innerHTML`.

**Animaciones del árbol (view/animations.js).**

- Búsqueda e inserción: resaltar los nodos de `path` uno a uno cada 350 ms.
- Recorridos: marcar el orden de visita con un número sobre cada nodo.
- Borrado de hoja o de nodo con un hijo: resaltar el nodo, `disintegrate` y redibujar.
- Borrado con dos hijos, en este orden: resaltar el nodo objetivo, recorrer `successorPath` hasta el sucesor, mover su valor al nodo objetivo, aplicar `disintegrate` al `div` del sucesor (su posición anterior, no la del valor pedido) y redibujar con el árbol que trae la respuesta.
- Respetar `prefers-reduced-motion`: sin secuencias, solo el resultado final. Lo mismo para `decode` y el fondo, y el fondo lleva un interruptor visible para apagarlo.

**Diseño visual.** Paleta oscura con un color de acento para la ruta activa y otro para el nodo encontrado. Se define primero como variables CSS y los componentes de Asciify se ajustan a ellas. Tipografía monoespaciada para valores. Contraste suficiente entre nodo y fondo animado, porque un fondo con GPU compite con la lectura.

**Disposición.** Barra superior (título, estado de la API, paleta). Panel lateral con entrada de valor y botones frecuentes. Zona central con el árbol. Panel inferior con pestañas de recorrido y contadores. En teléfono, el panel lateral pasa debajo del árbol.

---

## 5. Fases de trabajo

Cada fase termina con algo comprobable. Según `CLAUDE.md`, las búsquedas en el repositorio y las pruebas se delegan a los subagentes `search`, `grep` y `audit`, y `advisor` revisa antes de cerrar el plan, ante un error repetido y antes de dar el trabajo por terminado.

### Fase 0. Verificación de Asciify (media jornada)

- Crear `web/` con Vite, instalar `asciify-engine`.
- Probar uno por uno los 14 componentes de la sección 4 en una página de prueba.
- Registrar las firmas reales, cuáles funcionan sin framework y cuáles fallan.
- **Regla de salida:** si menos de unos 5 componentes funcionan sin framework, el resto de los efectos se escribe en CSS y no se insiste con la biblioteca. Si el paquete npm no existe o no coincide con el sitio, el plan de reserva es copiar los ejemplos de código de la referencia o escribir los efectos con CSS y JavaScript propios, y se declara en el documento.
- **Salida:** tabla corregida en la sección 4 y lista de sustituciones por CSS.

### Fase 1. Modelo (MVC), POO y SOLID, con pruebas

- Crear la solución .NET, `Abb.Core` y `Abb.Tests`.
- Implementar `Nodo`, `ArbolBinarioBusqueda`, las cuatro clases `IRecorrido`, las clases de consulta y `ArbolService` con las interfaces de la sección 2.
- Implementar las 15 operaciones con los resultados descritos arriba, más `Actualizar` para el CRUD.
- Pruebas de SOLID: recorrido nuevo registrado sin tocar código existente (O), misma batería contra el servicio real y un doble (L).
- Escribir antes la tabla de casos límite de la sección 3 y probar cada fila.
- Pruebas xUnit: árbol vacío, un nodo, árbol degenerado a la derecha y a la izquierda, borrado de hoja, de nodo con un hijo, con dos hijos y de la raíz, borrado cuyo sucesor tiene hijo derecho, duplicados, recorridos contra un caso calculado a mano, LCA cuando uno de los nodos es ancestro del otro, LCA con un nodo ausente y con `a == b`, ancestros de la raíz, extremos y niveles con el árbol vacío, balance en árbol balanceado, desbalanceado y vacío.
- **Salida:** `dotnet test` en verde.

### Fase 2. Controladores y API REST

- `NodosController` (crear, actualizar, borrar, leer un nodo), `ArbolController` (árbol, impresión, estadísticas y demás consultas, vaciar, sembrar) y `RecorridosController`, todos con las interfaces por constructor y registrados con inyección de dependencias. Las rutas son las de la sección 3, con proxy de Vite en desarrollo (CORS solo si hace falta), Swagger en desarrollo, `UseStaticFiles` para servir `wwwroot`.
- `ProblemDetails` para los tres errores, con validación manual del valor (ausente, decimal, desbordado, fuera de rango).
- Pruebas de integración con `WebApplicationFactory` para al menos insertar, buscar, borrar y un recorrido, y una con una cadena degenerada de más de 64 nodos para comprobar que la serialización no falla.
- README con los pasos de ejecución.
- **Salida:** las 15 operaciones responden desde Swagger.

### Fase 3. Esqueleto del frontend

- Estructura MVC del cliente: `model/TreeModel.js` y `model/ApiClient.js`, `view/TreeView.js` y `controller/TreeController.js`, con el patrón observador entre modelo y vistas.
- Estructura de la página, variables CSS, fondo.
- Estado de conexión y carga inicial con esqueleto.
- Dibujo estático del árbol con `TreeView` (sin animación aún).
- **Salida:** insertar valores desde la interfaz y verlos dibujados.

### Fase 4. Operaciones, menú y animaciones

- Paleta de comandos con las 15 operaciones y entrada de valor.
- Botones con estado de carga, avisos, contadores, pestañas de recorrido, gráfica por nivel.
- Animaciones de ruta, recorrido y borrado.
- Vista de impresión de elementos (texto con sangría).
- Comparar los títulos del menú de las operaciones 1 a 10 con el texto original de la instrucción. Las operaciones 11 a 15 llevan títulos propios.
- **Salida:** las 15 operaciones usables desde la interfaz.

### Fase 5. Pulido y comprobación

- Revisión en 375, 768 y 1280 px de ancho, y con `prefers-reduced-motion`.
- Navegación solo con teclado en la paleta y en las pestañas.
- Casos límite: árbol vacío en cada operación, valor no numérico, 409, 404, árbol de 50 nodos.
- Revisión de seguridad básica: validación de entrada en la API y sin HTML inyectado desde valores.
- **Salida:** lista de pruebas manuales marcada, sin errores en consola.

### Fase 6. Documento del proyecto

Estructura pedida: **Portada con datos completos** y **Desarrollo**.

**Portada.** Campos a completar por el autor (no se inventan):

- Institución y plantel
- Carrera y semestre
- Materia y unidad
- Título del proyecto
- Nombre completo del alumno y matrícula
- Nombre del docente
- Lugar y fecha de entrega

**Desarrollo.**

1. Introducción y objetivo del proyecto.
2. Marco breve: definición de ABB, propiedades, complejidad de búsqueda, inserción y borrado en caso promedio y peor.
3. Tecnologías: .NET, ASP.NET Core, JavaScript, Vite, Asciify (con los componentes realmente usados).
4. Diseño: arquitectura, diagrama de carpetas, contrato de la API.
5. Arquitectura MVC y principios aplicados: el patrón en servidor y cliente, la POO usada, una tabla SOLID con la clase concreta que cumple cada principio, y el mapa CRUD contra las rutas. Incluye un diagrama de clases y un diagrama de capas (figuras 23 y 24).
6. Implementación del modelo: código de `ArbolBinarioBusqueda`, de los recorridos y de `ArbolService`, con explicación por operación.
7. Implementación de controladores y API: código de los controladores y de `Program.cs`, con ejemplos de petición y respuesta.
8. Implementación del frontend: `TreeModel`, `TreeView`, `TreeController`, `ApiClient` y uso de componentes.
9. Funcionalidad: una subsección por cada una de las 15 operaciones con captura y descripción.
10. Pruebas realizadas y resultados.
11. Limitaciones (memoria, sin persistencia, tope de nodos, dependencia de GPU).
12. Conclusiones.
13. Referencias (documentación de .NET, sitio de Asciify con fecha de consulta) y enlace al repositorio de GitHub (https://github.com/Juulren/ABB.git).

**Imágenes con descripción.** Cada captura lleva número, título y un párrafo que dice qué operación se ve y qué valores se usaron.

| Fig. | Contenido |
|---|---|
| 1 | Pantalla principal con árbol de ejemplo |
| 2 | Paleta de comandos con las 15 operaciones |
| 3 | Impresión de elementos |
| 4 | Búsqueda con ruta resaltada (encontrado) |
| 5 | Búsqueda sin éxito |
| 6 | Inserción con ruta |
| 7 | Borrado de una hoja |
| 8 | Borrado de un nodo con un hijo |
| 9 | Borrado de un nodo con dos hijos |
| 10 | Recorrido por niveles |
| 11 | Recorrido preorden |
| 12 | Recorrido inorden |
| 13 | Recorrido postorden |
| 14 | Número de niveles y gráfica de nodos por nivel |
| 15 | Nivel de un nodo |
| 16 a 20 | Operaciones 11 a 15, una captura por operación |
| 21 | Swagger con los endpoints |
| 22 | Vista en teléfono |
| 23 | Diagrama de clases (modelo, interfaces, controladores). No es una captura, se dibuja |
| 24 | Diagrama de capas MVC en servidor y cliente. No es una captura, se dibuja |

Las capturas se toman con el navegador integrado sobre los datos de `POST /api/tree/seed`, para que sean reproducibles. Se guardan en `docs/capturas/` con el nombre `figNN_tema.png`.

**Exportación.** Del Markdown a .docx con pandoc y un `reference.docx` con los estilos de la escuela. Antes de empezar se comprueba que pandoc está instalado. La portada va en página aparte con un salto de página explícito, y se revisa visualmente el archivo resultante. Si el docente pide PDF, se genera desde el mismo .docx.

**Bloqueo de entrega.** Ningún campo de la portada puede quedar como marcador al entregar. Los títulos del menú para las operaciones 1 a 10 se comparan con la redacción exacta de la instrucción original.

---

## 6. Riesgos

| Riesgo | Efecto | Respuesta |
|---|---|---|
| Un componente de Asciify no es gratuito o no funciona sin framework | Falta un efecto previsto | Fase 0 lo detecta. Se reemplaza con CSS y se informa en el documento |
| El fondo con GPU ralentiza equipos modestos | Interfaz lenta durante la demostración | Interruptor de fondo y plan B estático |
| Árbol degenerado muy profundo | Desbordamiento de pila o dibujo ilegible | Tope de nodos y desplazamiento controlado en la vista |
| `disintegrate` solo opera sobre elementos HTML | No se puede animar un nodo SVG | Los nodos son `div` desde el diseño |
| Estado compartido en memoria | Dos pestañas ven el mismo árbol | Se acepta y se documenta. No hay usuarios en el alcance |
| Divergencia entre la vista y los datos | Árbol dibujado distinto al real | Las respuestas de escritura devuelven el árbol y el frontend redibuja siempre desde esa respuesta, sin mantener una copia propia |
| Límite de profundidad del JSON | Error al serializar árboles degenerados | Lista plana de nodos y prueba con más de 64 niveles |
| Lectura durante una escritura | Recorrido con resultado inconsistente | `ReaderWriterLockSlim` en todas las operaciones |
| Quien califica no tiene Node | La aplicación no arranca | El build de Vite se sirve desde `wwwroot` y basta `dotnet run` |

---

## 7. Criterios de terminado

- Las 15 operaciones funcionan desde la interfaz y desde Swagger.
- `dotnet test` pasa y cubre los tres casos de borrado.
- Ninguna operación falla ni deja la pantalla vacía con el árbol vacío.
- Sin errores en la consola del navegador en un recorrido completo.
- El documento tiene portada completa, las 24 figuras con descripción y el código de las tres capas.
- El código respeta la estructura MVC en servidor y cliente. Los controladores solo dependen de interfaces, y las pruebas lo comprueban con dobles.
- Los cinco principios SOLID tienen una clase concreta y una prueba que los respalda, y el CRUD está cubierto por `POST`, `GET`, `PUT` y `DELETE`.
- `dotnet run` levanta la aplicación completa, y el README explica los pasos.
- El repositorio de GitHub tiene el código, el README, el plan y el documento final, y el último commit coincide con lo entregado.
- El documento declara qué componentes de Asciify se usaron.

---

## 8. Orden sugerido y estimación

| Fase | Duración estimada |
|---|---|
| 0. Verificación de Asciify | 0.5 día |
| 1. Modelo, POO y SOLID, con pruebas | 1.5 días |
| 2. Controladores y API | 1 día |
| 3. Esqueleto del frontend | 1 día |
| 4. Operaciones y animaciones | 1.5 días |
| 5. Pulido y comprobación | 0.5 día |
| 6. Documento | 1 día |

Total: 7 días, sin margen. Las cifras son estimaciones para una persona y dependen sobre todo de lo que revele la Fase 0. No hay fecha de entrega registrada, así que antes de empezar se anota y se reserva al menos un día de margen.

**Lista de recortes** si el tiempo se acaba, en este orden: fondo animado, transición de página, contadores con odómetro, gráfica por nivel. Las 15 operaciones, el dibujo del árbol y el documento no se recortan.

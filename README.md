# Árbol Binario de Búsqueda (ABB)

Aplicación web para dibujar y operar un árbol binario de búsqueda. El servidor es una API REST en C# (ASP.NET Core, .NET 10) y la interfaz está en JavaScript con componentes de [Asciify](https://asciify.org/components).

Incluye las 15 operaciones de la instrucción: imprimir, buscar, insertar, borrar, los cuatro recorridos, número de niveles, nivel de un nodo, y cinco operaciones propias (mínimo y máximo, contar nodos y hojas, ancestros de un nodo, verificar balance y ancestro común más bajo). Además tiene una operación de actualizar valor para completar el CRUD.

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).
- Un navegador reciente con aceleración de GPU. Sin GPU la aplicación funciona, pero los efectos de Asciify no se dibujan.
- Node.js solo si vas a modificar el frontend. Para ejecutar la aplicación no hace falta, porque el frontend ya compilado está en `src/Abb.Api/wwwroot`.

## Cómo ejecutarlo

1. Clona el repositorio y entra a la carpeta:

   ```powershell
   git clone https://github.com/Juulren/ABB.git
   cd ABB
   ```

2. Inicia la aplicación:

   ```powershell
   dotnet run --project src/Abb.Api --launch-profile http
   ```

3. Abre en el navegador http://localhost:5146.

4. Pulsa **Ejemplo** para cargar un árbol de 10 nodos, o escribe un valor entre -999 y 999 y pulsa **Insertar**. Con **Ctrl K** se abre el menú con las 15 operaciones.

Para detener el servidor, pulsa Ctrl+C en la terminal.

Swagger, con todos los endpoints de la API, está en http://localhost:5146/swagger. Solo se activa en el entorno de desarrollo, que es el que usa el perfil `http`.

Si el puerto 5146 está ocupado, cambia `applicationUrl` en `src/Abb.Api/Properties/launchSettings.json`.

## Pruebas

```powershell
dotnet test src/Abb.Tests
```

Son 82 pruebas: 50 del modelo y los servicios, y 32 de integración sobre la API.

## Desarrollo del frontend

Requiere Node.js. Con la API en marcha (paso 2 de arriba), en otra terminal:

```powershell
npm --prefix web install
npm --prefix web run dev
```

Vite sirve la interfaz en http://localhost:5173 y reenvía las peticiones `/api` a la API. Para actualizar la versión que sirve la API:

```powershell
npm --prefix web run build
```

El build se copia a `src/Abb.Api/wwwroot`.

## Estructura

- `src/Abb.Core`: modelo (nodo, árbol, recorridos, consultas y servicios).
- `src/Abb.Api`: controladores, DTO y `Program.cs`.
- `src/Abb.Tests`: pruebas xUnit.
- `web/`: cliente, organizado en `model`, `view` y `controller`.
- `docs/`: documento del proyecto (`Proyecto_ABB.md` y `Proyecto_ABB.docx`) y capturas.
- `PLAN_IMPLEMENTACION.md`: plan de implementación.

## Problemas frecuentes

- **`dotnet` no se reconoce o pide otra versión del SDK.** Instala el SDK de .NET 10 y abre una terminal nueva. `dotnet --list-sdks` debe mostrar una versión 10.
- **Con el servidor de Vite, arriba dice «Sin conexión».** La API no está en ejecución. Inicia la API con el paso 2.
- **No se ven los efectos animados.** Es el comportamiento esperado sin aceleración de GPU. Las operaciones siguen funcionando.
- **El árbol se pierde al reiniciar.** El árbol vive en la memoria del servidor y no se guarda en disco.

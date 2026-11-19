# Árbol Binario de Búsqueda (ABB)

Aplicación web con API REST en C# (ASP.NET Core, .NET 10) y frontend en JavaScript con componentes de [Asciify](https://asciify.org/components).

## Ejecutar

Hace falta el SDK de .NET 10. El frontend ya compilado está en `src/Abb.Api/wwwroot`, así que no se necesita Node para usarlo.

```powershell
dotnet run --project src/Abb.Api --launch-profile http
```

Abre http://localhost:5146. Swagger está en http://localhost:5146/swagger (solo en desarrollo).

## Pruebas

```powershell
dotnet test src/Abb.Tests
```

## Desarrollo del frontend

Requiere Node. Con la API en marcha:

```powershell
npm --prefix web install
npm --prefix web run dev      # http://localhost:5173, con proxy de /api a la API
npm --prefix web run build    # copia el build a src/Abb.Api/wwwroot
```

## Estructura

- `src/Abb.Core`: modelo (nodo, árbol, recorridos, consultas, servicios).
- `src/Abb.Api`: controladores, DTO y `Program.cs`.
- `src/Abb.Tests`: pruebas xUnit.
- `web/`: cliente (`model`, `view`, `controller`).
- `docs/`: documento del proyecto (`Proyecto_ABB.md` y `.docx`) y capturas.
- `PLAN_IMPLEMENTACION.md`: plan de implementación.

Los efectos de Asciify necesitan WebGL2 con aceleración de GPU. Sin ella la interfaz sigue funcionando, sin esos efectos.

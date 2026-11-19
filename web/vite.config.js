import { defineConfig } from 'vite';

// En desarrollo, Vite reenvía /api a la API de .NET. Para entregar, el build va a wwwroot
// y la API lo sirve con UseStaticFiles (basta con `dotnet run`).
export default defineConfig({
  server: { port: 5173, proxy: { '/api': 'http://localhost:5146' } },
  build: { outDir: '../src/Abb.Api/wwwroot', emptyOutDir: true },
});

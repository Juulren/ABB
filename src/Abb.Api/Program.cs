using Abb.Core.Estructura;
using Abb.Core.Recorridos;
using Abb.Core.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Inyección de dependencias: los controladores solo conocen las interfaces.
builder.Services.AddSingleton<ArbolBinarioBusqueda>();
builder.Services.AddSingleton<IRecorrido, Preorden>();
builder.Services.AddSingleton<IRecorrido, Inorden>();
builder.Services.AddSingleton<IRecorrido, Postorden>();
builder.Services.AddSingleton<IRecorrido, PorNiveles>();
builder.Services.AddSingleton<ArbolService>();
builder.Services.AddSingleton<IArbolComandos>(sp => sp.GetRequiredService<ArbolService>());
builder.Services.AddSingleton<IArbolConsultas>(sp => sp.GetRequiredService<ArbolService>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// El build del frontend se copia a wwwroot, así que `dotnet run` sirve todo.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();

// Necesario para WebApplicationFactory en las pruebas de integración.
public partial class Program;

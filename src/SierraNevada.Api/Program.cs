using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SierraNevada.Application.Produccion.Calibracion;
using SierraNevada.Application.Produccion.CasosDeUso;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Application.Usuarios;
using SierraNevada.Application.Usuarios.Abstractions;
using SierraNevada.Application.Usuarios.Repositories;
using SierraNevada.Infrastructure.Persistence;
using SierraNevada.Infrastructure.Produccion;
using SierraNevada.Infrastructure.Usuarios;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SierraNevadaDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'SierraNevadaDb'. Configúrala en appsettings.Development.json " +
        "(local) o en la variable de entorno ConnectionStrings__SierraNevadaDb (producción/VPS).");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' de configuración.");

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
    throw new InvalidOperationException(
        "Falta 'Jwt:SecretKey'. Configúralo en appsettings.Development.json (local) o en la " +
        "variable de entorno Jwt__SecretKey (producción/VPS) — nunca comitear el valor real de producción.");

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        };
    });

builder.Services.AddAuthorization();

// CORS — el frontend (React) corre en un origen distinto al de la API.
// Orígenes permitidos por configuración: appsettings.Development.json en local,
// variable de entorno Cors__AllowedOrigins__0 (etc.) en producción/VPS.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
});

// Acceso a datos (ADO.NET puro — ver docs/arquitectura-tecnica.md sección 3).
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));

// Repositorios del módulo Producción.
builder.Services.AddScoped<IUnidadProduccionRepository, UnidadProduccionRepository>();
builder.Services.AddScoped<IBastidorRepository, BastidorRepository>();
builder.Services.AddScoped<ICampañaRepository, CampañaRepository>();
builder.Services.AddScoped<ILoteRepository, LoteRepository>();
builder.Services.AddScoped<IMuestreoRepository, MuestreoRepository>();
builder.Services.AddScoped<IRegistroAlimentacionRealRepository, RegistroAlimentacionRealRepository>();
builder.Services.AddScoped<IRegistroMortalidadRepository, RegistroMortalidadRepository>();
builder.Services.AddScoped<IRegistroCondicionesRepository, RegistroCondicionesRepository>();
builder.Services.AddScoped<IHistorialMovimientoRepository, HistorialMovimientoRepository>();
builder.Services.AddScoped<IEnfermedadRepository, EnfermedadRepository>();
builder.Services.AddScoped<ITablaReferenciaVersionRepository, TablaReferenciaVersionRepository>();

// Repositorios del módulo Inventario y Kardex.
builder.Services.AddScoped<SierraNevada.Application.Inventario.Repositories.ITipoAlimentoRepository, SierraNevada.Infrastructure.Inventario.TipoAlimentoRepository>();
builder.Services.AddScoped<SierraNevada.Application.Inventario.Repositories.IProveedorAlimentoRepository, SierraNevada.Infrastructure.Inventario.ProveedorAlimentoRepository>();
builder.Services.AddScoped<SierraNevada.Application.Inventario.Repositories.ILoteAlimentoRepository, SierraNevada.Infrastructure.Inventario.LoteAlimentoRepository>();
builder.Services.AddScoped<SierraNevada.Application.Inventario.Repositories.IKardexAlimentoRepository, SierraNevada.Infrastructure.Inventario.KardexAlimentoRepository>();

// Casos de uso del módulo Producción y ML.
builder.Services.AddScoped<RegistrarMuestreoService>();
builder.Services.AddScoped<RegistrarAlimentacionRealService>();
builder.Services.AddScoped<RegistrarMortalidadService>();
builder.Services.AddScoped<RealizarSeleccionService>();
builder.Services.AddScoped<CambiarEtapaService>();
builder.Services.AddScoped<CrearCampañaConLotesService>();
builder.Services.AddScoped<CalcularCalibracionLoteService>();
builder.Services.AddScoped<SierraNevada.Application.Produccion.Reportes.CalcularReporteProduccionGlobalService>();
builder.Services.AddScoped<CalcularProyeccionTgcBayesianoService>();

// Casos de uso del módulo Inventario y Reorden.
builder.Services.AddScoped<SierraNevada.Application.Inventario.CasosDeUso.RegistrarIngresoAlimentoService>();
builder.Services.AddScoped<SierraNevada.Application.Inventario.CasosDeUso.RegistrarEgresoAlimentoService>();
builder.Services.AddScoped<SierraNevada.Application.Inventario.CasosDeUso.CalcularPlanReordenService>();

// Módulo de usuarios/autenticación (propia, no ASP.NET Core Identity — ver docs/arquitectura-tecnica.md sección 6).
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<AutenticacionService>();

var app = builder.Build();

// Aplica los scripts SQL pendientes (DbUp) — reemplaza las migraciones automáticas de EF Core.
DatabaseMigrator.MigrateDatabase(connectionString);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

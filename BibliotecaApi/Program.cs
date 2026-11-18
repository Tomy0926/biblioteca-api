using BibliotecaApi;
using BibliotecaApi.Datos;
using BibliotecaApi.Entidades;
using BibliotecaApi.Servicios;
using BibliotecaApi.Servicios.V1;
using BibliotecaApi.Swagger;
using BibliotecaApi.Utilidades;
using BibliotecaApi.Utilidades.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//Area de servicios

builder.Services.AddOutputCache(opciones =>
{
    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
});

//builder.Services.AddStackExchangeRedisOutputCache(opciones =>
//{
//    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
//});

builder.Services.AddDataProtection();

var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(opcionesCORS =>
    {
        opcionesCORS.WithOrigins(origenesPermitidos).AllowAnyMethod().AllowAnyHeader()
        .WithExposedHeaders("cantidad-total-registros");
    });
});

builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

builder.Services.AddControllers(opciones =>
{
    opciones.Conventions.Add(new ConvencionAgrupaPorVersion());
}).AddNewtonsoftJson();

builder.Services.AddDbContext <ApplicationDbContext>(opciones =>
opciones.UseSqlServer("name=DefaultConnection"));

builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>();
builder.Services.AddScoped<SignInManager<Usuario>>();
builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
builder.Services.AddScoped<FiltroValidacionLibro>();
builder.Services.AddScoped<BibliotecaApi.Servicios.V1.IServicioAutores,
    BibliotecaApi.Servicios.V1.ServicioAutores>();

builder.Services.AddScoped<BibliotecaApi.Servicios.V1.IGeneradorEnlaces,
    BibliotecaApi.Servicios.V1.GeneradorEnlaces>();

builder.Services.AddScoped<HATEOASAutorAttribute>();

builder.Services.AddScoped<HATEOASAutoresAttribute>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;

    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esadmin", politica =>
    {
    politica.RequireClaim("esadmin", "1");
    });
});

builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Biblioteca API",
        Description = "Web api para trabajar con datos de autores y libros",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Tomy",
            Email = "tomydelossantos0926@gmail.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/license/mit/")
        }
    });
    opciones.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v2",
        Title = "Biblioteca API",
        Description = "Web api para trabajar con datos de autores y libros",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Tomy",
            Email = "tomydelossantos0926@gmail.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/license/mit/")
        }
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    opciones.OperationFilter<FiltroAutorizacion>();

    //opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        new string[]{}
    //    }
    //});

});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (dbcontext.Database.IsRelational())
    {
        dbcontext.Database.Migrate();
    }
}

//Area de middlewares

app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
    var exception = exceptionHandlerFeature?.Error!;

    var error = new Error()
    {
        MensajeError = exception.Message,
        StrackTrace = exception.StackTrace,
        Fecha = DateTime.UtcNow
    };

    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(error);
    await dbContext.SaveChangesAsync();
    await Results.InternalServerError(new
    {
        tipo = "error",
        mensaje = "Ha ocurrido un error inesperado",
        estatus = 500
    }).ExecuteAsync(context);
}));

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API V1");
    opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API V2");
});

app.UseStaticFiles();

app.UseCors();

app.UseOutputCache();

app.MapControllers();

app.Run();

public partial class Program { }
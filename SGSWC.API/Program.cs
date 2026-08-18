using Microsoft.IdentityModel.Tokens;
using SGSWC.API.Middlewares;
using System.Text;
using SGSWC.API.Services;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddSingleton<MonitorRendimientoService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

string key = builder.Configuration["Valores:KeyJWT"]!;

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var config = builder.Configuration;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<MonitoreoRendimientoMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

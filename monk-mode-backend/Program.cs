using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using monk_mode_backend.Application;
using monk_mode_backend.Domain;
using monk_mode_backend.Hubs;
using monk_mode_backend.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(MappingProfile));

var connectionString = builder.Configuration.GetConnectionString("postgresql");
builder.Services.AddDbContext<MonkModeDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSignalR();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<ITokenService, JWTService>();

// Identity: Password policy hardened (Dozentenfeedback)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;

    // Hardened password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<MonkModeDbContext>();

// JWT Authentication: Issuer/Audience/Expiration strictly validated
builder.Services.AddAuthentication(a =>
{
    a.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    a.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    a.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    var secret = builder.Configuration["JwtSettings:Secret"]!;
    var issuer = builder.Configuration["JwtSettings:Issuer"]!;
    var audience = builder.Configuration["JwtSettings:Audience"]!;

    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),

        ValidateIssuer = true,
        ValidIssuer = issuer,

        ValidateAudience = true,
        ValidAudience = audience,

        RequireExpirationTime = true,
        ValidateLifetime = true,

        // klein halten, damit Expiry strikt bleibt
        ClockSkew = TimeSpan.FromMinutes(2)
    };

    // JWT für SignalR (Query-Token bei /hubs)
    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Swagger: Bearer-Schema
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS: Dev (localhost/Expo) vs. Prod (Whitelist aus Config)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:19006",
                "http://127.0.0.1:19006")
            .AllowAnyHeader()
            .AllowAnyMethod()
    // Für Bearer-Auth keine Cookies/Credentials nötig:
    // -> kein .AllowCredentials()
    );

    options.AddPolicy("ProdCors", policy =>
    {
        var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowed.Length == 0)
        {
            // Fail-safe: im Zweifel niemanden erlauben statt AnyOrigin
            policy.WithOrigins(Array.Empty<string>());
        }
        else
        {
            policy.WithOrigins(allowed);
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod();
        // Keine Credentials, solange ihr nur Bearer-Header nutzt
    });
});

var app = builder.Build();

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}
else
{
    app.UseCors("ProdCors");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// app.UseHttpsRedirection();

app.Run();

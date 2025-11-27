using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using monk_mode_backend.Application;
using monk_mode_backend.Domain;
using monk_mode_backend.Hubs;
using monk_mode_backend.Infrastructure;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var connectionString = builder.Configuration.GetConnectionString("postgresql")
    ?? throw new InvalidOperationException("ConnectionStrings:postgresql is not configured.");
if (string.IsNullOrWhiteSpace(connectionString)) {
    throw new InvalidOperationException("ConnectionStrings:postgresql cannot be empty. Provide it via configuration or environment variables.");
}

builder.Services.AddDbContext<MonkModeDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSignalR();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
}).AddEntityFrameworkStores<MonkModeDbContext>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings section is missing.");
if (string.IsNullOrWhiteSpace(jwtSettings.Secret)) {
    throw new InvalidOperationException("JwtSettings:Secret is not configured or empty.");
}
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer)) {
    throw new InvalidOperationException("JwtSettings:Issuer is not configured or empty.");
}
if (string.IsNullOrWhiteSpace(jwtSettings.Audience)) {
    throw new InvalidOperationException("JwtSettings:Audience is not configured or empty.");
}
var jwtSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret));

builder.Services.AddAuthentication(a => {
    a.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    a.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    a.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt => {
    opt.TokenValidationParameters = new TokenValidationParameters {
        IssuerSigningKey = jwtSigningKey,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // JWT validation für SignalR
    opt.Events = new JwtBearerEvents {
        OnMessageReceived = context => {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) {
                context.Token = accessToken;
            }

            return System.Threading.Tasks.Task.CompletedTask; // Fully qualify Task
        }
    };
});

builder.Services.AddSwaggerGen(opt => {
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme() {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below."
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
          {
          {
             new OpenApiSecurityScheme
              {
                Reference = new OpenApiReference
                {
                  Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"
                }
              }, new string[] {}
          }
        });
});
builder.Services.AddScoped<ITokenService, JWTService>();

// Enable CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var allowedOrigins = corsOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options => {
    options.AddPolicy("FrontendCorsPolicy", policy => {
        var origins = allowedOrigins;

        if (builder.Environment.IsDevelopment()) {
            if (origins.Length == 0) {
                origins = new[] {
                    "http://localhost:3000",
                    "http://localhost:19006"
                };
            }

            var devPolicy = policy.WithOrigins(origins)
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();

            devPolicy.AllowCredentials();
        } else {
            if (origins.Length == 0) {
                throw new InvalidOperationException("Cors:AllowedOrigins must be configured for production.");
            }

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

//app.UseHttpsRedirection();
app.Run();

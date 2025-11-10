using MathSlides.Data_Analysis_Object;
using MathSlides.Repository.Interfaces;
using MathSlides.Repository.Repositories;
using MathSlides.Service.Interfaces;
using MathSlides.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System; 
using System.IO;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var authConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=MathSlidesAuthDB;Trusted_Connection=True;MultipleActiveResultSets=true";
builder.Services.AddDbContext<MathSlidesAuthDbContext>(options =>
    options.UseSqlServer(authConnectionString));

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IGDPTRepository, GDPTRepository>();
builder.Services.AddScoped<IPowerpointRepository, PowerpointRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();

builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGDPTService, GDPTService>();
builder.Services.AddScoped<IPowerpointService, PowerpointService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();

builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});


builder.Services.AddScoped<ISlideGenerationService, SlideGenerationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MathSlides Auth API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });


    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

});


var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is required");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}
var templatesPath = Path.Combine(wwwrootPath, "Templates");
if (!Directory.Exists(templatesPath))
{
    Directory.CreateDirectory(templatesPath);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
using Microsoft.EntityFrameworkCore;
using pethub.Data;

var builder = WebApplication.CreateBuilder(args);

// Tenta ler a variável de ambiente (Para Docker / Render)
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// Se não achou (está rodando local sem docker), pega do appsettings.json
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// Injeta o DbContext usando o driver do MySQL (Pomelo)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Adiciona os Controllers
builder.Services.AddControllers();

// Configura o Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configura o pipeline de requisições HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers(); // Mapeia seus endpoints

app.Run();
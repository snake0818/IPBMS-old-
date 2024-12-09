using Microsoft.EntityFrameworkCore;
using PigDB_API.Data;
using PigDB_API.Services;

var builder = WebApplication.CreateBuilder(args);
var CorsPolicyName = "AllowAllPolicy";

// Add services to the container.

builder.Services.AddHttpClient();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsPolicyName,
    builder =>
    {
        builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

builder.WebHost.ConfigureKestrel(serverOptions =>
    serverOptions.Limits.MaxRequestBodySize = 1024 * 1024 * 1024);

builder.Services.AddDbContext<PigDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddTransient<SettingService>();
// builder.Services.AddScoped<SettingService>();
builder.Services.AddSingleton<SettingService>();

var app = builder.Build();
app.UseCors(CorsPolicyName);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


using ServiceInventory.Models;
using ServiceInventory.Controllers;
using ServiceInventory.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.Configure<MessageBus>(builder.Configuration.GetSection("MessageBus"));
builder.Services.AddSingleton<ProductItemService>();
builder.Services.AddSingleton<ProductInfoService>();
//builder.Services.AddHostedService<TimedHostedService>();
builder.Services.AddHostedService<ConsumeScopedServiceHostedService>();
builder.Services.AddScoped<IScopedProcessingService, ScopedProcessingService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

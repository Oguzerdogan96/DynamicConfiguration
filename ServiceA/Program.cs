using DynamicConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton(provider =>
    new ConfigurationReader(
        applicationName: "SERVICE-A",
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        refreshTimerIntervalInMs: 5000)
);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles(); 
app.UseStaticFiles();      // <-- wwwroot'tan dosya sunmak için
    // <-- index.html otomatik açýlýr

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger"; // Bu satýr Swagger UI'nýn /swagger yolunda açýlmasýný saðlar
    });
}

app.UseHttpsRedirection();


app.UseAuthorization();

app.MapControllers();

app.Run();

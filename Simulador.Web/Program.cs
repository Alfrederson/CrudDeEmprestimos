using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulador.Core.Data;
using Simulador.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi()
    .AddSwaggerGen( options => options.EnableAnnotations() )
    .AddDbContext<SimuladorDbContext>( options =>
    {
        options.UseSqlite(new SqliteConnection("DataSource=file:memdb1?mode=memory&cache=shared"));
    })
    .AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<SimuladorDbContext>().Database.EnsureCreated();
    }
    app.MapOpenApi();
    app.MapSwagger();
    app.UseSwaggerUI( options =>
    {
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseMiddleware<ExceptionToResponse>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

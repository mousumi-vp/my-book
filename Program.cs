using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using my_books.Data;
using my_books.Data.Services;
using my_books.Exceptions;
using Serilog;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

try
{
    var configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json")
                   .Build();
    // Configure Serilog
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();

    // Add Serilog to the builder
    builder.Host.UseSerilog(Log.Logger);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddTransient<BooksServices>();
    builder.Services.AddTransient<AuthorsService>();
    builder.Services.AddTransient<PublishersService>();
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
    app.ConfigureBuildInExceptionHandler(app.Services.GetRequiredService<ILoggerFactory>());

    app.MapControllers();
    //AppDbInitializer.Seed(app);

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
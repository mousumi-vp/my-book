using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using my_books.Data;
using my_books.Data.Services;
using my_books.Exceptions;
using Serilog;
using System.Configuration;
using System.Text;

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
    builder.Services.AddMemoryCache();
    builder.Services.AddTransient<MemoryService>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin",
            builder =>
            {
                builder.WithOrigins("https://example.com")
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
    });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    var tokenValidationParameter= new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["JWT:Secret"])),

        ValidateIssuer = true,
        ValidIssuer = configuration["JWT:Issuer"],

        ValidateAudience = true,
        ValidAudience = configuration["JWT:Audience"],

        ValidateLifetime = true,
        ClockSkew=TimeSpan.Zero
    };
    builder.Services.AddSingleton(tokenValidationParameter);
    //Add Identity
    builder.Services.AddIdentity<my_books.Data.Model.ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    //Add Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    //Add JWT Bearer
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = tokenValidationParameter;
    });
    builder.Services.AddSwaggerGen();

    var app = builder.Build();


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.ConfigureBuildInExceptionHandler(app.Services.GetRequiredService<ILoggerFactory>());

    app.MapControllers();
    //AppDbInitializer.Seed(app);
    AppDbInitializer.SeedRoles(app).Wait();

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
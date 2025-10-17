using PxApiConverter.Models;
using PxApiConverter.Business;
using PxApiConverter.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging (add file logger before building services)
builder.Logging.AddSimpleFile();

// Bind PxApi options
builder.Services.Configure<PxApiOptions>(builder.Configuration.GetSection("PxApi"));

// Register converters
builder.Services.AddScoped<IApiConverter, ApiConverter>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Bind file logger options from configuration section "FileLogging" if present
builder.Services.Configure<FileLoggerOptions>(builder.Configuration.GetSection("FileLogging"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

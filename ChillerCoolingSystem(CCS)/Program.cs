using System.Text.Json;
using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Hubs;
using ChillerCoolingSystem_CCS_.Repositories;
using ChillerCoolingSystem_CCS_.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    // camelCase để khớp tên thuộc tính JS đang đọc (good/value).
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddSingleton<MachineLatestValuesStore>();
builder.Services.AddHostedService<KepwareSmokeTestWorker>();

builder.Services.AddDbContextFactory<HistoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HistoryDb")));
builder.Services.AddHostedService<HistoryLoggingWorker>();

builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddScoped<IMachineRepository, MachineRepository>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddScoped<IExportService, ExportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChillerRealtimeHub>("/hubs/chiller-realtime");

app.Run();

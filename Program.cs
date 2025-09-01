using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AdminP;
using AdminP.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure backend API base URL
builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri("http://192.168.1.87:5100")
    });
builder.Services.AddScoped<IComputerService, ComputerService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMetricsHubService, MetricsHubService>();

// Регистрируем сервис аутентификации
builder.Services.AddScoped<IAuthService, AuthService>();



await builder.Build().RunAsync();

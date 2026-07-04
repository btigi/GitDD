using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GitDD;
using GitDD.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CharacterBuilder>();
builder.Services.AddScoped<ProfileCacheService>();
builder.Services.AddScoped<GitHubService>(sp =>
{
    var http = new HttpClient
    {
        BaseAddress = new Uri("https://api.github.com/")
    };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("GitDD/1.0");
    http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    return new GitHubService(http, sp.GetRequiredService<ProfileCacheService>());
});

await builder.Build().RunAsync();

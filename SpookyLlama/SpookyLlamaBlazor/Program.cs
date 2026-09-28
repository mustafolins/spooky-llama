using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpookyLlamaBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var appBaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddScoped(_ => new HttpClient
{
	BaseAddress = new Uri(appBaseAddress, "_api/api/")
});
builder.Services.AddScoped(_ => new SpeechClient(new HttpClient
{
	BaseAddress = new Uri(appBaseAddress, "_api/speech/")
}));
builder.Services.AddScoped(_ => new ImageClient(new HttpClient
{
	BaseAddress = new Uri(appBaseAddress, "_api/image/")
}));

await builder.Build().RunAsync();

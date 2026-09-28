using SpookyLlamaBlazor;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https+http://api");
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddScoped(provider =>
    provider.GetRequiredService<IHttpClientFactory>().CreateClient("api"));
builder.Services.AddHttpClient<SpeechClient>(client =>
{
    client.BaseAddress = new Uri("https+http://speech");
    client.Timeout = TimeSpan.FromMinutes(3);
});
var imageEndpoint = builder.Configuration["services:image:https:0"]
    ?? builder.Configuration["services:image:http:0"];
if (imageEndpoint is null && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("The Aspire image service endpoint is not configured.");
}

imageEndpoint ??= "http://localhost:8000";
builder.Services.AddScoped(_ => new ImageClient(new HttpClient
{
    BaseAddress = new Uri(imageEndpoint),
    Timeout = TimeSpan.FromMinutes(3)
}));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();

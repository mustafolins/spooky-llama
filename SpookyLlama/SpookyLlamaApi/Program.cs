using SpookyLlamaCommon;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(x => x
  .AllowAnyMethod()
  .AllowAnyHeader()
  .AllowCredentials()
  .SetIsOriginAllowed(origin => true)); // allow any origin

app.UseHttpsRedirection();

app.MapPost("/api/spookyllama", async (SpookyLlamaRequest request) =>
{
    var spookyLlamaManager = new SpookyLlamaManager();

    if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest("Invalid request. Please provide a valid prompt.");
    }
    try
    {
        var response = await spookyLlamaManager.GetSpookyLlamaResponseAsync(request.Prompt, request.Context);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while processing the request: {ex.Message}");
    }
})
    .WithName("GetSpookyLlamaResponse")
    .WithOpenApi();

app.Run();

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

var context = new List<long>();
var responses = new List<string>();

// Endpoint to generate SpookyLlama response
app.MapPost("/api/spookyllama", async (SpookyLlamaRequest request) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest("Invalid request. Please provide a valid prompt.");
    }
    try
    {
        // Get the response from SpookyLlamaManager and add it to the responses list
        responses.Add(await SpookyLlamaManager.GetSpookyLlamaResponseAsync(request.Prompt, context));
        return Results.Created();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while processing the request: {ex.Message}");
    }
})
    .WithName("GenerateSpookyLlamaResponse")
    .WithOpenApi();

// Endpoint to get all SpookyLlama responses
app.MapDelete("/api/spookyllama/context", () =>
{
    context.Clear();
    responses.Clear();
    return Results.Ok("Context has been reset.");
})
    .WithName("ResetSpookyLlamaContext")
    .WithOpenApi();

// Endpoint to get all SpookyLlama responses
app.MapGet("/api/spookyllama/responses", () =>
{
    return Results.Ok(responses);
})
    .WithName("GetSpookyLlamaResponses")
    .WithOpenApi();

// Endpoint to get the latest SpookyLlama response
app.MapGet("/api/spookyllama/response", () =>
{
    if (responses.Count == 0)
    {
        return Results.Ok("No responses available.");
    }
    return Results.Ok(responses.Last());
})
    .WithName("GetLatestSpookyLlamaResponse")
    .WithOpenApi();

// Endpoint to reset SpookyLlama responses
app.MapDelete("/api/spookyllama/responses", () =>
{
    responses.Clear();
    return Results.Ok("All responses have been cleared.");
})
    .WithName("ClearSpookyLlamaResponses")
    .WithOpenApi();

app.Run();

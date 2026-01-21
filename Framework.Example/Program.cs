using Framework.Generated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

//Adds Mediator for request forwarding to handlers
//Auto registers all handlers. Throws compile time error when handler not available
builder.Services.AddSingletonMediatorServices();
builder.Services.AddSingletonRepositoryServices();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//Adds auto generated minimal api endpoints
app.AddApiEndpoints();

app.Run();
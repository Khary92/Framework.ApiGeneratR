using Framework.Contract.Documentation;
using Framework.Generated;
using Framework.Reusables.Websocket;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

//Adds Mediator for request forwarding to handlers
//Auto registers all handlers. Throws compile time error when handler not available
builder.Services.AddSingletonMediatorServices();
builder.Services.AddSingletonRepositoryServices();

// WebSocket Registry
builder.Services.AddSingleton<WebsocketRegistry>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//Adds auto generated minimal api endpoints
app.AddApiEndpoints();
app.AddWebsocket();

new DocumentationWriter(app.Services.GetServices<IDocumentation>()).Write();

app.Run();

class DocumentationWriter(IEnumerable<IDocumentation> documents)
{
    public void Write()
    {
        var folderName = "Doku";

        if (!Directory.Exists(folderName))
        {
            Directory.CreateDirectory(folderName);
        }

        foreach (var doc in documents)
        {
            var filePath = Path.Combine(folderName, doc.FileName);
            File.WriteAllText(filePath, doc.Markdown);
        }
    }
}
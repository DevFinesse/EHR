using EHR.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.ApiGateway");
builder.Services.AddOpenApi();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapGet("/", () => Results.Ok(new { service = "EHR.ApiGateway", routes = "tenant, identity, patient, appointment, encounter, audit" }));
app.MapReverseProxy();
app.Run();

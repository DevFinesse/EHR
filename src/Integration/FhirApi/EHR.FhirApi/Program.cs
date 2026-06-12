using EHR.FhirApi;
using EHR.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.FhirApi");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<FhirUpstreamClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();

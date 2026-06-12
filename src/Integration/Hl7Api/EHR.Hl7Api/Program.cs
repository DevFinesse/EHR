using EHR.Hl7Api;
using EHR.Messaging;
using EHR.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.Hl7Api");
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new Hl7TextInputFormatter());
    options.OutputFormatters.Insert(0, new Hl7TextOutputFormatter());
});
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<Hl7PatientWorkflowClient>();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddSingleton<Hl7MessageParser>();
builder.Services.AddSingleton<Hl7AdtMessageBuilder>();
builder.Services.AddScoped<Hl7AdtWorkflowService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();

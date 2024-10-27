using DomestiaHA.Abstraction;
using DomestiaHA.Configuration.Extensions;
using DomestiaHA.MQTTClient;
using DomestiaHA.MQTTClient.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder( args );

builder.Services.AddDomestiaHAConfiguration( config =>
{
    config.ConfigurationFile = "config.json";
} );

builder.Services.AddSingleton<IHAMQTTService, HAMQTTService>();
builder.Services.AddSingleton<IDomestiaLightService, FakeDomestiaLightService>();
builder.Services.AddHostedService<DomestiaHAHostedService>();

var app = builder.Build();
app.Run();

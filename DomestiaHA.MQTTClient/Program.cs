using DomestiaHA.Abstraction;
using DomestiaHA.DomestiaProtocol;
using DomestiaHA.DomestiaProtocol.Extensions;
using DomestiaHA.MQTTClient;
using DomestiaHA.MQTTClient.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host
    .CreateApplicationBuilder( args );

builder.Services.AddSingleton<IHAMQTTService, HAMQTTService>();
builder.Services.AddSingleton<ILightService, DomestiaLightService>();
builder.Services.AddHostedService<DomestiaHAHostedService>();

builder.Services.Configure<DomestiaHAHosetedServiceConfiguration>( config =>
{
    config.BrokerIPAddress = builder.Configuration["MQTT_BROKER_IP_ADDRESS"]!;
    config.BrokerPort = int.Parse( builder.Configuration["MQTT_BROKER_PORT"]! );
} );

builder.Services.AddDomestiaLightService( config =>
{
    config.IpAddress = builder.Configuration["DOMESTIA_IP_ADDRESS"]!;
} );

var app = builder.Build();
app.Run();

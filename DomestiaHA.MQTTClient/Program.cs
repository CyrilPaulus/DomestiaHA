using DomestiaHA.MQTTClient;
using DomestiaHA.DomestiaProtocol;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder( args );
var config = builder.Configuration;

string Required( string key ) => config[key] is { Length: > 0 } value
    ? value
    : throw new InvalidOperationException( $"Missing configuration: {key}" );

var domestiaHost = Required( "DOMESTIA_IP_ADDRESS" );
var broker = new MqttBrokerSettings(
    Required( "MQTT_BROKER_IP_ADDRESS" ),
    int.TryParse( config["MQTT_BROKER_PORT"], out var port ) ? port : 1883 );

builder.Services.AddSingleton( _ => new DomestiaClient( domestiaHost ) );
builder.Services.AddSingleton( broker );
builder.Services.AddHostedService<DomestiaHAService>();

builder.Build().Run();

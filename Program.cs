// wire up host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Text2ImageSample;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
   {
       IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appSettings.json", false)
        .AddEnvironmentVariables()
        .Build();

       services.AddSingleton(configuration);
       services.AddSingleton<ImageConfig>();
       services.AddSingleton<AgentProvider>();
       services.AddHostedService<Main>();
   });

await builder.RunConsoleAsync();

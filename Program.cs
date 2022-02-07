using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AnalyticsParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            { 
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(0);
            }
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    WorkerOptions options = configuration.GetSection("MySettings").Get<WorkerOptions>();
                    services.AddSingleton(options);
                    services.AddHostedService<Worker>();
                })

                ;
    }
}

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using RESTar.Resources;
using Starcounter.Nova;

namespace RESTar.Example
{
    [Database, RESTar]
    public abstract class Person
    {
        public abstract string Name { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Startup>();
    }
}
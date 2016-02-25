using System;
using System.IO;
using System.Linq;
using Npgsql;
using Marten;
using Topshelf;
using Topshelf.Nancy;
using Nancy;
using log4net.Config;
using Ferret.Sample.Domain;
using log4net;
using Nancy.ModelBinding;

namespace Ferret.Sample
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            string target = GetTargetConnectionString();
            string master = GetMasterConnectionString(target);
            var initializer = new Initializer(master, target);

            if (args != null && args.Any(x => x.ToLowerInvariant() == "--tear-down"))
            {
                Log.Info("Tearing down...");
                initializer.TearDown();
                return;
            }
            else if (args != null && args.Any(x => x.ToLowerInvariant() == "--drop"))
            {
                Log.Info("Dropping...");
                initializer.Drop();
                return;
            }

            Log.Info("Initializing...");
            var store = new Initializer(master, target).Initialize(ConfigureSchema);

            var host = HostFactory.New(x =>
            {
                x.UseLog4Net();
                x.Service<SampleService>(s =>
                {
                    s.ConstructUsing(settings => new SampleService());
                    s.WhenStarted(service => service.Start());
                    s.WhenStopped(service => service.Stop());
                    s.WithNancyEndpoint(x, c =>
                    {
                        c.Bootstrapper = new Bootstrapper(store);
                        c.AddHost(port: 8585);
                        c.CreateUrlReservationsOnInstall();
                    });
                });
                x.StartAutomatically();
                x.SetServiceName("Ferret.Sample");
                x.RunAsNetworkService();
            });

            host.Run();
        }
 
        private class SampleService
        {
            public bool Start()
            {
                return true;
            }
            public bool Stop()
            {
                return true;
            }
        }

        public static string GetMasterConnectionString(string target)
        {
            var builder = new NpgsqlConnectionStringBuilder(target);
            builder.Database = "postgres";
            var master = builder.ConnectionString;
            return master;
        }

        public static string GetTargetConnectionString()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connection.txt");
            if (!File.Exists(path))
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                path = Path.Combine(dir.Parent.Parent.FullName, "connection.txt");
            }

            var target = File.ReadLines(path).First();
            return target;
        }

        static void ConfigureSchema(MartenRegistry cfg)
        {

        }
    }

    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get["/"] = _ =>
            {
                return "Hello Ferret!";
            };
        }
    }

    public class MischiefModule : NancyModule
    {
        public MischiefModule(Mischief.Manager manager)
            : base("/mischief")
        {
            Get["/{mischiefId}"] = _ =>
            {

                return "Mischief status";
            };

            Post["/map-discovery"] = _ =>
            {
                var command = this.Bind<OpenMaraudersMap>();
                manager.When(command);
                return HttpStatusCode.OK;
            };

            Post["/room-of-requirement-visit"] = _ =>
            {
                var command = this.Bind<GotoRoomOfRequirement>();
                manager.When(command);
                return HttpStatusCode.OK;
            };
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Npgsql;
using Marten;
using Topshelf;
using Topshelf.Nancy;
using log4net.Config;
using log4net;

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

            if (args != null && args.Any(x => x.ToLowerInvariant() == "--init"))
            {
                Log.Info("Initializing...");
                var store = new Initializer(master, target).Initialize(ConfigureStore);
                Log.Debug(store.Schema.ToDDL());
                return;
            }
            else if (args != null && args.Any(x => x.ToLowerInvariant() == "--tear-down"))
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
                        c.Bootstrapper = new Bootstrapper(new Initializer(master, target).Initialize(ConfigureStore));
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

        static void ConfigureStore(StoreOptions register)
        {
            var stateTypes = typeof(Program)
                .Assembly
                .GetExportedTypes()
                .Where(x => x.Name == "State");
            register.RegisterDocumentTypes(stateTypes);
        }
    }
}

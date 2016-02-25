using System;
using System.IO;
using System.Linq;
using Npgsql;
using Marten;
using Topshelf;
using Topshelf.Nancy;
using Nancy;
using log4net.Config;
using Nancy.TinyIoc;
using Ferret.Sample.Domain;
using Nancy.Bootstrapper;
using log4net;
using Nancy.ModelBinding;

namespace Ferret.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            if (args != null && args.Any(x => x.ToLowerInvariant() == "--init"))
            {
                string target = GetTargetConnectionString();
                string master = GetMasterConnectionString(target);

                var store = new Initializer(master, target).Initialize(ConfigureSchema);

                return;
            }
            else if (args != null && args.Any(x => x.ToLowerInvariant() == "--tear-down"))
            {
                string target = GetTargetConnectionString();
                string master = GetMasterConnectionString(target);

                var store = new Initializer(master, target).Initialize(ConfigureSchema);

                store.Advanced.Clean.CompletelyRemoveAll();
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

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            string target = Program.GetTargetConnectionString();

            var store = DocumentStore.For(target);

            container.Register<IDocumentStore>(store);
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            var store = container.Resolve<IDocumentStore>();
            var repository = new AggregateRepository(store);

            container.Register<IAggregateRepository>(repository);
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            pipelines.BeforeRequest += (NancyContext ctx) =>
            {
                var log = LogManager.GetLogger("Nancy.BeforeRequest");
                log.InfoFormat("[{0}] {1}", ctx.Request.Method, ctx.Request.Url);
                return null;
            };
            pipelines.AfterRequest += (NancyContext ctx) =>
            {
                var log = LogManager.GetLogger("Nancy.AfterRequest");
                log.InfoFormat("{0} [{1}] {2}", (int)ctx.Response.StatusCode, ctx.Request.Method, ctx.Request.Url);
            };
            pipelines.OnError += (ctx, ex) =>
            {
                var log = LogManager.GetLogger("Nancy.OnError");
                log.ErrorFormat("[{0}] {1}\r\n{2}", ctx.Request.Method, ctx.Request.Url, ex);
                return null;
            };
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

    public class SampleService
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
}

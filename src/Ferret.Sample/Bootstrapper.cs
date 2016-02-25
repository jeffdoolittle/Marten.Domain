using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Marten;
using log4net;

namespace Ferret.Sample
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly IDocumentStore _store;

        public Bootstrapper(IDocumentStore store)
        {
            _store = store;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register(_store);
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
}

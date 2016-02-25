using Marten;
using Npgsql;
using System;
using System.IO;
using System.Linq;

namespace Ferret.Specs
{
    public abstract class DocumentStoreFixture
    {
        protected IDocumentStore _theStore;

        public DocumentStoreFixture()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connection.txt");
            if (!File.Exists(path))
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                path = Path.Combine(dir.Parent.Parent.FullName, "connection.txt");
            }

            var target = File.ReadLines(path).First();
            var builder = new NpgsqlConnectionStringBuilder(target);
            builder.Database = "postgres";
            var master = builder.ConnectionString;

            _theStore = new Initializer(master, target).Initialize(Configure);
            _theStore.Advanced.Clean.CompletelyRemoveAll();
        }

        private void Configure(StoreOptions registry)
        {
            registry.AutoCreateSchemaObjects = true;
            ConfigureStore(registry);
        }

        protected virtual void ConfigureStore(StoreOptions registry)
        {
        }
    }
}

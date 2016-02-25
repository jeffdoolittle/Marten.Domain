using System;
using Npgsql;
using Marten;
using Marten.Services;

namespace Ferret
{
    public class Initializer
    {
        private readonly string _masterConnectionString;
        private readonly string _targetConnectionString;

        public Initializer(string masterConnectionString, string targetConnectionString)
        {
            _masterConnectionString = masterConnectionString;
            _targetConnectionString = targetConnectionString;
        }

        public IDocumentStore Initialize(Action<StoreOptions> register = null)
        {
            var builder = new NpgsqlConnectionStringBuilder(_targetConnectionString);
            var targetDatabaseName = builder.Database;

            var databaseNotFound = false;

            using (var connection = new NpgsqlConnection(_masterConnectionString))
            {
                connection.Open();
                var existsCommand = connection.CreateCommand();
                existsCommand.CommandText = "select (count(*) > 0)::boolean as exists from pg_database where datname=:0";
                existsCommand.Parameters.Add(new NpgsqlParameter("0", targetDatabaseName));
                var exists = (bool)existsCommand.ExecuteScalar();
                if (!exists)
                {
                    databaseNotFound = true;
                    var createCommand = connection.CreateCommand();
                    createCommand.CommandText = string.Format("CREATE DATABASE \"{0}\"", targetDatabaseName);
                    createCommand.ExecuteNonQuery();
                }
            }

            var store = DocumentStore.For(cfg =>
            {
                cfg.Connection(_targetConnectionString);
                cfg.Schema.For<Commit>()
                    .Searchable(x => x.StreamId)
                    .Searchable(x => x.StreamVersion);
                if (register != null)
                {
                    register(cfg);
                }
            });

            if (databaseNotFound)
            {
                var ddl = store.Schema.ToDDL();
                using (var connection = new NpgsqlConnection(_targetConnectionString))
                {
                    connection.Open();
                    var ddlCommand = connection.CreateCommand();
                    ddlCommand.CommandText = ddl;
                    ddlCommand.ExecuteNonQuery();
                }
            }

            return store;
        }

        public void TearDown()
        {
            var builder = new NpgsqlConnectionStringBuilder(_targetConnectionString);
            var targetDatabaseName = builder.Database;

            var store = DocumentStore.For(cfg =>
            {
                cfg.Connection(_targetConnectionString);
            });
            store.Advanced.Clean.CompletelyRemoveAll();
        }

        public void Drop()
        {
            var builder = new NpgsqlConnectionStringBuilder(_targetConnectionString);
            var targetDatabaseName = builder.Database;

            using (var connection = new NpgsqlConnection(_masterConnectionString))
            {
                connection.Open();
                var existsCommand = connection.CreateCommand();
                existsCommand.CommandText = "select (count(*) > 0)::boolean as exists from pg_database where datname=:0";
                existsCommand.Parameters.Add(new NpgsqlParameter("0", targetDatabaseName));
                var exists = (bool)existsCommand.ExecuteScalar();
                if (exists)
                {
                    var createCommand = connection.CreateCommand();
                    createCommand.CommandText = string.Format("DROP DATABASE \"{0}\"", targetDatabaseName);
                    createCommand.ExecuteNonQuery();
                }
            }
        }
    }
}

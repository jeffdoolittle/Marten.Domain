using System;
using System.Linq;
using Xunit;

namespace Ferret.Specs
{
    public class MultiCommitFixture : DocumentStoreFixture
    {
        [Fact]
        public void can_create_aggregate_with_multiple_commits()
        {
            Guid fooId = Guid.NewGuid();

            for (int i = 0; i < 25; i++)
            {
                using (var repository = new AggregateRepository(_theStore))
                {
                    var manager = new Foo.Manager(repository);
                    manager.When(new FooCommand { Id = fooId, Message = "Hello Foo! " + (i + 1) });
                }
            }

            var streamId = Guid.Empty;
            using (var session = _theStore.QuerySession())
            {
                var foo = session.Query<Foo.State>().FirstOrDefault();
                var commit = session.Query<Commit>().FirstOrDefault();
                Assert.NotNull(foo);
                Assert.NotNull(commit);
                Assert.Equal(foo.Id, commit.StreamId);
                streamId = commit.StreamId;
            }

            using (var repository = new AggregateRepository(_theStore))
            {
                var response = repository.Advanced.FetchCommits(new CommitRequest
                {
                    StreamId = streamId,
                    PageSize = 10,
                    Page = 1
                });

                Assert.Equal(25, response.TotalCommits);
                Assert.Equal(10, response.Commits.Count);
                Assert.Equal(response.Commits.First().StreamId, streamId);
            }

            using (var repository = new AggregateRepository(_theStore))
            {
                var response = repository.Advanced.FetchCommits(new CommitRequest
                {
                    MinimumCommitId = 11,
                    PageSize = 10,
                    Page = 1
                });

                Assert.Equal(15, response.TotalCommits);
                Assert.Equal(10, response.Commits.Count);
                Assert.Equal(response.Commits.First().StreamId, streamId);
            }

            using (var repository = new AggregateRepository(_theStore))
            {
                var response = repository.Advanced.FetchCommits(new CommitRequest
                {
                    MinimumCommitId = 21,
                    PageSize = 10,
                    Page = 1
                });

                Assert.Equal(5, response.TotalCommits);
                Assert.Equal(5, response.Commits.Count);
                Assert.Equal(response.Commits.First().StreamId, streamId);
            }
        }
    }

    public class Foo
    {
        public class Manager
        {
            private readonly IAggregateRepository _repository;

            public Manager(IAggregateRepository repository)
            {
                _repository = repository;
            }

            public void When(FooCommand command)
            {
                var aggregate = _repository.Load<Aggregate, State>(command.Id);
                aggregate.When(command);
                _repository.Save(aggregate);
            }
        }

        public class Aggregate : Aggregate<State>
        {
            public Aggregate(State state)
                : base(state)
            {
            }

            public void When(FooCommand command)
            {
                Apply(new FooEvent { Id = command.Id, Message = command.Message });
            }
        }

        public class State : StateBase
        {
            public string LastMessage { get; set; }

            public void Apply(FooEvent e)
            {
                Id = e.Id;
                LastMessage = e.Message;
                Version += 1;
            }
        }
    }

    public class FooCommand
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
    }

    public class FooEvent
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
    }
}

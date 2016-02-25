using System;
using System.Linq;
using Marten;
using Xunit;

namespace Ferret.Specs
{
    public class OnboardingFixture : DocumentStoreFixture
    {
        protected override void ConfigureStore(StoreOptions registry)
        {
            registry.Schema.For<Onboarding.State>().Searchable(x => x.Email);
        }

        [Fact]
        public void can_create_aggregate_with_commits()
        {
            using (var repository = new AggregateRepository(_theStore))
            {
                var manager = new Onboarding.Manager(repository);
                manager.When(new RequestNewUserRegistration
                {
                    OnboardingId = Guid.NewGuid(),
                    FirstName = "Jeff",
                    LastName = "Doolittle",
                    Email = "jeff@nowherefast.org"
                });
            }

            Guid streamId = Guid.Empty;
            using (var session = _theStore.QuerySession())
            {
                var onboarding = session.Query<Onboarding.State>().FirstOrDefault();
                var commit = session.Query<Commit>().FirstOrDefault();
                Assert.NotNull(onboarding);
                Assert.NotNull(commit);
                Assert.Equal(onboarding.Id, commit.StreamId);
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

                Assert.Equal(1, response.TotalCommits);
                Assert.Equal(1, response.Commits.Count);
                Assert.Equal(response.Commits.First().StreamId, streamId);
            }
        }

        [Fact]
        public void can_delete_aggregate_and_associated_commits()
        {
            Guid id = Guid.NewGuid();
            using (var repository = new AggregateRepository(_theStore))
            {
                var manager = new Onboarding.Manager(repository);
                manager.When(new RequestNewUserRegistration { OnboardingId = id });
            }

            using (var repository = new AggregateRepository(_theStore))
            {
                var manager = new Onboarding.Manager(repository);
                manager.When(new DeleteCommand { OnboardingId = id });
            }

            using (var session = _theStore.QuerySession())
            {
                var onboarding = session.Query<Onboarding.State>().FirstOrDefault();
                var commit = session.Query<Commit>().FirstOrDefault();
                Assert.Null(onboarding);
                Assert.Null(commit);
            }
        }
    }

    public class Onboarding
    {
        public class Manager
        {
            private readonly IAggregateRepository _repository;

            public Manager(IAggregateRepository repository)
            {
                _repository = repository;
            }

            public void When(RequestNewUserRegistration command)
            {
                var aggregate = _repository.Load<Aggregate, State>(command.OnboardingId);
                aggregate.When(command);
                _repository.Save(aggregate);
            }

            public void When(DeleteCommand command)
            {
                var aggregate = _repository.Load<Aggregate, State>(command.OnboardingId);
                _repository.Remove(aggregate);
            }
        }

        public class State : StateBase
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }

            public void Apply(NewUserRegistrationReqested e)
            {
                Id = e.OnboardingId;
                FirstName = e.FirstName;
                LastName = e.LastName;
                Email = e.Email;

                Version += 1;
            }
        }

        public class Aggregate : Aggregate<State>
        {
            public Aggregate(State state)
            : base(state)
            {
            }

            public void When(RequestNewUserRegistration command)
            {
                Apply(new NewUserRegistrationReqested
                {
                    OnboardingId = command.OnboardingId,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Email = command.Email
                });
            }
        }
    }

    public class RequestNewUserRegistration
    {
        public Guid OnboardingId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class DeleteCommand
    {
        public Guid OnboardingId { get; set; }
    }

    public class NewUserRegistrationReqested
    {
        public Guid OnboardingId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}

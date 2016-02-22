using Marten;
using System;
using System.Linq;

namespace Ferret
{
    public interface IAggregateRepository : IDisposable
    {
        TAggregate Load<TAggregate, TState>(Guid? id = null)
                    where TAggregate : IAggregate<TState>
                    where TState : class, IState, new();
        void Save<TState>(IAggregate<TState> aggregate) where TState : class, IState;
        void Remove<TState>(IAggregate<TState> aggregate) where TState : class, IState;
        IAdvancedAggregateRepository Advanced { get; }
    }

    public interface IAdvancedAggregateRepository : IDisposable
    {
        CommitResponse FetchCommits(CommitRequest request);
    }

    public class AggregateRepository : IAggregateRepository, IAdvancedAggregateRepository
    {
        private readonly IDocumentSession _session;

        public AggregateRepository(IDocumentStore store)
        {
            _session = store.DirtyTrackedSession();
        }

        public TAggregate Load<TAggregate, TState>(Guid? id = null)
            where TAggregate : IAggregate<TState>
            where TState : class, IState, new()
        {
            var factory = new AggregateFactory();
            var state = _session.Load<TState>(id ?? Guid.Empty);
            if (state == null)
            {
                state = new TState();
                _session.Store(state);
            }
            var aggregate = factory.Create(state);
            return (TAggregate)aggregate;
        }

        public void Save<TState>(IAggregate<TState> aggregate)
            where TState : class, IState
        {
            if (aggregate.Id == Guid.Empty)
            {
                throw new FerretException("Aggregate Id cannot be empty");
            }

            if (_session.Load<TState>(aggregate.Id) == null)
            {
                _session.Store(aggregate.State);
            }

            if (aggregate.AppliedEvents.Any())
            {
                var commit = new Commit
                {
                    StreamId = aggregate.Id,
                    StreamVersion = aggregate.Version,
                    CommitDateTimeUtc = DateTimeOffset.UtcNow,
                    Events = aggregate.AppliedEvents.ToList()
                };
                _session.Store(commit);
            }
        }

        public void Remove<TState>(IAggregate<TState> aggregate)
            where TState : class, IState
        {
            var commits = _session.Query<Commit>().Where(x => x.StreamId == aggregate.Id);

            foreach (var commit in commits)
            {
                _session.Delete(commit);
            }

            _session.Delete(aggregate.State);
        }

        public void Dispose()
        {
            if (_session != null)
            {
                _session.SaveChanges();
                _session.Dispose();
            }
        }

        CommitResponse IAdvancedAggregateRepository.FetchCommits(CommitRequest request)
        {
            var specification = new CommitSpecification(request);
            var total = _session.Query<Commit>().Count(specification);

            var skip = (request.Page - 1) * request.PageSize;
            var take = request.PageSize;

            var commits = _session.Query<Commit>()
                .Where(specification)
                .OrderBy(x => x.Id)
                .Skip(skip)
                .Take(take);

            var response = new CommitResponse(commits, total);

            return response;
        }

        public IAdvancedAggregateRepository Advanced { get { return this; } }
    }
}

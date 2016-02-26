using System;
using System.Collections.Generic;

namespace Ferret
{
    public interface IAggregate
    {
        Guid Id { get; }
        int Version { get; }
        IReadOnlyCollection<DomainEvent> AppliedEvents { get; }
    }

    public interface IAggregate<TState> : IAggregate
    {
        TState State { get; }
    }

    public abstract class Aggregate<TState> : IAggregate, IAggregate<TState>
        where TState : class, IState, new()
    {
        private List<DomainEvent> _eventsToApply;

        protected Aggregate(TState state)
        {
            State = state;
            _eventsToApply = new List<DomainEvent>();
        }

        public Guid Id { get { return State.Id; } }

        public int Version { get { return State.Version; } }

        protected TState State { get; private set; }

        public IReadOnlyCollection<DomainEvent> AppliedEvents { get { return _eventsToApply; } }

        TState IAggregate<TState>.State{ get { return State; } }

        protected void Apply(object eventToApply)
        {
            var domainEvent = new DomainEvent
            {
                Id = Guid.NewGuid(),
                Body = eventToApply,
                Type = eventToApply.GetType().FullName
            };

            _eventsToApply.Add(domainEvent);

            RedirectToApply.InvokeApply(State, domainEvent.Body);
        }
    }    
}

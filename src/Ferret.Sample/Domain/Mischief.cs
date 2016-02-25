using System;

namespace Ferret.Sample.Domain
{
    public class Mischief
    {
        public class Manager
        {
            private readonly IAggregateRepository _repository;

            public Manager(IAggregateRepository repository)
            {
                _repository = repository;
            }

            public void When(OpenMaraudersMap command)
            {
                var aggregate = _repository.Load<Aggregate, State>(command.MischiefId);
                aggregate.When(command);
                _repository.Save(aggregate);
            }

            public void When(GotoRoomOfRequirement command)
            {
                var aggregate = _repository.Load<Aggregate, State>(command.MischiefId);
                aggregate.When(command);
                _repository.Save(aggregate);
            }
        }

        public class State : StateBase
        {
            public int TimesVisitedRoomOfRequirement { get; set; }

            public void Apply(OpenedMaraudersMap e)
            {
                Id = e.MischiefId;
                Version += 1;
            }

            public void Apply(WentToRoomOfRequirement e)
            {
                TimesVisitedRoomOfRequirement += 1;
                Version += 1;
            }
        }

        public class Aggregate : Aggregate<State>
        {
            public Aggregate(State state)
                : base(state)
            {
            }

            public void When(OpenMaraudersMap command)
            {
                Apply(new OpenedMaraudersMap { MischiefId = command.MischiefId });
            }

            public void When(GotoRoomOfRequirement command)
            {
                Apply(new WentToRoomOfRequirement { MischiefId = command.MischiefId });
            }
        }
    }

    public class OpenMaraudersMap
    {
        public Guid MischiefId { get; set; }
    }

    public class OpenedMaraudersMap
    {
        public Guid MischiefId { get; set; }
    }

    public class GotoRoomOfRequirement
    {
        public Guid MischiefId { get; set; }
    }

    public class WentToRoomOfRequirement
    {
        public Guid MischiefId { get; set; }
    }
}

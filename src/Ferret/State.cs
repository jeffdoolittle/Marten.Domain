using System;

namespace Ferret
{
    public interface IState
    {
        Guid Id { get; }
        int Version { get; }
    }

    public abstract class StateBase : IState
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
    }
}

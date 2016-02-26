using System;

namespace Ferret
{
    public class DomainEvent
    {
        public Guid Id { get; set; }
        public object Body { get; set; }
        public string Type { get; set; }
    }
}
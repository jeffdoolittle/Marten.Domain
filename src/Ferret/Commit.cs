using System;
using System.Collections.Generic;

namespace Ferret
{
    public class Commit
    {
        public Commit()
        {
            Events = new List<DomainEvent>();
        }

        public long Id { get; set; }

        public Guid StreamId { get; set; }

        public int StreamVersion { get; set; }

        public string AggregateType { get; set; }

        public DateTimeOffset CommitDateTimeUtc { get; set; }

        public List<DomainEvent> Events { get; set; }
    }
}
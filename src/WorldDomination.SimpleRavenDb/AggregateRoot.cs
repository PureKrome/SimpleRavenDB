using System;

namespace WorldDomination.SimpleRavenDb
{
    public abstract class AggregateRoot
    {
        public string Id { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}

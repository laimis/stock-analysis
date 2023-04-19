#nullable enable
using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Portfolio
{
    public class Routine : Aggregate
    {
        public RoutineState State { get; } = new RoutineState();

        public override IAggregateState AggregateState => State;

        public Routine(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public Routine(string description, string name, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Missing routine name");
            }

            Apply(new RoutineCreated(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, description, name, userId));
        }

        public void Update(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Missing routine name");
            }

            name = name.Trim();
            description = description?.Trim();

            if (name == State.Name && description == State.Description)
            {
                return;
            }

            Apply(new RoutineUpdated(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, description, name));
        }

        public void AddStep(string label, string? url)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new InvalidOperationException("Missing step label");
            }

            label = label.Trim();
            url = url?.Trim();

            Apply(new RoutineStepAdded(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, label, url));
        }

        public void UpdateStep(int index, string label, string? url)
        {
            if (index < 0 || index >= State.Steps.Count)
            {
                throw new InvalidOperationException("Invalid step index");
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new InvalidOperationException("Missing step label");
            }

            label = label.Trim();
            url = url?.Trim();

            var step = State.Steps[index];
            if (label == step.label && url == step.url)
            {
                return;
            }

            Apply(new RoutineStepUpdated(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, index, label, url));
        }

        public void RemoveStep(int index)
        {
            if (index < 0 || index >= State.Steps.Count)
            {
                throw new InvalidOperationException("Invalid step index");
            }

            Apply(new RoutineStepRemoved(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, index));
        }
    }

    public class RoutineState : IAggregateState
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        #nullable disable
        public string Name { get; private set; }
        #nullable enable
        public string? Description { get; private set; }
        public List<RoutineStep> Steps { get; } = new List<RoutineStep>();

        public void Apply(AggregateEvent e) => ApplyInternal(e);
        protected void ApplyInternal(dynamic obj) => ApplyInternal(obj);

        private void ApplyInternal(RoutineCreated e)
        {
            Id = e.AggregateId;
            UserId = e.UserId;
            Name = e.Name;
            Description = e.Description;
        }

        private void ApplyInternal(RoutineUpdated e)
        {
            Name = e.Name;
            Description = e.Description;
        }

        private void ApplyInternal(RoutineStepAdded e)
        {
            Steps.Add(new RoutineStep(label: e.Label, url: e.Url));
        }

        private void ApplyInternal(RoutineStepRemoved e)
        {
            Steps.RemoveAt(e.Index);
        }

        private void ApplyInternal(RoutineStepUpdated e)
        {
            var step = Steps[e.Index];
            Steps[e.Index] = new RoutineStep(label: e.Label, url: e.Url);
        }

        public record struct RoutineStep(string label, string? url);
    }
}
#nullable restore
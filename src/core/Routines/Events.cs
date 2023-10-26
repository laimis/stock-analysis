using System;
using core.Shared;

namespace core.Routines;

internal class RoutineCreated : AggregateEvent
{
    public RoutineCreated(Guid id, Guid aggregateId, DateTimeOffset when, string description, string name, Guid userId)
        : base(id, aggregateId, when)
    {
        Description = description;
        Name = name;
        UserId = userId;
    }

    public string Description { get; }
    public string Name { get; }
    public Guid UserId { get; }
}

internal class RoutineUpdated : AggregateEvent
{
    public RoutineUpdated(Guid id, Guid aggregateId, DateTimeOffset when, string description, string name)
        : base(id, aggregateId, when)
    {
        Description = description;
        Name = name;
    }

    public string Description { get; }
    public string Name { get; }
}

internal class RoutineStepAdded : AggregateEvent
{
    public RoutineStepAdded(Guid id, Guid aggregateId, DateTimeOffset when, string label, string url)
        : base(id, aggregateId, when)
    {
        Label = label;
        Url = url;
    }

    public string Label { get; }
    public string Url { get; }
}

internal class RoutineStepRemoved : AggregateEvent
{
    public RoutineStepRemoved(Guid id, Guid aggregateId, DateTimeOffset when, int index)
        : base(id, aggregateId, when)
    {
        Index = index;
    }

    public int Index { get; }
}

internal class RoutineStepMoved : AggregateEvent
{
    public RoutineStepMoved(Guid id, Guid aggregateId, DateTimeOffset when, int direction, int stepIndex)
        : base(id, aggregateId, when)
    {
        Direction = direction;
        StepIndex = stepIndex;
    }

    public int Direction { get; }
    public int StepIndex { get; }
}

internal class RoutineStepUpdated : AggregateEvent
{
    public RoutineStepUpdated(Guid id, Guid aggregateId, DateTimeOffset when, int index, string label, string url)
        : base(id, aggregateId, when)
    {
        Index = index;
        Label = label;
        Url = url;
    }

    public int Index { get; }
    public string Label { get; }
    public string Url { get; }
}
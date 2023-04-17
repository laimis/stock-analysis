using System;
using System.Linq;
using core.Portfolio;
using Xunit;

namespace coretests.Stocks
{
    public class RoutineTests
    {
        private static Guid _userId = Guid.NewGuid();

        private static Routine Create(string name, string description) =>
            new Routine(description: description, name: name, userId: _userId);

        [Fact]
        public void CreateWorks()
        {
            var name = "daily";
            var description = "daily steps to take";
            
            var routine = Create(name, description);

            var state = routine.State;

            Assert.NotEqual(Guid.Empty, state.Id);
            Assert.Equal(_userId, state.UserId);
            Assert.Equal(description, state.Description);
            Assert.Equal(name, state.Name);
        }

        [Fact]
        public void UpdateWorks()
        {
            var routine = Create("name", "description");

            routine.Update(name: "new name", description: "new description");

            Assert.Equal("new name", routine.State.Name);
            Assert.Equal("new description", routine.State.Description);
        }

        [Fact]
        public void CreateWithNoNameFails()
        {
            Assert.Throws<InvalidOperationException>(() => Create("", "description"));
        }

        [Fact]
        public void CreateWithNoDescriptionWorks()
        {
            var routine = Create("name", null);

            Assert.NotNull(routine);
        }

        [Fact]
        public void UpdateWithNoNameFails()
        {
            var routine = Create("name", "description");

            Assert.Throws<InvalidOperationException>(() => routine.Update("", "description"));
        }

        [Fact]
        public void UpdateWithNoDescriptionWorks()
        {
            var routine = Create("name", "description");

            routine.Update("name", null);

            Assert.NotNull(routine);
        }

        [Fact]
        public void AddingStepWorks()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            var step = routine.State.Steps.First();

            Assert.Equal("label", step.label);
            Assert.Equal("url", step.url);
        }

        [Fact]
        public void AddingStepWithNoLabelFails()
        {
            var routine = Create("name", "description");

            Assert.Throws<InvalidOperationException>(() => routine.AddStep("", "url"));
        }

        [Fact]
        public void AddingStepWithNoUrlSucceeds()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", null);

            Assert.Single(routine.State.Steps);
        }

        [Fact]
        public void RemovingStepWorks()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            routine.RemoveStep(0);

            Assert.Empty(routine.State.Steps);
        }

        [Fact]
        public void RemovingStepWithInvalidIndexFails()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            Assert.Throws<InvalidOperationException>(() => routine.RemoveStep(1));
        }

        [Fact]
        public void RemovingStepWithNegativeIndexFails()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            Assert.Throws<InvalidOperationException>(() => routine.RemoveStep(-1));
        }
    }
}

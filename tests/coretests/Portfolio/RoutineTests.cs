using System;
using System.Linq;
using core.Portfolio;
using Xunit;

namespace coretests.Portfolio
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

        [Fact]
        public void UpdatingStepWorks()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            routine.UpdateStep(0, "new label", "new url");

            var step = routine.State.Steps.First();

            Assert.Equal("new label", step.label);
            Assert.Equal("new url", step.url);
        }

        [Fact]
        public void UpdatingStepWithNoLabelFails()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            Assert.Throws<InvalidOperationException>(() => routine.UpdateStep(0, "", "new url"));
        }

        [Fact]
        public void UpdatingStepWithNoUrlSucceeds()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            routine.UpdateStep(0, "new label", null);

            var step = routine.State.Steps.First();

            Assert.Equal("new label", step.label);
            Assert.Null(step.url);
        }

        [Fact]
        public void UpdatingStepWithInvalidIndexFails()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");

            Assert.Single(routine.State.Steps);

            Assert.Throws<InvalidOperationException>(() => routine.UpdateStep(1, "new label", "new url"));
        }

        [Fact]
        public void MoveStepWorks()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");
            routine.AddStep("label2", "url2");

            Assert.Equal(2, routine.State.Steps.Count());

            routine.MoveStep(0, 1);

            var steps = routine.State.Steps.ToArray();

            Assert.Equal("label2", steps[0].label);
            Assert.Equal("url2", steps[0].url);
            Assert.Equal("label", steps[1].label);
            Assert.Equal("url", steps[1].url);
        }

        [Fact]
        public void MoveStepWithDirectionGreaterThanOneFails()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");
            routine.AddStep("label2", "url2");

            Assert.Equal(2, routine.State.Steps.Count());

            Assert.Throws<InvalidOperationException>(() => routine.MoveStep(0, 2));
        }

        [Fact]
        public void MoveStepWithDirectionLessThanNegativeOneFails()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");
            routine.AddStep("label2", "url2");

            Assert.Equal(2, routine.State.Steps.Count());

            Assert.Throws<InvalidOperationException>(() => routine.MoveStep(0, -2));
        }

        [Fact]
        public void MoveStepWithInvalidIndexFails()
        {
            var routine = Create("name", "description");

            routine.AddStep("label", "url");
            routine.AddStep("label2", "url2");

            Assert.Equal(2, routine.State.Steps.Count());

            Assert.Throws<InvalidOperationException>(() => routine.MoveStep(2, 1));
        }
    }
}

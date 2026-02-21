using NUnit.Framework;
using Systems.CommandSystem;
using System;

namespace Tests.CommandSystem
{
    public class CommandBaseTests
    {
        private class TestCommand : CommandBase
        {
            public bool ExecuteCalled { get; private set; }
            public bool UndoCalled { get; private set; }
            public bool RedoCalled { get; private set; }

            public TestCommand(string description = "Test Command") : base(description) { }

            protected override void ExecuteInternal() => ExecuteCalled = true;
            protected override void UndoInternal() => UndoCalled = true;
            protected override void RedoInternal() => RedoCalled = true;
        }

        [Test]
        public void Constructor_SetsDescription()
        {
            var cmd = new TestCommand("Custom Description");
            Assert.AreEqual("Custom Description", cmd.Description);
        }

        [Test]
        public void Constructor_NullDescription_SetsDefault()
        {
            var cmd = new TestCommand(null);
            Assert.AreEqual("Unknown Command", cmd.Description);
        }

        [Test]
        public void Execute_CallsExecuteInternal()
        {
            var cmd = new TestCommand();
            cmd.Execute();
            Assert.IsTrue(cmd.ExecuteCalled);
        }

        [Test]
        public void Execute_WhenAlreadyExecuted_DoesNotCallExecuteInternalAgain()
        {
            var cmd = new TestCommand();
            cmd.Execute();
            cmd.ExecuteCalled = false; // Reset
            
            cmd.Execute();
            
            Assert.IsFalse(cmd.ExecuteCalled);
        }

        [Test]
        public void Undo_CallsUndoInternal()
        {
            var cmd = new TestCommand();
            cmd.Execute(); // Must execute before undo
            cmd.Undo();
            Assert.IsTrue(cmd.UndoCalled);
        }

        [Test]
        public void Undo_WhenNotExecuted_DoesNotCallUndoInternal()
        {
            var cmd = new TestCommand();
            cmd.Undo();
            Assert.IsFalse(cmd.UndoCalled);
        }

        [Test]
        public void Redo_CallsRedoInternal()
        {
            var cmd = new TestCommand();
            cmd.Execute();
            cmd.Undo();
            cmd.Redo();
            Assert.IsTrue(cmd.RedoCalled);
        }

        [Test]
        public void Redo_WhenAlreadyExecuted_DoesNotCallRedoInternal()
        {
            var cmd = new TestCommand();
            cmd.Execute();
            cmd.Redo();
            Assert.IsFalse(cmd.RedoCalled);
        }

        [Test]
        public void CanMergeWith_ReturnsFalseByDefault()
        {
            var cmd1 = new TestCommand();
            var cmd2 = new TestCommand();
            Assert.IsFalse(cmd1.CanMergeWith(cmd2));
        }

        [Test]
        public void MergeWith_ThrowsNotImplementedExceptionByDefault()
        {
            var cmd1 = new TestCommand();
            var cmd2 = new TestCommand();
            Assert.Throws<NotImplementedException>(() => cmd1.MergeWith(cmd2));
        }
    }
}

using NUnit.Framework;
using Systems.CommandSystem;
using System;
using System.Collections.Generic;

namespace Tests.CommandSystem
{
    public class CompositeCommandTests
    {
        private class MockCommand : ICommand
        {
            public string Description { get; }
            public bool ExecuteCalled { get; private set; }
            public bool UndoCalled { get; private set; }
            public bool RedoCalled { get; private set; }
            
            public Action OnExecute { get; set; }
            public Action OnUndo { get; set; }

            public MockCommand(string desc = "Mock")
            {
                Description = desc;
            }

            public void Execute()
            {
                ExecuteCalled = true;
                OnExecute?.Invoke();
            }

            public void Undo()
            {
                UndoCalled = true;
                OnUndo?.Invoke();
            }

            public void Redo() => RedoCalled = true;
            public bool CanMergeWith(ICommand other) => false;
            public void MergeWith(ICommand other) { }
        }

        [Test]
        public void AddCommand_AddsToInternalList()
        {
            var composite = new CompositeCommand("Group");
            var cmd = new MockCommand();
            composite.AddCommand(cmd);
            
            composite.Execute();
            Assert.IsTrue(cmd.ExecuteCalled);
        }

        [Test]
        public void AddCommand_WhenAlreadyExecuted_ThrowsInvalidOperationException()
        {
            var composite = new CompositeCommand("Group");
            composite.Execute();
            var cmd = new MockCommand();
            
            Assert.Throws<InvalidOperationException>(() => composite.AddCommand(cmd));
        }

        [Test]
        public void Execute_CallsExecuteOnAllCommands()
        {
            var cmd1 = new MockCommand();
            var cmd2 = new MockCommand();
            var composite = new CompositeCommand("Group", cmd1, cmd2);
            
            composite.Execute();
            
            Assert.IsTrue(cmd1.ExecuteCalled);
            Assert.IsTrue(cmd2.ExecuteCalled);
        }

        [Test]
        public void Undo_CallsUndoOnAllCommandsInReverseOrder()
        {
            var executionOrder = new List<string>();
            var cmd1 = new MockCommand("cmd1") { OnUndo = () => executionOrder.Add("cmd1") };
            var cmd2 = new MockCommand("cmd2") { OnUndo = () => executionOrder.Add("cmd2") };
            var composite = new CompositeCommand("Group", cmd1, cmd2);
            
            composite.Execute();
            composite.Undo();
            
            Assert.IsTrue(cmd1.UndoCalled);
            Assert.IsTrue(cmd2.UndoCalled);
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("cmd2", executionOrder[0]);
            Assert.AreEqual("cmd1", executionOrder[1]);
        }

        [Test]
        public void Redo_CallsRedoOnAllCommands()
        {
            var cmd1 = new MockCommand();
            var cmd2 = new MockCommand();
            var composite = new CompositeCommand("Group", cmd1, cmd2);
            
            composite.Execute();
            composite.Undo();
            composite.Redo();
            
            Assert.IsTrue(cmd1.RedoCalled);
            Assert.IsTrue(cmd2.RedoCalled);
        }
    }
}

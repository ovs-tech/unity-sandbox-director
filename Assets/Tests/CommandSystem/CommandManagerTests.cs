using NUnit.Framework;
using Systems.CommandSystem;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Tests.CommandSystem
{
    public class CommandManagerTests
    {
        private GameObject go;
        private CommandManager manager;

        private class DummyCommand : ICommand
        {
            public string Description { get; }
            public bool Executed { get; private set; }
            public bool Undone { get; private set; }
            public bool Redone { get; private set; }
            
            public bool CanMerge { get; set; } = false;
            
            public DummyCommand(string desc = "Dummy") { Description = desc; }
            public void Execute() => Executed = true;
            public void Undo() => Undone = true;
            public void Redo() => Redone = true;
            public bool CanMergeWith(ICommand other) => CanMerge;
            public void MergeWith(ICommand other) { } 
        }

        [SetUp]
        public void Setup()
        {
            go = new GameObject("CommandManager");
            manager = go.AddComponent<CommandManager>();
            manager.DebugLog = false; // keep test output clean
        }

        [TearDown]
        public void Teardown()
        {
            if (go != null)
            {
                GameObject.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecuteCommand_ExecutesAndAddsToUndoStack()
        {
            var cmd = new DummyCommand();
            manager.ExecuteCommand(cmd, false);
            
            Assert.IsTrue(cmd.Executed);
            Assert.IsTrue(manager.CanUndo);
            Assert.AreEqual(1, manager.UndoStackCount);
        }

        [Test]
        public void ExecuteCommand_ClearsRedoStack()
        {
            var cmd1 = new DummyCommand();
            var cmd2 = new DummyCommand();
            
            manager.ExecuteCommand(cmd1, false);
            manager.Undo();
            Assert.IsTrue(manager.CanRedo);
            
            manager.ExecuteCommand(cmd2, false);
            Assert.IsFalse(manager.CanRedo);
            Assert.AreEqual(0, manager.RedoStackCount);
        }

        [Test]
        public void Undo_PopsFromUndoAndPushesToRedo()
        {
            var cmd = new DummyCommand();
            manager.ExecuteCommand(cmd, false);
            
            manager.Undo();
            
            Assert.IsTrue(cmd.Undone);
            Assert.IsFalse(manager.CanUndo);
            Assert.IsTrue(manager.CanRedo);
            Assert.AreEqual(0, manager.UndoStackCount);
            Assert.AreEqual(1, manager.RedoStackCount);
        }

        [Test]
        public void Redo_PopsFromRedoAndPushesToUndo()
        {
            var cmd = new DummyCommand();
            manager.ExecuteCommand(cmd, false);
            manager.Undo();
            
            manager.Redo();
            
            Assert.IsTrue(cmd.Redone);
            Assert.IsTrue(manager.CanUndo);
            Assert.IsFalse(manager.CanRedo);
        }

        [Test]
        public void ExecuteCommand_Namespaced_UsesSeparateStacks()
        {
            var cmd1 = new DummyCommand("ns1_cmd");
            var cmd2 = new DummyCommand("ns2_cmd");
            
            manager.ExecuteCommand(cmd1, false, "NS1");
            manager.ExecuteCommand(cmd2, false, "NS2");
            
            Assert.IsTrue(manager.CanUndoForNamespace("NS1"));
            Assert.IsTrue(manager.CanUndoForNamespace("NS2"));
            
            manager.Undo("NS1");
            
            Assert.IsFalse(manager.CanUndoForNamespace("NS1"));
            Assert.IsTrue(manager.CanUndoForNamespace("NS2")); 
        }

        [Test]
        public void Undo_Namespaced_PopsFromUndoAndPushesToRedo()
        {
            var cmd = new DummyCommand("ns1_cmd");
            manager.ExecuteCommand(cmd, false, "NS1");
            
            manager.Undo("NS1");
            
            Assert.IsTrue(cmd.Undone);
            Assert.IsFalse(manager.CanUndoForNamespace("NS1"));
            Assert.IsTrue(manager.CanRedoForNamespace("NS1"));
            Assert.AreEqual(0, manager.UndoStackCountForNamespace("NS1"));
            Assert.AreEqual(1, manager.RedoStackCountForNamespace("NS1"));
        }

        [Test]
        public void Redo_Namespaced_PopsFromRedoAndPushesToUndo()
        {
            var cmd = new DummyCommand("ns1_cmd");
            manager.ExecuteCommand(cmd, false, "NS1");
            manager.Undo("NS1");
            
            manager.Redo("NS1");
            
            Assert.IsTrue(cmd.Redone);
            Assert.IsTrue(manager.CanUndoForNamespace("NS1"));
            Assert.IsFalse(manager.CanRedoForNamespace("NS1"));
            Assert.AreEqual(1, manager.UndoStackCountForNamespace("NS1"));
            Assert.AreEqual(0, manager.RedoStackCountForNamespace("NS1"));
        }

        [Test]
        public void Clear_Namespaced_RemovesHistoryOnlyForThatNamespace()
        {
            var cmd1 = new DummyCommand("ns1_cmd");
            var cmd2 = new DummyCommand("ns2_cmd");
            
            manager.ExecuteCommand(cmd1, false, "NS1");
            manager.ExecuteCommand(cmd2, false, "NS2");
            manager.Undo("NS1");
            manager.Undo("NS2");
            
            manager.Clear("NS1");
            
            Assert.IsFalse(manager.CanUndoForNamespace("NS1"));
            Assert.IsFalse(manager.CanRedoForNamespace("NS1"));
            
            Assert.IsFalse(manager.CanUndoForNamespace("NS2")); // It was undone
            Assert.IsTrue(manager.CanRedoForNamespace("NS2")); // So redo should be possible
        }

        [Test]
        public void ActiveNamespaces_ReturnsAllUsedNamespaces()
        {
            manager.ExecuteCommand(new DummyCommand(), false, "NS1");
            manager.ExecuteCommand(new DummyCommand(), false, "NS2");
            
            var namespaces = new List<string>(manager.ActiveNamespaces);
            
            Assert.IsTrue(namespaces.Contains("NS1"));
            Assert.IsTrue(namespaces.Contains("NS2"));
            Assert.IsTrue(namespaces.Contains(CommandManager.DEFAULT_NAMESPACE)); // default is always included
        }

        [Test]
        public void Events_Namespaced_FiredOnExecuteUndoRedo()
        {
            int executeCount = 0;
            int undoCount = 0;
            int redoCount = 0;
            int stackChangeCount = 0;
            string lastNamespace = null;

            manager.OnCommandExecutedByNamespace += (ns, _) => { executeCount++; lastNamespace = ns; };
            manager.OnUndoPerformedByNamespace += (ns, _) => { undoCount++; lastNamespace = ns; };
            manager.OnRedoPerformedByNamespace += (ns, _) => { redoCount++; lastNamespace = ns; };
            manager.OnStacksChangedByNamespace += (ns, _, _) => { stackChangeCount++; lastNamespace = ns; };

            var cmd = new DummyCommand();
            
            manager.ExecuteCommand(cmd, false, "TestNS"); 
            Assert.AreEqual("TestNS", lastNamespace);

            manager.Undo("TestNS");              
            Assert.AreEqual("TestNS", lastNamespace);

            manager.Redo("TestNS");              
            Assert.AreEqual("TestNS", lastNamespace);

            Assert.AreEqual(1, executeCount);
            Assert.AreEqual(1, undoCount);
            Assert.AreEqual(1, redoCount);
            Assert.AreEqual(3, stackChangeCount);
        }

        [Test]
        public void Properties_Namespaced_ReturnsCorrectly()
        {
            var cmd1 = new DummyCommand("FirstNS");
            var cmd2 = new DummyCommand("SecondNS");
            
            Assert.AreEqual("", manager.NextUndoDescriptionForNamespace("NS1"));
            Assert.AreEqual("", manager.NextRedoDescriptionForNamespace("NS1"));

            manager.ExecuteCommand(cmd1, false, "NS1");
            manager.ExecuteCommand(cmd2, false, "NS1");

            Assert.AreEqual("SecondNS", manager.NextUndoDescriptionForNamespace("NS1"));

            manager.Undo("NS1"); 
            
            Assert.AreEqual("FirstNS", manager.NextUndoDescriptionForNamespace("NS1"));
            Assert.AreEqual("SecondNS", manager.NextRedoDescriptionForNamespace("NS1"));
        }

        [Test]
        public void GetHistory_Namespaced_ReturnsDescriptionsInOrder()
        {
            manager.ExecuteCommand(new DummyCommand("Cmd1"), false, "NS1");
            manager.ExecuteCommand(new DummyCommand("Cmd2"), false, "NS1");
            manager.ExecuteCommand(new DummyCommand("OtherNSCmd"), false, "NS2");
            
            manager.Undo("NS1"); // Cmd2 goes to redo stack

            var undoHistory = manager.GetUndoHistory("NS1");
            Assert.AreEqual(1, undoHistory.Length);
            Assert.AreEqual("Cmd1", undoHistory[0]);
            
            var redoHistory = manager.GetRedoHistory("NS1");
            Assert.AreEqual(1, redoHistory.Length);
            Assert.AreEqual("Cmd2", redoHistory[0]);
        }

        [Test]
        public void Clear_RemovesAllHistory()
        {
            var cmd = new DummyCommand();
            manager.ExecuteCommand(cmd, false);
            manager.Undo();
            
            manager.Clear();
            
            Assert.IsFalse(manager.CanUndo);
            Assert.IsFalse(manager.CanRedo);
        }

        private class MergableDummyCommand : ICommand
        {
            public string Description { get; }
            public bool Executed { get; private set; }
            public bool Undone { get; private set; }
            public bool Redone { get; private set; }
            
            public bool CanMerge { get; set; } = true;
            public bool Merged { get; private set; }
            
            public MergableDummyCommand(string desc) { Description = desc; }
            public void Execute() => Executed = true;
            public void Undo() => Undone = true;
            public void Redo() => Redone = true;
            public bool CanMergeWith(ICommand other) => CanMerge;
            public void MergeWith(ICommand other) { Merged = true; } 
        }

        [Test]
        public void ExecuteCommand_Merge_MergesIfPossible()
        {
            var cmd1 = new MergableDummyCommand("cmd1");
            var cmd2 = new MergableDummyCommand("cmd2");
            
            manager.ExecuteCommand(cmd1, false);
            Assert.IsTrue(cmd1.Executed);
            
            manager.ExecuteCommand(cmd2, true);
            // cmd1 should merge cmd2
            Assert.IsTrue(cmd1.Merged);
            Assert.IsFalse(cmd2.Executed); 
            Assert.AreEqual(1, manager.UndoStackCount); 
        }

        [Test]
        public void Events_FiredOnExecuteUndoRedo()
        {
            int executeCount = 0;
            int undoCount = 0;
            int redoCount = 0;
            int stackChangeCount = 0;

            manager.OnCommandExecuted += (_) => executeCount++;
            manager.OnUndoPerformed += (_) => undoCount++;
            manager.OnRedoPerformed += (_) => redoCount++;
            manager.OnStacksChanged += (_, _) => stackChangeCount++;

            var cmd = new DummyCommand();
            
            manager.ExecuteCommand(cmd, false); // execute: stack changes
            manager.Undo();              // undo: stack changes
            manager.Redo();              // redo: stack changes

            Assert.AreEqual(1, executeCount);
            Assert.AreEqual(1, undoCount);
            Assert.AreEqual(1, redoCount);
            Assert.AreEqual(3, stackChangeCount);
        }

        [Test]
        public void Properties_NextDescriptions_ReturnCorrectly()
        {
            var cmd1 = new DummyCommand("First");
            var cmd2 = new DummyCommand("Second");
            
            Assert.AreEqual("", manager.NextUndoDescription);
            Assert.AreEqual("", manager.NextRedoDescription);

            manager.ExecuteCommand(cmd1, false);
            manager.ExecuteCommand(cmd2, false);

            Assert.AreEqual("Second", manager.NextUndoDescription);

            manager.Undo(); // Undoes Second
            
            Assert.AreEqual("First", manager.NextUndoDescription);
            Assert.AreEqual("Second", manager.NextRedoDescription);
        }
    }
}

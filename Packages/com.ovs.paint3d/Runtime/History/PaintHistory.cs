using System;
using System.Collections.Generic;
using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Global undo / redo for painting. Wrap a stroke in <see cref="BeginStroke"/> / <see cref="EndStroke"/>; the engine
    /// snapshots every texture the first time the stroke touches it. Painting outside a stroke (particles, collisions)
    /// is not recorded. Snapshots are GPU copies, so keep <see cref="MaxSteps"/> modest on mobile.
    /// </summary>
    public static class PaintHistory
    {
        private sealed class Snapshot
        {
            public PaintableTexture Slot;
            public RenderTexture Texture;
        }

        private sealed class Step
        {
            public readonly List<Snapshot> Snapshots = new List<Snapshot>();
        }

        private static readonly LinkedList<Step> _undo = new LinkedList<Step>();
        private static readonly Stack<Step> _redo = new Stack<Step>();
        private static readonly HashSet<Paintable> _touched = new HashSet<Paintable>();
        private static Step _current;
        private static int _maxSteps = 16;

        /// <summary>Raised whenever the undo / redo stacks change.</summary>
        public static event Action Changed;

        /// <summary>Maximum number of undo steps kept in memory.</summary>
        public static int MaxSteps
        {
            get => _maxSteps;
            set
            {
                _maxSteps = Mathf.Max(1, value);
                TrimUndo();
            }
        }

        public static bool IsRecording => _current != null;
        public static int UndoCount => _undo.Count;
        public static int RedoCount => _redo.Count;
        public static bool CanUndo => _undo.Count > 0;
        public static bool CanRedo => _redo.Count > 0;

        /// <summary>Starts recording a stroke. An already open stroke is closed first.</summary>
        public static void BeginStroke()
        {
            if (_current != null)
            {
                EndStroke();
            }
            _current = new Step();
        }

        /// <summary>Closes the current stroke and pushes it to the undo stack (empty strokes are discarded).</summary>
        public static void EndStroke()
        {
            Step step = _current;
            _current = null;
            if (step == null || step.Snapshots.Count == 0)
            {
                return;
            }

            ClearRedo();
            _undo.AddLast(step);
            TrimUndo();
            Changed?.Invoke();
        }

        /// <summary>Called by the engine before the first draw into a slot during the current stroke.</summary>
        internal static void RecordBeforePaint(PaintableTexture slot)
        {
            if (_current == null || slot == null || !slot.IsAllocated)
            {
                return;
            }

            foreach (Snapshot existing in _current.Snapshots)
            {
                if (existing.Slot == slot)
                {
                    return;
                }
            }

            _current.Snapshots.Add(Capture(slot));
        }

        public static bool Undo()
        {
            if (_current != null)
            {
                EndStroke();
            }

            if (_undo.Count == 0)
            {
                return false;
            }

            Step step = _undo.Last.Value;
            _undo.RemoveLast();
            _redo.Push(Swap(step));
            Changed?.Invoke();
            return true;
        }

        public static bool Redo()
        {
            if (_redo.Count == 0)
            {
                return false;
            }

            Step step = _redo.Pop();
            _undo.AddLast(Swap(step));
            TrimUndo();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Releases every snapshot.</summary>
        public static void Clear()
        {
            ReleaseStep(_current);
            _current = null;
            foreach (Step step in _undo)
            {
                ReleaseStep(step);
            }
            _undo.Clear();
            ClearRedo();
            Changed?.Invoke();
        }

        /// <summary>Drops snapshots that reference a slot being released.</summary>
        internal static void Forget(PaintableTexture slot)
        {
            bool changed = RemoveSlot(_current, slot);
            foreach (Step step in _undo)
            {
                changed |= RemoveSlot(step, slot);
            }
            foreach (Step step in _redo)
            {
                changed |= RemoveSlot(step, slot);
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Restores a step and returns the inverse step (state before restoring).</summary>
        private static Step Swap(Step step)
        {
            var inverse = new Step();
            _touched.Clear();
            foreach (Snapshot snapshot in step.Snapshots)
            {
                if (snapshot.Slot == null || !snapshot.Slot.IsAllocated)
                {
                    PaintTextureUtility.Destroy(snapshot.Texture);
                    continue;
                }

                inverse.Snapshots.Add(Capture(snapshot.Slot));
                Graphics.Blit(snapshot.Texture, snapshot.Slot.Current);
                snapshot.Slot.MarkModified();
                PaintTextureUtility.Destroy(snapshot.Texture);
                if (snapshot.Slot.Owner != null)
                {
                    _touched.Add(snapshot.Slot.Owner);
                }
            }

            foreach (Paintable paintable in _touched)
            {
                paintable.NotifyPainted();
            }
            _touched.Clear();
            return inverse;
        }

        private static Snapshot Capture(PaintableTexture slot)
        {
            RenderTexture copy = PaintTextureUtility.CreateLike(slot.Current, slot.Current.name + "_Undo");
            Graphics.Blit(slot.Current, copy);
            return new Snapshot { Slot = slot, Texture = copy };
        }

        private static void TrimUndo()
        {
            while (_undo.Count > _maxSteps)
            {
                ReleaseStep(_undo.First.Value);
                _undo.RemoveFirst();
            }
        }

        private static void ClearRedo()
        {
            while (_redo.Count > 0)
            {
                ReleaseStep(_redo.Pop());
            }
        }

        private static void ReleaseStep(Step step)
        {
            if (step == null)
            {
                return;
            }

            foreach (Snapshot snapshot in step.Snapshots)
            {
                PaintTextureUtility.Destroy(snapshot.Texture);
            }
            step.Snapshots.Clear();
        }

        private static bool RemoveSlot(Step step, PaintableTexture slot)
        {
            if (step == null)
            {
                return false;
            }

            int removed = step.Snapshots.RemoveAll(snapshot =>
            {
                if (snapshot.Slot != slot)
                {
                    return false;
                }
                PaintTextureUtility.Destroy(snapshot.Texture);
                return true;
            });
            return removed > 0;
        }
    }
}

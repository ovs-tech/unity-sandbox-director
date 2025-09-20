using UnityEngine;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Frame-based snapping system for precise timeline positioning
    /// Provides frame-accurate positioning and visual feedback
    /// </summary>
    public class TimelineSnapSystem
    {
        private readonly TimelineEditorUI timelineEditor;
        private float frameRate = 30f;
        private bool snapEnabled = true;
        private float snapThreshold = 0.1f; // Time threshold for snapping in seconds
        
        // Visual feedback
        private bool showSnapGuides = true;
        private Color snapGuideColor = new Color(1f, 1f, 0f, 0.7f);
        
        public TimelineSnapSystem(TimelineEditorUI editor)
        {
            timelineEditor = editor;
        }
        
        #region Properties
        
        public bool SnapEnabled
        {
            get => snapEnabled;
            set => snapEnabled = value;
        }
        
        public float FrameRate
        {
            get => frameRate;
            set => frameRate = Mathf.Max(1f, value);
        }
        
        public float SnapThreshold
        {
            get => snapThreshold;
            set => snapThreshold = Mathf.Max(0.01f, value);
        }
        
        public bool ShowSnapGuides
        {
            get => showSnapGuides;
            set => showSnapGuides = value;
        }
        
        #endregion
        
        #region Frame Snapping
        
        /// <summary>
        /// Snap a time value to the nearest frame
        /// </summary>
        /// <param name="time">Input time in seconds</param>
        /// <returns>Snapped time in seconds</returns>
        public float SnapToFrame(float time)
        {
            if (!snapEnabled || frameRate <= 0f)
                return time;
            
            int frame = Mathf.RoundToInt(time * frameRate);
            return frame / frameRate;
        }
        
        /// <summary>
        /// Snap a time value to the nearest frame if within threshold
        /// </summary>
        /// <param name="time">Input time in seconds</param>
        /// <returns>Snapped time if within threshold, original time otherwise</returns>
        public float SnapToFrameIfClose(float time)
        {
            if (!snapEnabled || frameRate <= 0f)
                return time;
            
            float snappedTime = SnapToFrame(time);
            float distance = Mathf.Abs(time - snappedTime);
            
            // Convert threshold to frame-relative distance
            float frameThreshold = snapThreshold / frameRate;
            
            return distance <= frameThreshold ? snappedTime : time;
        }
        
        /// <summary>
        /// Get the frame number for a given time
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <returns>Frame number</returns>
        public int TimeToFrame(float time)
        {
            if (frameRate <= 0f) return 0;
            return Mathf.RoundToInt(time * frameRate);
        }
        
        /// <summary>
        /// Get the time for a given frame number
        /// </summary>
        /// <param name="frame">Frame number</param>
        /// <returns>Time in seconds</returns>
        public float FrameToTime(int frame)
        {
            if (frameRate <= 0f) return 0f;
            return frame / frameRate;
        }
        
        /// <summary>
        /// Get the next frame time after the given time
        /// </summary>
        /// <param name="time">Current time</param>
        /// <returns>Next frame time</returns>
        public float NextFrameTime(float time)
        {
            int currentFrame = TimeToFrame(time);
            return FrameToTime(currentFrame + 1);
        }
        
        /// <summary>
        /// Get the previous frame time before the given time
        /// </summary>
        /// <param name="time">Current time</param>
        /// <returns>Previous frame time</returns>
        public float PreviousFrameTime(float time)
        {
            int currentFrame = TimeToFrame(time);
            return FrameToTime(Mathf.Max(0, currentFrame - 1));
        }
        
        #endregion
        
        #region Clip Snapping
        
        /// <summary>
        /// Snap clip position considering other clips and frame boundaries
        /// </summary>
        /// <param name="clipUI">Clip being moved</param>
        /// <param name="targetTime">Desired time position</param>
        /// <returns>Snapped time position</returns>
        public float SnapClipPosition(ClipUI clipUI, float targetTime)
        {
            if (!snapEnabled) return targetTime;
            
            var snapCandidates = GetSnapCandidates(clipUI, targetTime);
            
            // Find the closest snap candidate
            float bestSnap = targetTime;
            float bestDistance = float.MaxValue;
            
            foreach (var candidate in snapCandidates)
            {
                float distance = Mathf.Abs(targetTime - candidate);
                if (distance < bestDistance && distance <= snapThreshold)
                {
                    bestDistance = distance;
                    bestSnap = candidate;
                }
            }
            
            return bestSnap;
        }
        
        /// <summary>
        /// Snap clip resize operation
        /// </summary>
        /// <param name="clipUI">Clip being resized</param>
        /// <param name="newStart">New start time</param>
        /// <param name="newDuration">New duration</param>
        /// <param name="isResizingStart">True if resizing from start, false if from end</param>
        /// <returns>Snapped start time and duration</returns>
        public (float start, float duration) SnapClipResize(ClipUI clipUI, float newStart, float newDuration, bool isResizingStart)
        {
            if (!snapEnabled) return (newStart, newDuration);
            
            if (isResizingStart)
            {
                // Snapping start time
                float snappedStart = SnapClipPosition(clipUI, newStart);
                float adjustedDuration = newDuration + (newStart - snappedStart);
                return (snappedStart, Mathf.Max(0.1f, adjustedDuration));
            }
            else
            {
                // Snapping end time
                float endTime = newStart + newDuration;
                float snappedEnd = SnapClipPosition(clipUI, endTime);
                float adjustedDuration = snappedEnd - newStart;
                return (newStart, Mathf.Max(0.1f, adjustedDuration));
            }
        }
        
        private float[] GetSnapCandidates(ClipUI excludeClip, float targetTime)
        {
            var candidates = new System.Collections.Generic.List<float>();
            
            // Add frame boundaries
            candidates.AddRange(GetFrameSnapCandidates(targetTime));
            
            // Add clip boundaries from other clips
            candidates.AddRange(GetClipSnapCandidates(excludeClip));
            
            // Add playhead position
            if (timelineEditor?.Director != null)
            {
                candidates.Add(timelineEditor.Director.Time);
            }
            
            return candidates.ToArray();
        }
        
        private float[] GetFrameSnapCandidates(float targetTime)
        {
            var candidates = new System.Collections.Generic.List<float>();
            
            // Get nearby frame times
            int targetFrame = TimeToFrame(targetTime);
            
            for (int i = -2; i <= 2; i++)
            {
                int frame = targetFrame + i;
                if (frame >= 0)
                {
                    candidates.Add(FrameToTime(frame));
                }
            }
            
            return candidates.ToArray();
        }
        
        private float[] GetClipSnapCandidates(ClipUI excludeClip)
        {
            var candidates = new System.Collections.Generic.List<float>();
            
            if (timelineEditor == null) return candidates.ToArray();
            
            // Get boundaries from all clips except the one being moved
            foreach (var trackUI in timelineEditor.GetComponentsInChildren<TrackUI>())
            {
                foreach (var clipUI in trackUI.ClipUIs)
                {
                    if (clipUI == excludeClip) continue;
                    
                    // Add start and end positions
                    candidates.Add(clipUI.Clip.Start);
                    candidates.Add(clipUI.Clip.Start + clipUI.Clip.Duration);
                }
            }
            
            return candidates.ToArray();
        }
        
        #endregion
        
        #region Visual Feedback
        
        /// <summary>
        /// Get snap guide information for visual feedback
        /// </summary>
        /// <param name="draggedClip">Clip being dragged</param>
        /// <param name="currentTime">Current mouse time position</param>
        /// <returns>Snap guide data for rendering</returns>
        public SnapGuideData GetSnapGuide(ClipUI draggedClip, float currentTime)
        {
            if (!snapEnabled || !showSnapGuides)
                return new SnapGuideData { show = false };
            
            float snappedTime = SnapClipPosition(draggedClip, currentTime);
            
            if (Mathf.Abs(currentTime - snappedTime) < snapThreshold)
            {
                return new SnapGuideData
                {
                    show = true,
                    time = snappedTime,
                    color = snapGuideColor,
                    type = GetSnapType(snappedTime)
                };
            }
            
            return new SnapGuideData { show = false };
        }
        
        private SnapType GetSnapType(float time)
        {
            // Check if it's a frame boundary
            float frameTime = SnapToFrame(time);
            if (Mathf.Abs(time - frameTime) < 0.001f)
            {
                return SnapType.Frame;
            }
            
            // Check if it's playhead
            if (timelineEditor?.Director != null)
            {
                float playheadTime = timelineEditor.Director.Time;
                if (Mathf.Abs(time - playheadTime) < 0.001f)
                {
                    return SnapType.Playhead;
                }
            }
            
            // Default to clip
            return SnapType.Clip;
        }
        
        #endregion
        
        #region Update Methods
        
        /// <summary>
        /// Update snap settings from timeline project
        /// </summary>
        public void UpdateFromProject()
        {
            if (timelineEditor?.Director?.Project != null)
            {
                frameRate = timelineEditor.Director.Project.frameRate;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for snap guide visual feedback
    /// </summary>
    public struct SnapGuideData
    {
        public bool show;
        public float time;
        public Color color;
        public SnapType type;
    }
    
    /// <summary>
    /// Types of snap targets
    /// </summary>
    public enum SnapType
    {
        Frame,      // Frame boundary snap
        Clip,       // Clip boundary snap
        Playhead,   // Playhead position snap
        Marker      // Custom marker snap
    }
    
    /// <summary>
    /// Extension methods for easy snapping in UI components
    /// </summary>
    public static class SnapExtensions
    {
        /// <summary>
        /// Snap time using the timeline editor's snap system
        /// </summary>
        /// <param name="editor">Timeline editor</param>
        /// <param name="time">Time to snap</param>
        /// <returns>Snapped time</returns>
        public static float SnapTime(this TimelineEditorUI editor, float time)
        {
            // TODO: Access snap system from editor
            // return editor.SnapSystem?.SnapToFrame(time) ?? time;
            return time;
        }
        
        /// <summary>
        /// Check if snapping is enabled
        /// </summary>
        /// <param name="editor">Timeline editor</param>
        /// <returns>True if snapping is enabled</returns>
        public static bool IsSnapEnabled(this TimelineEditorUI editor)
        {
            // TODO: Access snap system from editor
            // return editor.SnapSystem?.SnapEnabled ?? false;
            return true; // Default for now
        }
    }
}
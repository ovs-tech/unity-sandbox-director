using System;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Command for moving a clip to a new time position
    /// Supports merging for smooth dragging operations
    /// </summary>
    public class MoveClipCommand : TimelineCommandBase
    {
        private readonly IMiniClip clip;
        private readonly float oldStartTime;
        private readonly float newStartTime;
        private readonly TrackUI trackUI;
        
        public MoveClipCommand(IMiniClip clipToMove, float oldStart, float newStart, TrackUI track) 
            : base($"Move {clipToMove.Id}")
        {
            clip = clipToMove;
            oldStartTime = oldStart;
            newStartTime = newStart;
            trackUI = track;
        }
        
        protected override void ExecuteInternal()
        {
            // TODO: Update clip data through proper API
            // For now, we'll need to update through the track system
            SetClipStartTime(newStartTime);
            
            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }
        
        protected override void UndoInternal()
        {
            SetClipStartTime(oldStartTime);
            
            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }
        
        public override bool CanMergeWith(ITimelineCommand other)
        {
            if (other is MoveClipCommand moveCommand)
            {
                return moveCommand.clip == clip;
            }
            return false;
        }
        
        public override void MergeWith(ITimelineCommand other)
        {
            if (other is MoveClipCommand moveCommand && moveCommand.clip == clip)
            {
                // Update our new position to the other command's position
                // The old position stays the same (original start of the drag)
                // newStartTime = moveCommand.newStartTime; // This would be done through reflection or proper API
            }
        }
        
        private void SetClipStartTime(float time)
        {
            // TODO: Implement proper clip data modification
            // This should go through the IMiniClip interface or track system
            Debug.Log($"Setting clip {clip.Id} start time to {time}");
        }
    }
    
    /// <summary>
    /// Command for resizing a clip (changing duration and/or start time)
    /// </summary>
    public class ResizeClipCommand : TimelineCommandBase
    {
        private readonly IMiniClip clip;
        private readonly float oldStartTime;
        private readonly float oldDuration;
        private readonly float newStartTime;
        private readonly float newDuration;
        private readonly TrackUI trackUI;
        
        public ResizeClipCommand(IMiniClip clipToResize, float oldStart, float oldDur, 
                               float newStart, float newDur, TrackUI track)
            : base($"Resize {clipToResize.Id}")
        {
            clip = clipToResize;
            oldStartTime = oldStart;
            oldDuration = oldDur;
            newStartTime = newStart;
            newDuration = newDur;
            trackUI = track;
        }
        
        protected override void ExecuteInternal()
        {
            SetClipTiming(newStartTime, newDuration);
            
            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }
        
        protected override void UndoInternal()
        {
            SetClipTiming(oldStartTime, oldDuration);
            
            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }
        
        public override bool CanMergeWith(ITimelineCommand other)
        {
            if (other is ResizeClipCommand resizeCommand)
            {
                return resizeCommand.clip == clip;
            }
            return false;
        }
        
        public override void MergeWith(ITimelineCommand other)
        {
            if (other is ResizeClipCommand resizeCommand && resizeCommand.clip == clip)
            {
                // Update our new timing to the other command's timing
                // The old timing stays the same (original before resize)
                // This would be done through proper API
            }
        }
        
        private void SetClipTiming(float start, float duration)
        {
            // TODO: Implement proper clip data modification
            Debug.Log($"Setting clip {clip.Id} timing: start={start}, duration={duration}");
        }
    }
    
    /// <summary>
    /// Command for creating a new clip on a track
    /// </summary>
    public class CreateClipCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly ClipData clipData;
        private readonly TrackUI trackUI;
        private IMiniClip createdClip;
        
        public CreateClipCommand(IMiniTrack targetTrack, ClipData data, TrackUI trackInterface)
            : base($"Create Clip")
        {
            track = targetTrack;
            clipData = data;
            trackUI = trackInterface;
        }
        
        protected override void ExecuteInternal()
        {
            // TODO: Create clip through proper track API
            // createdClip = track.CreateClip(clipData);
            
            // Update UI
            // trackUI?.BuildClipUIs();
            
            Debug.Log($"Creating clip at {clipData.start} with duration {clipData.duration}");
        }
        
        protected override void UndoInternal()
        {
            if (createdClip != null)
            {
                // TODO: Remove clip through proper track API
                // track.RemoveClip(createdClip);
                
                // Update UI
                // trackUI?.BuildClipUIs();
                
                Debug.Log($"Removing clip {createdClip.Id}");
            }
        }
    }
    
    /// <summary>
    /// Command for deleting a clip from a track
    /// </summary>
    public class DeleteClipCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly IMiniClip clip;
        private readonly ClipData clipBackup;
        private readonly TrackUI trackUI;
        
        public DeleteClipCommand(IMiniTrack sourceTrack, IMiniClip clipToDelete, TrackUI trackInterface)
            : base($"Delete {clipToDelete.Id}")
        {
            track = sourceTrack;
            clip = clipToDelete;
            trackUI = trackInterface;
            
            // TODO: Backup clip data for undo
            clipBackup = new ClipData
            {
                id = clip.Id,
                start = clip.Start,
                duration = clip.Duration,
                // payload = clip.GetPayload() // Need proper API
            };
        }
        
        protected override void ExecuteInternal()
        {
            // TODO: Remove clip through proper track API
            // track.RemoveClip(clip);
            
            // Update UI
            // trackUI?.BuildClipUIs();
            
            Debug.Log($"Deleting clip {clip.Id}");
        }
        
        protected override void UndoInternal()
        {
            // TODO: Recreate clip through proper track API
            // var restoredClip = track.CreateClip(clipBackup);
            
            // Update UI
            // trackUI?.BuildClipUIs();
            
            Debug.Log($"Restoring clip {clip.Id}");
        }
    }
    
    /// <summary>
    /// Command for changing timeline playback time (scrubbing)
    /// This command typically doesn't need undo/redo as it's a navigation operation
    /// </summary>
    public class SeekTimeCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly float oldTime;
        private readonly float newTime;
        
        public SeekTimeCommand(MiniTimelineDirector timelineDirector, float oldTimeValue, float newTimeValue)
            : base($"Seek to {newTimeValue:F2}s")
        {
            director = timelineDirector;
            oldTime = oldTimeValue;
            newTime = newTimeValue;
        }
        
        protected override void ExecuteInternal()
        {
            director?.Seek(newTime);
        }
        
        protected override void UndoInternal()
        {
            director?.Seek(oldTime);
        }
        
        public override bool CanMergeWith(ITimelineCommand other)
        {
            // Seek commands can be merged to avoid cluttering undo history
            return other is SeekTimeCommand;
        }
        
        public override void MergeWith(ITimelineCommand other)
        {
            if (other is SeekTimeCommand seekCommand)
            {
                // Keep our old time, update to the new command's new time
                // newTime = seekCommand.newTime; // Would be done through proper API
            }
        }
    }
    
    /// <summary>
    /// Command for changing timeline zoom level
    /// </summary>
    public class ZoomTimelineCommand : TimelineCommandBase
    {
        private readonly TimelineEditorUI editor;
        private readonly float oldZoom;
        private readonly float newZoom;
        
        public ZoomTimelineCommand(TimelineEditorUI timelineEditor, float oldZoomValue, float newZoomValue)
            : base($"Zoom to {newZoomValue:F1}x")
        {
            editor = timelineEditor;
            oldZoom = oldZoomValue;
            newZoom = newZoomValue;
        }
        
        protected override void ExecuteInternal()
        {
            editor?.SetZoom(newZoom);
        }
        
        protected override void UndoInternal()
        {
            editor?.SetZoom(oldZoom);
        }
        
        public override bool CanMergeWith(ITimelineCommand other)
        {
            // Zoom commands can be merged
            return other is ZoomTimelineCommand;
        }
        
        public override void MergeWith(ITimelineCommand other)
        {
            if (other is ZoomTimelineCommand zoomCommand)
            {
                // Keep our old zoom, update to the new command's new zoom
                // newZoom = zoomCommand.newZoom; // Would be done through proper API
            }
        }
    }
    
    /// <summary>
    /// Command for adding a new track to the timeline
    /// </summary>
    public class AddTrackCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly TrackData trackData;
        private readonly TimelineEditorUI editorUI;
        private IMiniTrack createdTrack;
        
        public AddTrackCommand(MiniTimelineDirector timelineDirector, TrackData data, TimelineEditorUI editor)
            : base($"Add {data.type} Track")
        {
            director = timelineDirector;
            trackData = data;
            editorUI = editor;
        }
        
        protected override void ExecuteInternal()
        {
            // TODO: Add track through proper project API
            // director.Project.tracks.Add(trackData);
            // director.MarkDirty();
            
            // Rebuild UI
            // editorUI?.BuildTimelineUI();
            
            Debug.Log($"Adding {trackData.type} track");
        }
        
        protected override void UndoInternal()
        {
            if (createdTrack != null)
            {
                // TODO: Remove track through proper project API
                // director.Project.tracks.Remove(trackData);
                // director.MarkDirty();
                
                // Rebuild UI
                // editorUI?.BuildTimelineUI();
                
                Debug.Log($"Removing {trackData.type} track");
            }
        }
    }
    
    /// <summary>
    /// Command for removing a track from the timeline
    /// </summary>
    public class RemoveTrackCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly IMiniTrack track;
        private readonly TrackData trackBackup;
        private readonly TimelineEditorUI editorUI;
        private readonly int trackIndex;
        
        public RemoveTrackCommand(MiniTimelineDirector timelineDirector, IMiniTrack trackToRemove, TimelineEditorUI editor)
            : base($"Remove Track")
        {
            director = timelineDirector;
            track = trackToRemove;
            editorUI = editor;
            
            // TODO: Backup track data and find index
            // trackIndex = director.Project.tracks.FindIndex(t => t.id == track.Id);
            // trackBackup = director.Project.tracks[trackIndex];
        }
        
        protected override void ExecuteInternal()
        {
            // TODO: Remove track through proper project API
            // director.Project.tracks.RemoveAt(trackIndex);
            // director.MarkDirty();
            
            // Rebuild UI
            // editorUI?.BuildTimelineUI();
            
            Debug.Log($"Removing track {track.Id}");
        }
        
        protected override void UndoInternal()
        {
            // TODO: Restore track through proper project API
            // director.Project.tracks.Insert(trackIndex, trackBackup);
            // director.MarkDirty();
            
            // Rebuild UI
            // editorUI?.BuildTimelineUI();
            
            Debug.Log($"Restoring track {track.Id}");
        }
    }
}
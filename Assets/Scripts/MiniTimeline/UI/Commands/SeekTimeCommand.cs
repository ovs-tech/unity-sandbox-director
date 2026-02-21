using Systems.CommandSystem;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for changing timeline playback time (scrubbing)
    /// This command typically doesn't need undo/redo as it's a navigation operation
    /// </summary>
    public class SeekTimeCommand : CommandBase
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
        
        public override bool CanMergeWith(ICommand other)
        {
            // Seek commands can be merged to avoid cluttering undo history
            return other is SeekTimeCommand;
        }
        
        public override void MergeWith(ICommand other)
        {
            if (other is SeekTimeCommand seekCommand)
            {
                // Keep our old time, update to the new command's new time
                // newTime = seekCommand.newTime; // Would be done through proper API
            }
        }
    }
    
}

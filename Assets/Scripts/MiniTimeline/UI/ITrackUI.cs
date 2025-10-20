namespace MiniTimeline.UI
{
    /// <summary>
    /// Interface for timeline editor UI implementations that can be used by commands
    /// </summary>
    public interface ITrackUI
    {
        /// <summary>
        /// Rebuild the timeline UI from the current project data
        /// </summary>
        void RebuildClipUIs();
    }
}
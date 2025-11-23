namespace MiniTimeline.UI
{
    /// <summary>
    /// Interface for timeline editor UI implementations that can be used by commands
    /// </summary>
    public interface ITimelineEditorUI
    {
        /// <summary>
        /// Rebuild the timeline UI from the current project data
        /// </summary>
        void BuildTimelineUI();

        /// <summary>
        /// Set the zoom level of the timeline
        /// </summary>
        /// <param name="zoom">Zoom level to set</param>
        void SetZoom(float zoom);
    }
}
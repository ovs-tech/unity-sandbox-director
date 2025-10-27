namespace MiniTimeline.Core
{
    /// <summary>
    /// Defines when a track should trigger evaluation callbacks
    /// </summary>
    public enum EvaluateMode
    {
        /// <summary>
        /// Chỉ bắt đầu - Only trigger OnEnter when entering active state
        /// </summary>
        OnEnter,
        
        /// <summary>
        /// Chỉ kết thúc - Only trigger OnExit when leaving active state
        /// </summary>
        OnExit,
        
        /// <summary>
        /// Bắt đầu và Kết thúc - Trigger OnEnter and OnExit
        /// </summary>
        OnEnterAndExit,
        
        /// <summary>
        /// Trong toàn thời gian evaluate - Trigger OnEnter, OnEvaluate, and OnExit (Default)
        /// </summary>
        Continuous
    }
}

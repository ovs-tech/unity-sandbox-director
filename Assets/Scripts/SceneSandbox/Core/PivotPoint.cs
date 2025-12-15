namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Pivot point options for grid and bounds positioning (3D box)
    /// Total: 27 points (1 center + 6 face centers + 12 edge centers + 8 corners)
    /// </summary>
    public enum PivotPoint
    {
        // Center (1)
        Center,
        
        // Face Centers (6 faces)
        FrontCenter,
        BackCenter,
        LeftCenter,
        RightCenter,
        TopCenter,
        BottomCenter,
        
        // Bottom Edge Centers (4 edges)
        BottomFrontEdge,
        BottomBackEdge,
        BottomLeftEdge,
        BottomRightEdge,
        
        // Top Edge Centers (4 edges)
        TopFrontEdge,
        TopBackEdge,
        TopLeftEdge,
        TopRightEdge,
        
        // Vertical Edge Centers (4 edges)
        FrontLeftEdge,
        FrontRightEdge,
        BackLeftEdge,
        BackRightEdge,
        
        // Bottom Corners (4 corners)
        BottomFrontLeft,
        BottomFrontRight,
        BottomBackLeft,
        BottomBackRight,
        
        // Top Corners (4 corners)
        TopFrontLeft,
        TopFrontRight,
        TopBackLeft,
        TopBackRight
    }
}

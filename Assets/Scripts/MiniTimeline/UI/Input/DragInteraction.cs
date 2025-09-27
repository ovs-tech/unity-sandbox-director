using System;
using UnityEngine;

namespace MiniTimeline.UI.Input
{
    /// <summary>
    /// Handles drag interactions (movement while pressed)
    /// </summary>
    public class DragInteraction : IInputInteraction
    {
        public event Action<Vector2> OnDragStart; // screen position where drag started
        public event Action<Vector2, Vector2> OnDragging; // current position, delta from start
        public event Action<Vector2, Vector2> OnDragEnd; // end position, total delta
        
        private bool _isActive;
        private bool _isDragging; // true when drag threshold is reached
        private Vector2 _startPosition;
        private Vector2 _previousPosition;
        private float _startTime;
        
        // Configuration
        private readonly float _dragThreshold;
        
        public DragInteraction(float dragThreshold = 10f)
        {
            _dragThreshold = dragThreshold;
        }
        
        public bool IsActive => _isActive;
        public bool IsDragging => _isDragging;
        public int Priority => 2; // Medium priority, can be interrupted by hold
        
        public void OnInteractionStart(Vector2 screenPosition)
        {
            _isActive = true;
            _isDragging = false;
            _startPosition = screenPosition;
            _previousPosition = screenPosition;
            _startTime = Time.unscaledTime;
        }
        
        public void OnInteractionUpdate(Vector2 screenPosition, float deltaTime)
        {
            if (!_isActive) return;
            
            // Check if we should start dragging
            if (!_isDragging)
            {
                float distance = Vector2.Distance(_startPosition, screenPosition);
                if (distance >= _dragThreshold)
                {
                    _isDragging = true;
                    OnDragStart?.Invoke(_startPosition);
                }
            }
            
            // Continue dragging
            if (_isDragging)
            {
                Vector2 deltaFromStart = screenPosition - _startPosition;
                OnDragging?.Invoke(screenPosition, deltaFromStart);
            }
            
            _previousPosition = screenPosition;
        }
        
        public void OnInteractionEnd(Vector2 screenPosition)
        {
            if (!_isActive) return;
            
            if (_isDragging)
            {
                Vector2 totalDelta = screenPosition - _startPosition;
                OnDragEnd?.Invoke(screenPosition, totalDelta);
            }
            
            _isActive = false;
            _isDragging = false;
        }
        
        public void OnInteractionCancel()
        {
            _isActive = false;
            _isDragging = false;
        }
    }
}
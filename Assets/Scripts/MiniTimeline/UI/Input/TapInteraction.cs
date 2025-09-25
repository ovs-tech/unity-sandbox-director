using System;
using UnityEngine;

namespace MiniTimeline.UI.Input
{
    /// <summary>
    /// Handles tap (quick click) interactions
    /// </summary>
    public class TapInteraction : IInputInteraction
    {
        public event Action<Vector2> OnTap; // screen position
        
        private bool _isActive;
        private Vector2 _startPosition;
        private float _startTime;
        
        // Configuration
        private readonly float _maxTapDuration;
        private readonly float _maxTapDistance;
        
        public TapInteraction(float maxTapDuration = 0.3f, float maxTapDistance = 50f)
        {
            _maxTapDuration = maxTapDuration;
            _maxTapDistance = maxTapDistance;
        }
        
        public bool IsActive => _isActive;
        public int Priority => 1; // Lower priority, can be overridden by hold
        
        public void OnInteractionStart(Vector2 screenPosition)
        {
            _isActive = true;
            _startPosition = screenPosition;
            _startTime = Time.unscaledTime;
        }
        
        public void OnInteractionUpdate(Vector2 screenPosition, float deltaTime)
        {
            if (!_isActive) return;
            
            // Check if we've moved too far (cancel tap)
            float distance = Vector2.Distance(_startPosition, screenPosition);
            if (distance > _maxTapDistance)
            {
                OnInteractionCancel();
                return;
            }
            
            // Check if we've held too long (let hold interaction take over)
            float elapsed = Time.unscaledTime - _startTime;
            if (elapsed > _maxTapDuration)
            {
                OnInteractionCancel();
            }
        }
        
        public void OnInteractionEnd(Vector2 screenPosition)
        {
            if (!_isActive) return;
            
            float elapsed = Time.unscaledTime - _startTime;
            float distance = Vector2.Distance(_startPosition, screenPosition);
            
            // Valid tap: within time and distance limits
            if (elapsed <= _maxTapDuration && distance <= _maxTapDistance)
            {
                OnTap?.Invoke(screenPosition);
            }
            
            _isActive = false;
        }
        
        public void OnInteractionCancel()
        {
            _isActive = false;
        }
    }
}
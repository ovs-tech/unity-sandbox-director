using System;
using UnityEngine;

namespace MiniTimeline.UI.Input
{
    /// <summary>
    /// Handles hold (long press) interactions
    /// </summary>
    public class HoldInteraction : IInputInteraction
    {
        public event Action<Vector2> OnHoldStart; // screen position where hold started
        public event Action<Vector2, float> OnHolding; // screen position, hold duration
        public event Action<Vector2, float> OnHoldEnd; // screen position, total hold duration
        
        private bool _isActive;
        private bool _isHolding; // true when hold threshold is reached
        private Vector2 _startPosition;
        private float _startTime;
        
        // Configuration
        private readonly float _holdThreshold;
        private readonly float _maxStartDistance;
        
        public HoldInteraction(float holdThreshold = 0.5f, float maxStartDistance = 30f)
        {
            _holdThreshold = holdThreshold;
            _maxStartDistance = maxStartDistance;
        }
        
        public bool IsActive => _isActive;
        public bool IsHolding => _isHolding;
        public int Priority => 3; // Highest priority - hold should not be interrupted
        
        public void OnInteractionStart(Vector2 screenPosition)
        {
            _isActive = true;
            _isHolding = false;
            _startPosition = screenPosition;
            _startTime = Time.unscaledTime;
            
            if (InputInteractionManager.DebugMode)
            {
                Debug.Log($"[HoldInteraction] Started at {screenPosition}, threshold: {_holdThreshold}s, maxDistance: {_maxStartDistance}px");
            }
        }
        
        public void OnInteractionUpdate(Vector2 screenPosition, float deltaTime)
        {
            if (!_isActive) return;
            
            float elapsed = Time.unscaledTime - _startTime;
            float distance = Vector2.Distance(_startPosition, screenPosition);
            
            if (InputInteractionManager.DebugMode)
            {
                Debug.Log($"[HoldInteraction] Update - Elapsed: {elapsed:F2}s, Distance: {distance:F1}px, IsHolding: {_isHolding}");
            }
            
            // Check if we should start holding
            if (!_isHolding && elapsed >= _holdThreshold)
            {
                if (distance <= _maxStartDistance)
                {
                    _isHolding = true;
                    if (InputInteractionManager.DebugMode)
                    {
                        Debug.Log($"[HoldInteraction] HOLD STARTED! Elapsed: {elapsed:F2}s, Distance: {distance:F1}px");
                    }
                    OnHoldStart?.Invoke(_startPosition);
                }
                else
                {
                    // Moved too far during hold threshold, cancel
                    if (InputInteractionManager.DebugMode)
                    {
                        Debug.Log($"[HoldInteraction] CANCELLED - Moved too far! Distance: {distance:F1}px > {_maxStartDistance}px");
                    }
                    OnInteractionCancel();
                    return;
                }
            }
            
            // Continue holding
            if (_isHolding)
            {
                OnHolding?.Invoke(screenPosition, elapsed);
            }
        }
        
        public void OnInteractionEnd(Vector2 screenPosition)
        {
            if (!_isActive) return;
            
            float elapsed = Time.unscaledTime - _startTime;
            
            if (InputInteractionManager.DebugMode)
            {
                Debug.Log($"[HoldInteraction] Ended after {elapsed:F2}s, was holding: {_isHolding}");
            }
            
            if (_isHolding)
            {
                OnHoldEnd?.Invoke(screenPosition, elapsed);
            }
            
            _isActive = false;
            _isHolding = false;
        }
        
        public void OnInteractionCancel()
        {
            if (InputInteractionManager.DebugMode && _isActive)
            {
                Debug.Log($"[HoldInteraction] Cancelled - was active: {_isActive}, was holding: {_isHolding}");
            }
            
            _isActive = false;
            _isHolding = false;
        }
    }
}
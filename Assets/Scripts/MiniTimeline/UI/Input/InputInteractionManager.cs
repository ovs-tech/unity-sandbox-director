using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniTimeline.UI.Input
{
    /// <summary>
    /// Manages multiple input interactions and ensures only the appropriate ones are active
    /// based on priority and current state
    /// </summary>
    public class InputInteractionManager
    {
        /// <summary>
        /// Static debug mode to enable/disable logging
        /// </summary>
        public static bool DebugMode { get; set; } = true; // Set to true by default for debugging
        
    private readonly List<IInputInteraction> _interactions;
    private IInputInteraction _activeInteraction;
    private bool _isPressed;
    private Vector2 _currentPosition;
    private float _lastPointPerformedTime = 0f;
    public bool IsEnabled { get; set; } = true;
        
        public InputInteractionManager()
        {
            _interactions = new List<IInputInteraction>();
        }
        
        /// <summary>
        /// Add an interaction to be managed
        /// </summary>
        public void AddInteraction(IInputInteraction interaction)
        {
            if (!_interactions.Contains(interaction))
            {
                _interactions.Add(interaction);
                // Sort by priority (highest first)
                _interactions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                
                if (DebugMode)
                {
                    Debug.Log($"[InputInteractionManager] Added interaction: {interaction.GetType().Name} with priority {interaction.Priority}");
                }
            }
        }
        
        /// <summary>
        /// Remove an interaction from management
        /// </summary>
        public void RemoveInteraction(IInputInteraction interaction)
        {
            _interactions.Remove(interaction);
        }
        
        /// <summary>
        /// Called when input press starts
        /// </summary>
        public void OnPressStart(Vector2 screenPosition)
        {
            if (!IsEnabled) return;
            _isPressed = true;
            _currentPosition = screenPosition;
            _lastPointPerformedTime = Time.unscaledTime; // Reset timing when press starts
            if (DebugMode)
            {
                Debug.Log($"[InputInteractionManager] OnPressStart at {screenPosition}. Active interactions: {_interactions.Count}");
            }
            // Cancel any existing active interaction
            if (_activeInteraction != null)
            {
                if (DebugMode)
                {
                    Debug.Log($"[InputInteractionManager] Cancelling previous active interaction: {_activeInteraction.GetType().Name}");
                }
                _activeInteraction.OnInteractionCancel();
                _activeInteraction = null;
            }
            // Start all interactions (they will self-manage their states)
            foreach (var interaction in _interactions)
            {
                if (DebugMode)
                {
                    Debug.Log($"[InputInteractionManager] Starting interaction: {interaction.GetType().Name}");
                }
                interaction.OnInteractionStart(screenPosition);
            }
        }
        
        /// <summary>
        /// Update method to be called continuously - handles timing internally
        /// </summary>
        public void Update()
        {
            if (!IsEnabled) return;
            if (!_isPressed) return;
            
            // Use current mouse position
            Vector2 screenPosition = UnityEngine.Input.mousePosition;
            _currentPosition = screenPosition;
            
            // Calculate delta time internally
            float now = Time.unscaledTime;
            float deltaTime = _lastPointPerformedTime > 0f ? (now - _lastPointPerformedTime) : Time.unscaledDeltaTime;
            _lastPointPerformedTime = now;
            
            OnPressUpdate(screenPosition, deltaTime);
        }
        
        /// <summary>
        /// Called continuously while pressed - overloaded version that handles timing internally
        /// </summary>
        public void OnPressUpdate(Vector2 screenPosition)
        {
            if (!IsEnabled) return;
            if (!_isPressed) return;
            
            // Calculate delta time internally
            float now = Time.unscaledTime;
            float deltaTime = _lastPointPerformedTime > 0f ? (now - _lastPointPerformedTime) : Time.unscaledDeltaTime;
            _lastPointPerformedTime = now;
            
            OnPressUpdate(screenPosition, deltaTime);
        }
        
        /// <summary>
        /// Called continuously while pressed
        /// </summary>
        public void OnPressUpdate(Vector2 screenPosition, float deltaTime)
        {
            if (!IsEnabled) return;
            if (DebugMode)
            {
                Debug.Log($"[InputInteractionManager] OnPressUpdate called. screenPosition: {screenPosition}, deltaTime: {deltaTime}, _isPressed: {_isPressed}");
            }
            if (!_isPressed) return;
            _currentPosition = screenPosition;
            // Update all interactions
            foreach (var interaction in _interactions)
            {
                if (interaction.IsActive)
                {
                    interaction.OnInteractionUpdate(screenPosition, deltaTime);
                }
            }
            // Debug: Log active interactions
            var activeInteractions = _interactions.Where(i => i.IsActive).ToList();
            if (DebugMode && activeInteractions.Any())
            {
                var activeNames = string.Join(", ", activeInteractions.Select(i => 
                {
                    string status = "";
                    if (i is HoldInteraction holdInt)
                    {
                        status = $"(IsHolding: {holdInt.IsHolding})";
                    }
                    else if (i is DragInteraction dragInt)
                    {
                        status = $"(IsDragging: {dragInt.IsDragging})";
                    }
                    return $"{i.GetType().Name}{status}";
                }));
                Debug.Log($"[InputInteractionManager] Active interactions: {activeNames}");
            }
            // Check if any high-priority interaction is actually performing its main action
            var performingInteraction = activeInteractions
                .Where(i => IsInteractionPerforming(i))
                .OrderByDescending(i => i.Priority)
                .FirstOrDefault();
            if (performingInteraction != null)
            {
                if (DebugMode && performingInteraction != _activeInteraction)
                {
                    Debug.Log($"[InputInteractionManager] Setting active interaction to: {performingInteraction.GetType().Name}");
                }
                if (performingInteraction != _activeInteraction)
                {
                    // Cancel lower priority interactions only when a higher priority one is actually performing
                    foreach (var interaction in activeInteractions)
                    {
                        if (interaction != performingInteraction && interaction.Priority < performingInteraction.Priority)
                        {
                            interaction.OnInteractionCancel();
                        }
                    }
                    _activeInteraction = performingInteraction;
                }
            }
        }
        
        /// <summary>
        /// Check if an interaction is actually performing its main action (not just active)
        /// </summary>
        private bool IsInteractionPerforming(IInputInteraction interaction)
        {
            // Check specific interaction types for their performing state
            if (interaction is DragInteraction dragInteraction)
            {
                bool isDragging = dragInteraction.IsDragging;
                if (DebugMode)
                {
                    Debug.Log($"[InputInteractionManager] DragInteraction performing check: {isDragging}");
                }
                return isDragging;
            }
            if (interaction is HoldInteraction holdInteraction)
            {
                bool isHolding = holdInteraction.IsHolding;
                if (DebugMode)
                {
                    Debug.Log($"[InputInteractionManager] HoldInteraction performing check: {isHolding}");
                }
                return isHolding;
            }
            if (interaction is TapInteraction)
            {
                // Tap interactions are always "performing" when active since they're instantaneous
                if (DebugMode)
                {
                    Debug.Log($"[InputInteractionManager] TapInteraction performing check: false (doesn't block others)");
                }
                return false; // Taps don't block other interactions
            }
            
            // Default: assume active means performing for unknown interaction types
            if (DebugMode)
            {
                Debug.Log($"[InputInteractionManager] Unknown interaction type {interaction.GetType().Name} performing check: {interaction.IsActive}");
            }
            return interaction.IsActive;
        }
        
        /// <summary>
        /// Called when input press ends
        /// </summary>
        public void OnPressEnd(Vector2 screenPosition)
        {
            if (!IsEnabled) return;
            if (!_isPressed) return;
            _isPressed = false;
            _currentPosition = screenPosition;
            _lastPointPerformedTime = 0f; // Reset timing when press ends
            if (DebugMode)
            {
                Debug.Log($"[InputInteractionManager] OnPressEnd at {screenPosition}");
            }
            // End all active interactions
            foreach (var interaction in _interactions)
            {
                if (interaction.IsActive)
                {
                    if (DebugMode)
                    {
                        Debug.Log($"[InputInteractionManager] Ending interaction: {interaction.GetType().Name}");
                    }
                    interaction.OnInteractionEnd(screenPosition);
                }
            }
            _activeInteraction = null;
        }
        
        /// <summary>
        /// Cancel all current interactions
        /// </summary>
        public void CancelAll()
        {
            if (!IsEnabled) return;
            foreach (var interaction in _interactions)
            {
                if (interaction.IsActive)
                {
                    interaction.OnInteractionCancel();
                }
            }
            _activeInteraction = null;
            _isPressed = false;
        }
        
        /// <summary>
        /// Get the currently active interaction (highest priority active interaction)
        /// </summary>
        public IInputInteraction GetActiveInteraction()
        {
            return _activeInteraction;
        }
        
        /// <summary>
        /// Check if any interaction is currently active
        /// </summary>
        public bool HasActiveInteraction => _interactions.Any(i => i.IsActive);
        
        /// <summary>
        /// Check if currently in a pressed state
        /// </summary>
        public bool IsPressed => _isPressed;
    }
}
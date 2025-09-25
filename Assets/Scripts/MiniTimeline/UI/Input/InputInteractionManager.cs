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
        private readonly List<IInputInteraction> _interactions;
        private IInputInteraction _activeInteraction;
        private bool _isPressed;
        private Vector2 _currentPosition;
        
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
            _isPressed = true;
            _currentPosition = screenPosition;
            
            // Cancel any existing active interaction
            if (_activeInteraction != null)
            {
                _activeInteraction.OnInteractionCancel();
                _activeInteraction = null;
            }
            
            // Start all interactions (they will self-manage their states)
            foreach (var interaction in _interactions)
            {
                interaction.OnInteractionStart(screenPosition);
            }
        }
        
        /// <summary>
        /// Called continuously while pressed
        /// </summary>
        public void OnPressUpdate(Vector2 screenPosition, float deltaTime)
        {
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
            
            // Determine which interaction should be active based on priority
            // Only one interaction can be "active" at a time
            var newActiveInteraction = _interactions
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.Priority)
                .FirstOrDefault();
            
            // If the active interaction changed, cancel lower priority ones
            if (newActiveInteraction != _activeInteraction && newActiveInteraction != null)
            {
                // Cancel lower priority interactions
                foreach (var interaction in _interactions)
                {
                    if (interaction != newActiveInteraction && interaction.IsActive)
                    {
                        if (interaction.Priority < newActiveInteraction.Priority)
                        {
                            interaction.OnInteractionCancel();
                        }
                    }
                }
                
                _activeInteraction = newActiveInteraction;
            }
        }
        
        /// <summary>
        /// Called when input press ends
        /// </summary>
        public void OnPressEnd(Vector2 screenPosition)
        {
            if (!_isPressed) return;
            
            _isPressed = false;
            _currentPosition = screenPosition;
            
            // End all active interactions
            foreach (var interaction in _interactions)
            {
                if (interaction.IsActive)
                {
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
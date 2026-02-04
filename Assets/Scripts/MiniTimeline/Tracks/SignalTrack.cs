
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
	/// <summary>
	/// Signal track for triggering signals and markers
	/// Fires events when playhead crosses signal markers
	/// </summary>
	[Serializable]
	public class SignalTrack : MiniTrackBase<SignalClip>
	{
		// Events
		public event Action<TimelineEvent> OnTimelineEvent;

		// Unity Events for editor binding
		[SerializeField] private UnityEvent<TimelineEvent> onTimelineEvent = new UnityEvent<TimelineEvent>();

		// Event routing
		private readonly Dictionary<string, Action<TimelineEvent>> eventHandlers = new Dictionary<string, Action<TimelineEvent>>();

		// Performance optimization
		private readonly List<SignalClip> tempActiveSignals = new List<SignalClip>();

		#region Track Lifecycle

		protected override void OnPrepare()
		{
			// Sort clips by time for efficient processing
			clips.Sort((a, b) => a.Start.CompareTo(b.Start));

			// Debug.Log($"[SignalTrack] Prepared track '{Id}' with {clips.Count} signal clips");
		}

		protected override void OnEvaluate(float time, bool scrub)
		{
			// Get the director to access previous time
			var director = UnityEngine.Object.FindFirstObjectByType<MiniTimelineDirector>();
			if (director == null) return;

			float previousTime = director.PreviousTime;

			// Check all signal clips for triggers
			tempActiveSignals.Clear();

			foreach (var signal in clips)
			{
				if (signal.ShouldTrigger(previousTime, time, scrub))
				{
					tempActiveSignals.Add(signal);
				}
			}

			// Fire events for triggered signals
			foreach (var signal in tempActiveSignals)
			{
				FireEvent(signal, time, scrub, previousTime);
			}

			tempActiveSignals.Clear();
		}

		protected override void OnCleanup()
		{
			// Clear event handlers
			if (eventHandlers != null)
			{
				eventHandlers.Clear();
			}

			// Debug.Log($"[SignalTrack] Cleaned up track '{Id}'");
		}

		#endregion

		#region Event Handling

		/// <summary>
		/// Fire an event for a signal clip
		/// </summary>
		/// <param name="signal">Signal clip that triggered</param>
		/// <param name="currentTime">Current timeline time</param>
		/// <param name="scrub">Whether this is a scrub operation</param>
		/// <param name="previousTime">Previous timeline time</param>
		private void FireEvent(SignalClip signal, float currentTime, bool scrub, float previousTime)
		{
			var timelineEvent = new TimelineEvent
			{
				eventId = signal.eventId,
				payload = signal.payload,
				time = signal.Start,
				direction = signal.GetTriggerDirection(previousTime, currentTime),
				isScrub = scrub,
				sourceClip = signal
			};

			try
			{
				// Fire global event
				OnTimelineEvent?.Invoke(timelineEvent);

				// Fire Unity event
				onTimelineEvent?.Invoke(timelineEvent);

				// Fire specific event handler if registered
				if (eventHandlers != null && eventHandlers.TryGetValue(signal.eventId, out var handler))
				{
					handler.Invoke(timelineEvent);
				}

				// Debug.Log($"[SignalTrack] Fired event '{signal.eventId}' at time {signal.Start:F2} (payload: '{signal.payload}')");
			}
			catch (Exception)
			{
				// Debug.LogError($"[SignalTrack] Error firing event '{signal.eventId}': {e.Message}");
			}
		}

		/// <summary>
		/// Register a specific event handler
		/// </summary>
		/// <param name="eventId">Event ID to listen for</param>
		/// <param name="handler">Event handler</param>
		public void RegisterEventHandler(string eventId, Action<TimelineEvent> handler)
		{
			if (string.IsNullOrEmpty(eventId) || handler == null) return;
			if (eventHandlers == null) return;

			if (eventHandlers.ContainsKey(eventId))
			{
				eventHandlers[eventId] += handler;
			}
			else
			{
				eventHandlers[eventId] = handler;
			}

			// Debug.Log($"[SignalTrack] Registered handler for event '{eventId}'");
		}

		/// <summary>
		/// Unregister a specific event handler
		/// </summary>
		/// <param name="eventId">Event ID</param>
		/// <param name="handler">Event handler to remove</param>
		public void UnregisterEventHandler(string eventId, Action<TimelineEvent> handler)
		{
			if (string.IsNullOrEmpty(eventId) || handler == null) return;
			if (eventHandlers == null) return;

			if (eventHandlers.TryGetValue(eventId, out var existingHandler))
			{
				eventHandlers[eventId] = existingHandler - handler;

				if (eventHandlers[eventId] == null)
				{
					eventHandlers.Remove(eventId);
				}
			}
		}

		/// <summary>
		/// Clear all event handlers for a specific event ID
		/// </summary>
		/// <param name="eventId">Event ID to clear</param>
		public void ClearEventHandlers(string eventId)
		{
			if (eventHandlers != null)
			{
				eventHandlers.Remove(eventId);
			}
		}

		/// <summary>
		/// Clear all event handlers
		/// </summary>
		public void ClearAllEventHandlers()
		{
			if (eventHandlers != null)
			{
				eventHandlers.Clear();
			}
		}

		#endregion

		#region Public API

		/// <summary>
		/// Add a signal clip to this track
		/// </summary>
		/// <param name="time">Signal time</param>
		/// <param name="eventId">Event identifier</param>
		/// <param name="payload">Event payload</param>
		/// <returns>Created signal clip</returns>
		public SignalClip AddSignal(float time, string eventId, string payload = "")
		{
			var signal = new SignalClip
			{
				Id = Guid.NewGuid().ToString(),
				Start = time,
				eventId = eventId,
				payload = payload
			};

			clips.Add(signal);

			// Keep clips sorted by time
			clips.Sort((a, b) => a.Start.CompareTo(b.Start));

			return signal;
		}

		/// <summary>
		/// Remove a signal clip
		/// </summary>
		/// <param name="clipId">Clip ID to remove</param>
		/// <returns>True if removed</returns>
		public bool RemoveSignal(string clipId)
		{
			var signal = clips.FirstOrDefault(c => c.Id == clipId);
			if (signal != null)
			{
				clips.Remove(signal);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get all signals with a specific event ID
		/// </summary>
		/// <param name="eventId">Event ID to search for</param>
		/// <returns>Collection of matching signals</returns>
		public IEnumerable<SignalClip> GetSignalsByEventId(string eventId)
		{
			return clips.Where(c => c.eventId == eventId);
		}

		/// <summary>
		/// Get signal at specific time
		/// </summary>
		/// <param name="time">Time to check</param>
		/// <param name="tolerance">Time tolerance</param>
		/// <returns>Signal at time or null</returns>
		public SignalClip GetSignalAtTime(float time, float tolerance = 0.01f)
		{
			return clips.FirstOrDefault(c => Mathf.Abs(c.Start - time) <= tolerance);
		}

		/// <summary>
		/// Get all signals within a time range
		/// </summary>
		/// <param name="startTime">Range start</param>
		/// <param name="endTime">Range end</param>
		/// <returns>Signals within range</returns>
		public IEnumerable<SignalClip> GetSignalsInRange(float startTime, float endTime)
		{
			return clips.Where(c => c.Start >= startTime && c.Start <= endTime);
		}

		/// <summary>
		/// Manually fire an event (useful for testing)
		/// </summary>
		/// <param name="eventId">Event ID</param>
		/// <param name="payload">Event payload</param>
		/// <param name="time">Event time</param>
		public void FireManualEvent(string eventId, string payload = "", float time = 0f)
		{
			var manualEvent = new TimelineEvent
			{
				eventId = eventId,
				payload = payload,
				time = time,
				direction = 1,
				isScrub = false,
				sourceClip = null
			};

			OnTimelineEvent?.Invoke(manualEvent);
			onTimelineEvent?.Invoke(manualEvent);

			if (eventHandlers != null && eventHandlers.TryGetValue(eventId, out var handler))
			{
				handler.Invoke(manualEvent);
			}
		}

		/// <summary>
		/// Get Unity event for editor binding
		/// </summary>
		/// <returns>Unity event</returns>
		public UnityEvent<TimelineEvent> GetUnityEvent()
		{
			return onTimelineEvent;
		}

		#endregion
	}
}

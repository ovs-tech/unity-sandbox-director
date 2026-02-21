using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Systems.Core.UI.Core.Helpers
{
    /// <summary>
    /// Helper class to safely create UI elements and prevent TextMeshPro threading issues
    /// Ensures all UI creation happens sequentially on the main thread
    /// </summary>
    public static class UICreationHelper
    {
        private static bool isCreatingUI = false;
        private static readonly Queue<Action> uiCreationQueue = new Queue<Action>();
        private static MonoBehaviour coroutineRunner;
        
        /// <summary>
        /// Initialize the UI creation helper with a MonoBehaviour to run coroutines
        /// </summary>
        public static void Initialize(MonoBehaviour runner)
        {
            coroutineRunner = runner;
        }
        
        /// <summary>
        /// Safely create a TextMeshProUGUI component with thread safety
        /// </summary>
        public static void SafeCreateTextMeshPro(GameObject parent, Action<TextMeshProUGUI> onCreated)
        {
            if (!Application.isPlaying)
            {
                // In editor, create immediately
                var textMesh = parent.AddComponent<TextMeshProUGUI>();
                onCreated?.Invoke(textMesh);
                return;
            }
            
            // Queue the creation to prevent concurrent access
            uiCreationQueue.Enqueue(() =>
            {
                try
                {
                    var textMesh = parent.AddComponent<TextMeshProUGUI>();
                    onCreated?.Invoke(textMesh);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create TextMeshProUGUI: {e.Message}");
                }
            });
            
            ProcessQueue();
        }
        
        /// <summary>
        /// Safely create a TMP_InputField component with thread safety
        /// </summary>
        public static void SafeCreateInputField(GameObject parent, Action<TMP_InputField> onCreated)
        {
            if (!Application.isPlaying)
            {
                // In editor, create immediately
                var inputField = parent.AddComponent<TMP_InputField>();
                onCreated?.Invoke(inputField);
                return;
            }
            
            // Queue the creation to prevent concurrent access
            uiCreationQueue.Enqueue(() =>
            {
                try
                {
                    var inputField = parent.AddComponent<TMP_InputField>();
                    onCreated?.Invoke(inputField);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create TMP_InputField: {e.Message}");
                }
            });
            
            ProcessQueue();
        }
        
        /// <summary>
        /// Queue any UI creation action to be executed safely
        /// </summary>
        public static void SafeExecuteUIAction(Action action)
        {
            if (!Application.isPlaying)
            {
                // In editor, execute immediately
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"UI Action failed: {e.Message}");
                }
                return;
            }
            
            uiCreationQueue.Enqueue(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Queued UI Action failed: {e.Message}");
                }
            });
            
            ProcessQueue();
        }
        
        /// <summary>
        /// Process the UI creation queue with delays to prevent threading issues
        /// </summary>
        private static void ProcessQueue()
        {
            if (isCreatingUI || uiCreationQueue.Count == 0 || coroutineRunner == null)
                return;
            
            coroutineRunner.StartCoroutine(ProcessQueueCoroutine());
        }
        
        /// <summary>
        /// Coroutine to process UI creation queue with small delays
        /// </summary>
        private static IEnumerator ProcessQueueCoroutine()
        {
            isCreatingUI = true;
            
            while (uiCreationQueue.Count > 0)
            {
                var action = uiCreationQueue.Dequeue();
                
                // Execute on main thread
                if (action != null)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"UI Creation action failed: {e.Message}");
                    }
                }
                
                // Small delay to prevent concurrent font access
                yield return new WaitForEndOfFrame();
            }
            
            isCreatingUI = false;
        }
        
        /// <summary>
        /// Create TextMeshProUGUI with standard settings
        /// </summary>
        public static TextMeshProUGUI CreateStandardTextMesh(GameObject parent, string text = "", float fontSize = 14f, Color? color = null)
        {
            var textMesh = parent.AddComponent<TextMeshProUGUI>();
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = color ?? Color.white;
            return textMesh;
        }
        
        /// <summary>
        /// Create a standard RectTransform setup
        /// </summary>
        public static RectTransform SetupStandardRect(GameObject obj, Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            var rect = obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? Vector2.zero;
            rect.anchorMax = anchorMax ?? Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }
        
        /// <summary>
        /// Safely destroy UI elements
        /// </summary>
        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            
            try
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(obj);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to destroy UI object: {e.Message}");
            }
        }
    }
}
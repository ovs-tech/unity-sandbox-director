#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Timeline {
    public class TimelineEditorView : MonoBehaviour {
        public VisualElement Root { get; private set; }
        
        [SerializeField] VisualTreeAsset _uxml;
        [SerializeField] StyleSheet _uss;
        UIDocument _document;

        public IEnumerator InitializeView(TimelineEditorController.ViewModel viewModel) {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document == null) {
                _document = gameObject.AddComponent<UIDocument>();
            }
            Root = _document.rootVisualElement;
            Root.Clear();

            if (_uss != null) Root.styleSheets.Add(_uss);
            if (_uxml != null) {
                var tree = _uxml.Instantiate();
                Root.Add(tree);
            } else {
                Debug.LogWarning("UXML not assigned in TimelineEditorView");
            }

            yield return null;
        }

        public Button GetButton(string name) => Root?.Q<Button>(name);
        public Slider GetSlider(string name) => Root?.Q<Slider>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);
    }
}
#endif
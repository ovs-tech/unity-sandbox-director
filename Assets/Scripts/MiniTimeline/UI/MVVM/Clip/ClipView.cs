#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Clip {
    public class ClipView : MonoBehaviour {
        public VisualElement Root { get; private set; }
        
        [SerializeField] VisualTreeAsset _uxml;
        [SerializeField] StyleSheet _uss;
        UIDocument _document;

        public IEnumerator InitializeView(ClipController.ViewModel viewModel) {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document == null) _document = gameObject.AddComponent<UIDocument>();
            Root = _document.rootVisualElement;
            Root.Clear();

            if (_uss != null) Root.styleSheets.Add(_uss);
            if (_uxml != null) Root.Add(_uxml.Instantiate());
            yield return null;
        }

        public Button GetButton(string name) => Root?.Q<Button>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);
    }
}
#endif
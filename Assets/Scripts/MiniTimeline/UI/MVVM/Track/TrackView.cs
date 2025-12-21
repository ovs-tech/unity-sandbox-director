#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Track {
    public class TrackView {
        public VisualElement Root { get; private set; }

        public TrackView(VisualElement root, VisualTreeAsset uxml = null, StyleSheet uss = null) {
            Root = root;
            Initialize(uxml, uss);
        }

        void Initialize(VisualTreeAsset uxml, StyleSheet uss) {
            if (Root == null) return;
            
            // Add stylesheet if provided
            if (uss != null) {
                Root.styleSheets.Add(uss);
            }
            
            // Instantiate and add UXML if provided
            if (uxml != null) {
                var tree = uxml.Instantiate();
                Root.Add(tree);
            }
        }

        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public Toggle GetToggle(string name) => Root?.Q<Toggle>(name);
        public Button GetButton(string name) => Root?.Q<Button>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);
    }
}
#endif
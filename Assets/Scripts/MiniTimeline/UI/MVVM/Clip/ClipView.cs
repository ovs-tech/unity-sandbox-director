#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Clip {
    public class ClipView {
        public VisualElement Root { get; private set; }

        public ClipView(VisualElement root, VisualTreeAsset uxml = null, StyleSheet uss = null) {
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

        public Button GetButton(string name) => Root?.Q<Button>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);
    }
}
#endif
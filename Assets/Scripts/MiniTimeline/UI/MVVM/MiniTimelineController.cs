using System.Collections;
using UnityEngine;
using MiniTimeline.UI.MVVM.Timeline;
using MiniTimeline.UI.MVVM.Track;
using MiniTimeline.UI.MVVM.Clip;
using MiniTimeline.UI.MVVM.Ruler;

namespace MiniTimeline.UI.MVVM {
    /// <summary>
    /// Bootstrap controller that composes MVVM timeline UI without modifying legacy classes.
    /// Instantiates and wires up Timeline Editor, Track, Clip, and Ruler components.
    /// </summary>
    public class MiniTimelineController : MonoBehaviour {
        [SerializeField] TimelineEditorView _editorView;
        [SerializeField] TimelineEditorModel _editorModel;

        TimelineEditorController _editorController;
        TimelineRulerController _rulerController;

        void Awake() {
            if (_editorView == null) _editorView = GetComponentInChildren<TimelineEditorView>();
            if (_editorModel == null) _editorModel = new TimelineEditorModel();
        }

        void Start() {
            StartCoroutine(Initialize());
        }

        IEnumerator Initialize() {
            // Build editor controller
            _editorController = new TimelineEditorController.Builder(_editorView)
                .WithModel(_editorModel)
                .Build();
            yield return null;
        }

        /// <summary>
        /// Factory method to instantiate a track MVVM component.
        /// </summary>
        public TrackController CreateTrack(GameObject go, TrackModel model = null) {
            var view = go.GetComponent<TrackView>() ?? go.AddComponent<TrackView>();
            return new TrackController.Builder(view)
                .WithModel(model ?? new TrackModel())
                .Build();
        }

        /// <summary>
        /// Factory method to instantiate a clip MVVM component.
        /// </summary>
        public ClipController CreateClip(GameObject go, ClipModel model = null) {
            var view = go.GetComponent<ClipView>() ?? go.AddComponent<ClipView>();
            return new ClipController.Builder(view)
                .WithModel(model ?? new ClipModel())
                .Build();
        }

        /// <summary>
        /// Factory method to instantiate a ruler MVVM component.
        /// </summary>
        public TimelineRulerController CreateRuler(GameObject go, TimelineRulerModel model = null) {
            var view = go.GetComponent<TimelineRulerView>() ?? go.AddComponent<TimelineRulerView>();
            return new TimelineRulerController.Builder(view)
                .WithModel(model ?? new TimelineRulerModel())
                .Build();
        }
    }
}
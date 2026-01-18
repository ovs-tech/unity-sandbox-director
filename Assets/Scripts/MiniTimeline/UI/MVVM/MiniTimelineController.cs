using System.Collections;
using UnityEngine;
using Systems.MiniTimeline.UI.MVVM.Timeline;
using Systems.MiniTimeline.UI.MVVM.Track;
using Systems.MiniTimeline.UI.MVVM.Clip;
using Systems.MiniTimeline.UI.MVVM.Ruler;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.UI.MVVM
{
    /// <summary>
    /// Bootstrap controller that composes MVVM timeline UI without modifying legacy classes.
    /// Instantiates and wires up Timeline Editor, Track, Clip, and Ruler components.
    /// </summary>
    public class MiniTimelineController : MonoBehaviour
    {
        [SerializeField] TimelineEditorView _editorView;
        [SerializeField] TimelineEditorModel _editorModel;

        [SerializeField] MiniTimelineDirector _director;

        TimelineEditorController _editorController;
        TimelineRulerController _rulerController;

        void Awake()
        {
            if (_editorView == null) _editorView = GetComponentInChildren<TimelineEditorView>();
            if (_editorModel == null) _editorModel = new TimelineEditorModel();
        }

        void Start()
        {
            StartCoroutine(Initialize());
        }

        IEnumerator Initialize()
        {
            // Build editor controller
            _editorController = new TimelineEditorController.Builder(_editorView)
                .WithModel(_editorModel)
                .WithDirector(_director)
                .Build();

            // Build ruler controller
            // _rulerController = CreateRuler(_editorView.gameObject, new TimelineRulerModel());

            yield return null;
        }
    }
}
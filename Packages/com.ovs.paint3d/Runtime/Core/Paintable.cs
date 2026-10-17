using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Paint3D
{
    public enum PaintSaveMode
    {
        /// <summary>Painting is never persisted.</summary>
        None = 0,

        /// <summary>Call <see cref="Paintable.Save"/> / <see cref="Paintable.Load"/> yourself.</summary>
        Manual = 1,

        /// <summary>Loads on activation and saves when disabled, paused, quitting or on an interval.</summary>
        Automatic = 2
    }

    /// <summary>
    /// Makes a <see cref="MeshRenderer"/> or <see cref="SkinnedMeshRenderer"/> paintable.
    /// Works in play mode and in edit mode (activated by the Paint3D editor window).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    [AddComponentMenu("Paint3D/Paintable")]
    public class Paintable : MonoBehaviour
    {
        private static readonly List<Paintable> _activePaintables = new List<Paintable>();

        #region Serialized Fields

        [Header("Textures")]
        [Tooltip("Texture slots to paint. Add several entries for multi-texture or multi-material painting.")]
        [SerializeField] private List<PaintableTexture> _textures = new List<PaintableTexture> { new PaintableTexture() };

        [Header("Activation")]
        [Tooltip("Create the paint textures automatically when entering play mode.")]
        [SerializeField] private bool _activateOnStart = true;

        [Header("Quality")]
        [Tooltip("Bleed paint across UV island borders after every dab so seams stay invisible.")]
        [SerializeField] private bool _seamless = true;

        [Header("Mirroring")]
        [Tooltip("Space used by smart mirroring. Defaults to this transform.")]
        [SerializeField] private Transform _mirrorSpace;

        [Header("Save & Load")]
        [Tooltip("How painting is persisted between sessions.")]
        [SerializeField] private PaintSaveMode _saveMode = PaintSaveMode.None;

        [Tooltip("Unique storage key. Leave empty to derive one from the scene and hierarchy path.")]
        [SerializeField] private string _saveKey = string.Empty;

        [Tooltip("Automatic mode only: also save every N seconds when modified (0 = disabled).")]
        [SerializeField, Min(0f)] private float _autoSaveInterval;

        #endregion

        private Renderer _renderer;
        private MeshFilter _meshFilter;
        private SkinnedMeshRenderer _skinnedRenderer;
        private Mesh _bakedMesh;
        private int _bakedFrame = -1;
        private MaterialPropertyBlock _propertyBlock;
        private bool _activated;
        private bool _dirty;
        private float _nextAutoSaveTime;

        private readonly List<Vector3> _rayVertices = new List<Vector3>();
        private readonly List<int> _rayTriangles = new List<int>();
        private readonly List<int> _subMeshTriangles = new List<int>();
        private Mesh _rayTriangleSource;
        private int _rayVertexFrame = -1;

        /// <summary>Raised after every paint, undo, redo, load or reset.</summary>
        public event Action<Paintable> Painted;

        /// <summary>All activated and enabled paintables.</summary>
        public static IReadOnlyList<Paintable> ActivePaintables => _activePaintables;

        public IReadOnlyList<PaintableTexture> Textures => _textures;
        public bool IsActivated => _activated;
        public bool IsDirty => _dirty;
        public bool IsSkinned => CachedSkinnedRenderer != null;
        public bool Seamless { get => _seamless; set => _seamless = value; }
        public PaintSaveMode SaveMode { get => _saveMode; set => _saveMode = value; }
        public Transform MirrorSpace => _mirrorSpace != null ? _mirrorSpace : transform;
        public Renderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<Renderer>());
        public Bounds WorldBounds => Renderer.bounds;

        private SkinnedMeshRenderer CachedSkinnedRenderer
        {
            get
            {
                if (_skinnedRenderer == null)
                {
                    _skinnedRenderer = Renderer as SkinnedMeshRenderer;
                }
                return _skinnedRenderer;
            }
        }

        public string SaveKey
        {
            get
            {
                if (!string.IsNullOrEmpty(_saveKey))
                {
                    return _saveKey;
                }

                string path = name;
                for (Transform t = transform.parent; t != null; t = t.parent)
                {
                    path = t.name + "/" + path;
                }
                Scene scene = gameObject.scene;
                return (scene.IsValid() ? scene.name : "NoScene") + "/" + path;
            }
            set => _saveKey = value;
        }

        #region Unity Lifecycle

        private void Start()
        {
            if (_activateOnStart)
            {
                Activate();
            }
        }

        private void OnEnable()
        {
            if (_activated && !_activePaintables.Contains(this))
            {
                _activePaintables.Add(this);
            }
        }

        private void OnDisable()
        {
            _activePaintables.Remove(this);
            if (Application.isPlaying && _saveMode == PaintSaveMode.Automatic && _dirty)
            {
                Save();
            }
        }

        private void OnDestroy()
        {
            Deactivate();
            if (_bakedMesh != null)
            {
                PaintTextureUtility.Destroy(_bakedMesh);
                _bakedMesh = null;
            }
        }

        private void Update()
        {
            if (_saveMode == PaintSaveMode.Automatic && _autoSaveInterval > 0f && _dirty && Time.unscaledTime >= _nextAutoSaveTime)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _saveMode == PaintSaveMode.Automatic && _dirty)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (_saveMode == PaintSaveMode.Automatic && _dirty)
            {
                Save();
            }
        }

        #endregion

        #region Activation

        /// <summary>Creates the GPU paint textures and starts displaying them. Safe to call repeatedly.</summary>
        public bool Activate()
        {
            if (_activated)
            {
                return true;
            }

            _meshFilter = GetComponent<MeshFilter>();
            Mesh mesh = GetPaintMesh(out _);
            if (mesh == null)
            {
                Debug.LogWarning($"[Paint3D] '{name}' has no mesh to paint.", this);
                return false;
            }

            Material[] materials = Renderer.sharedMaterials;
            bool anyAllocated = false;
            foreach (PaintableTexture slot in _textures)
            {
                if (slot != null && slot.Allocate(this, materials, mesh))
                {
                    anyAllocated = true;
                }
            }

            if (!anyAllocated)
            {
                return false;
            }

            _activated = true;
            if (isActiveAndEnabled && !_activePaintables.Contains(this))
            {
                _activePaintables.Add(this);
            }
            ApplyPropertyBlocks();

            if (Application.isPlaying && _saveMode == PaintSaveMode.Automatic)
            {
                Load();
            }
            return true;
        }

        /// <summary>Releases the paint textures and shows the original materials again. Unsaved paint is lost.</summary>
        public void Deactivate()
        {
            _activePaintables.Remove(this);
            if (!_activated)
            {
                return;
            }

            foreach (PaintableTexture slot in _textures)
            {
                if (slot == null)
                {
                    continue;
                }
                PaintHistory.Forget(slot);
                slot.Release();
            }

            if (Renderer != null)
            {
                int materialCount = Renderer.sharedMaterials.Length;
                for (int i = 0; i < materialCount; i++)
                {
                    Renderer.SetPropertyBlock(null, i);
                }
            }

            _activated = false;
            _dirty = false;
        }

        /// <summary>Pushes the current paint textures to the renderer (per-material property blocks).</summary>
        public void ApplyPropertyBlocks()
        {
            if (!_activated)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            int materialCount = Renderer.sharedMaterials.Length;
            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                bool hasSlot = false;
                foreach (PaintableTexture slot in _textures)
                {
                    if (slot == null || !slot.IsAllocated || slot.MaterialIndex != materialIndex)
                    {
                        continue;
                    }

                    if (!hasSlot)
                    {
                        Renderer.GetPropertyBlock(_propertyBlock, materialIndex);
                        hasSlot = true;
                    }
                    _propertyBlock.SetTexture(slot.PropertyId, slot.Current);
                }

                if (hasSlot)
                {
                    Renderer.SetPropertyBlock(_propertyBlock, materialIndex);
                }
            }
        }

        #endregion

        #region Mesh Access

        /// <summary>
        /// Returns the mesh to paint and its object-to-world matrix. Skinned meshes are baked in their current pose
        /// (at most once per frame in play mode), so animated characters can be painted.
        /// </summary>
        public Mesh GetPaintMesh(out Matrix4x4 localToWorld)
        {
            SkinnedMeshRenderer skinned = CachedSkinnedRenderer;
            if (skinned != null)
            {
                if (_bakedMesh == null)
                {
                    _bakedMesh = new Mesh { name = name + "_Paint3DBaked", hideFlags = HideFlags.HideAndDontSave };
                    _bakedFrame = -1;
                }

                if (!Application.isPlaying || _bakedFrame != Time.frameCount)
                {
                    skinned.BakeMesh(_bakedMesh, true);
                    _bakedFrame = Time.frameCount;
                }

                Transform t = skinned.transform;
                localToWorld = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
                return _bakedMesh;
            }

            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }
            localToWorld = transform.localToWorldMatrix;
            return _meshFilter != null ? _meshFilter.sharedMesh : null;
        }

        /// <summary>CPU geometry used by <see cref="PaintRaycaster"/> when no collider is available.</summary>
        internal bool TryGetRaycastGeometry(out List<Vector3> vertices, out List<int> triangles, out Matrix4x4 localToWorld)
        {
            vertices = _rayVertices;
            triangles = _rayTriangles;
            Mesh mesh = GetPaintMesh(out localToWorld);
            if (mesh == null || (!Application.isEditor && !IsSkinned && !mesh.isReadable))
            {
                return false;
            }

            bool refreshVertices = _rayTriangleSource != mesh
                || (IsSkinned && (!Application.isPlaying || _rayVertexFrame != Time.frameCount));
            if (refreshVertices)
            {
                mesh.GetVertices(_rayVertices);
                _rayVertexFrame = Time.frameCount;
            }

            if (_rayTriangleSource != mesh)
            {
                _rayTriangles.Clear();
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    mesh.GetTriangles(_subMeshTriangles, subMesh);
                    _rayTriangles.AddRange(_subMeshTriangles);
                }
                _rayTriangleSource = mesh;
            }
            return _rayVertices.Count > 0 && _rayTriangles.Count > 0;
        }

        #endregion

        #region Painting State

        /// <summary>Called by the engine and history after the paint changed.</summary>
        public void NotifyPainted()
        {
            if (!_dirty)
            {
                _nextAutoSaveTime = Time.unscaledTime + _autoSaveInterval;
            }
            _dirty = true;
            Painted?.Invoke(this);
        }

        /// <summary>Restores every slot to its original texture.</summary>
        public void ResetPainting()
        {
            foreach (PaintableTexture slot in _textures)
            {
                slot?.RestoreOriginal();
            }
            NotifyPainted();
        }

        /// <summary>Fills every slot of a group with a color.</summary>
        public void ClearPainting(Color color, int group = 0)
        {
            foreach (PaintableTexture slot in _textures)
            {
                if (slot != null && slot.Group == group)
                {
                    slot.Clear(color);
                }
            }
            NotifyPainted();
        }

        #endregion

        #region Save & Load

        public string GetSlotKey(PaintableTexture slot) => $"{SaveKey}_{slot.MaterialIndex}_{slot.PropertyName}";

        /// <summary>Saves every allocated slot through <see cref="PaintStorage"/>.</summary>
        public void Save()
        {
            if (!_activated)
            {
                return;
            }

            foreach (PaintableTexture slot in _textures)
            {
                if (slot != null && slot.IsAllocated)
                {
                    PaintStorage.Save(slot, GetSlotKey(slot));
                }
            }
            _dirty = false;
        }

        /// <summary>Loads previously saved paint. Returns true when at least one slot was restored.</summary>
        public bool Load()
        {
            if (!_activated)
            {
                return false;
            }

            bool loaded = false;
            foreach (PaintableTexture slot in _textures)
            {
                if (slot != null && slot.IsAllocated && PaintStorage.Load(slot, GetSlotKey(slot)))
                {
                    loaded = true;
                }
            }

            if (loaded)
            {
                Painted?.Invoke(this);
            }
            _dirty = false;
            return loaded;
        }

        /// <summary>Deletes saved paint for this paintable.</summary>
        public void ClearSavedData()
        {
            foreach (PaintableTexture slot in _textures)
            {
                if (slot != null)
                {
                    PaintStorage.Delete(GetSlotKey(slot));
                }
            }
        }

        #endregion
    }
}

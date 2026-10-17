using System;
using UnityEngine;

namespace Paint3D
{
    public enum PaintUVChannel
    {
        UV0 = 0,
        UV1 = 1
    }

    /// <summary>
    /// One paintable texture slot: a texture property of one material of a <see cref="Paintable"/>.
    /// Any shader can be painted by naming its texture property, and a paintable can own several slots
    /// (multi-texture, multi-material). At runtime the slot owns a GPU render texture that replaces the
    /// original texture through a material property block, so shared materials are never modified.
    /// </summary>
    [Serializable]
    public class PaintableTexture
    {
        private static readonly string[] DefaultProperties = { "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo" };

        [Tooltip("Index of the material (and sub-mesh) on the renderer.")]
        [SerializeField, Min(0)] private int _materialIndex;

        [Tooltip("Texture property to paint, e.g. _BaseMap (URP), _MainTex (Built-in), _EmissionMap, or any custom property.")]
        [SerializeField] private string _propertyName = "_BaseMap";

        [Tooltip("Brushes only paint slots whose group matches the brush group.")]
        [SerializeField] private int _group;

        [Tooltip("Mesh UV channel used to map this texture.")]
        [SerializeField] private PaintUVChannel _uvChannel = PaintUVChannel.UV0;

        [Tooltip("Paint texture resolution. Zero uses the original texture size (or 1024 when there is none).")]
        [SerializeField] private Vector2Int _resolution = Vector2Int.zero;

        [Tooltip("Color data (sRGB). Disable for masks and other linear data.")]
        [SerializeField] private bool _isColor = true;

        [Tooltip("Fill color used when the material has no texture in this slot.")]
        [SerializeField] private Color _clearColor = Color.white;

        [Tooltip("Optional texture to start from instead of the material texture.")]
        [SerializeField] private Texture _startTexture;

        [NonSerialized] private RenderTexture _current;
        [NonSerialized] private RenderTexture _temp;
        [NonSerialized] private RenderTexture _uvMask;
        [NonSerialized] private Texture _original;
        [NonSerialized] private Paintable _owner;
        [NonSerialized] private string _resolvedProperty;
        [NonSerialized] private int _propertyId;
        [NonSerialized] private int _version;

        public PaintableTexture() { }

        public PaintableTexture(int materialIndex, string propertyName, int group = 0, bool isColor = true)
        {
            _materialIndex = materialIndex;
            _propertyName = propertyName;
            _group = group;
            _isColor = isColor;
        }

        public int MaterialIndex { get => _materialIndex; set => _materialIndex = Mathf.Max(0, value); }
        public string PropertyName { get => _propertyName; set => _propertyName = value; }
        public int Group { get => _group; set => _group = value; }
        public PaintUVChannel UVChannel { get => _uvChannel; set => _uvChannel = value; }
        public Vector2Int Resolution { get => _resolution; set => _resolution = value; }
        public bool IsColor { get => _isColor; set => _isColor = value; }
        public Color ClearColor { get => _clearColor; set => _clearColor = value; }
        public Texture StartTexture { get => _startTexture; set => _startTexture = value; }

        /// <summary>Live GPU paint texture. Changes identity when seams are fixed, so do not cache it across paints.</summary>
        public RenderTexture Current => _current;

        /// <summary>White where the mesh covers the texture (UV islands), black elsewhere.</summary>
        public RenderTexture UVMask => _uvMask;

        /// <summary>Texture the slot started from (null when filled with <see cref="ClearColor"/>).</summary>
        public Texture Original => _original;

        public Paintable Owner => _owner;
        public bool IsAllocated => _current != null;
        public string ResolvedPropertyName => _resolvedProperty;
        public int PropertyId => _propertyId;

        /// <summary>Incremented every time the texture content changes.</summary>
        public int Version => _version;

        internal bool Allocate(Paintable owner, Material[] materials, Mesh mesh)
        {
            if (IsAllocated)
            {
                return true;
            }

            if (_materialIndex >= materials.Length || materials[_materialIndex] == null)
            {
                Debug.LogWarning($"[Paint3D] '{owner.name}' has no material at index {_materialIndex}.", owner);
                return false;
            }

            Material material = materials[_materialIndex];
            _resolvedProperty = ResolveProperty(material, _propertyName);
            if (_resolvedProperty == null)
            {
                Debug.LogWarning($"[Paint3D] Material '{material.name}' on '{owner.name}' has no texture property '{_propertyName}'.", owner);
                return false;
            }

            _owner = owner;
            _propertyId = Shader.PropertyToID(_resolvedProperty);
            _original = _startTexture != null ? _startTexture : material.GetTexture(_propertyId);

            int width = _resolution.x > 0 ? _resolution.x : (_original != null ? _original.width : 1024);
            int height = _resolution.y > 0 ? _resolution.y : (_original != null ? _original.height : 1024);
            width = Mathf.Clamp(width, 4, SystemInfo.maxTextureSize);
            height = Mathf.Clamp(height, 4, SystemInfo.maxTextureSize);

            string label = $"Paint3D_{owner.name}_{_resolvedProperty}";
            _current = PaintTextureUtility.Create(width, height, _isColor, label);
            if (_original != null)
            {
                _current.wrapMode = _original.wrapMode;
                _current.filterMode = _original.filterMode == FilterMode.Point ? FilterMode.Point : FilterMode.Bilinear;
            }
            _temp = PaintTextureUtility.CreateLike(_current, label + "_Temp");
            _uvMask = PaintEngine.RenderUVMask(mesh, GetSubMesh(mesh), _uvChannel, width, height);

            RestoreOriginal();
            return true;
        }

        internal void Release()
        {
            PaintTextureUtility.Destroy(_current);
            PaintTextureUtility.Destroy(_temp);
            PaintTextureUtility.Destroy(_uvMask);
            _current = null;
            _temp = null;
            _uvMask = null;
            _original = null;
            _owner = null;
        }

        internal RenderTexture Temp => _temp;

        /// <summary>Makes the temp buffer the current one (used after a full screen post pass).</summary>
        internal void SwapBuffers()
        {
            (_current, _temp) = (_temp, _current);
        }

        internal void MarkModified()
        {
            _version++;
        }

        internal int GetSubMesh(Mesh mesh) =>
            mesh == null || mesh.subMeshCount == 0 ? 0 : Mathf.Min(_materialIndex, mesh.subMeshCount - 1);

        /// <summary>Resets the texture to its original content.</summary>
        public void RestoreOriginal()
        {
            if (!IsAllocated)
            {
                return;
            }

            if (_original != null)
            {
                Graphics.Blit(_original, _current);
            }
            else
            {
                PaintTextureUtility.Fill(_current, _clearColor);
            }
            MarkModified();
        }

        /// <summary>Fills the whole texture with a color.</summary>
        public void Clear(Color color)
        {
            if (!IsAllocated)
            {
                return;
            }

            PaintTextureUtility.Fill(_current, color);
            MarkModified();
        }

        /// <summary>Replaces the paint with the content of another texture (scaled to fit).</summary>
        public void LoadFrom(Texture source)
        {
            if (!IsAllocated || source == null)
            {
                return;
            }

            Graphics.Blit(source, _current);
            MarkModified();
        }

        /// <summary>Copies the current paint into a new CPU texture. The caller owns (and must destroy) it.</summary>
        public Texture2D ReadToTexture2D() => IsAllocated ? PaintTextureUtility.ReadToTexture2D(_current) : null;

        private static string ResolveProperty(Material material, string preferred)
        {
            if (!string.IsNullOrEmpty(preferred) && material.HasTexture(preferred))
            {
                return preferred;
            }

            bool fallbackAllowed = string.IsNullOrEmpty(preferred) || Array.IndexOf(DefaultProperties, preferred) >= 0;
            if (!fallbackAllowed)
            {
                return null;
            }

            foreach (string candidate in DefaultProperties)
            {
                if (material.HasTexture(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }
    }
}

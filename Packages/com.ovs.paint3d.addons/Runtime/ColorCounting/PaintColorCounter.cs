using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Paint3D.Addons
{
    /// <summary>
    /// Color counting: measures how much of a paintable texture is covered by each palette color (team coverage in
    /// splat games, "paint 80% of the wall" objectives...). The texture is downsampled on the GPU and read back
    /// asynchronously when supported (synchronously on WebGL / GLES without async readback).
    /// </summary>
    [RequireComponent(typeof(Paintable))]
    [AddComponentMenu("Paint3D/Addons/Paint Color Counter")]
    public class PaintColorCounter : MonoBehaviour
    {
        [Serializable]
        public class ColorEntry
        {
            [Tooltip("Display name, e.g. a team name.")]
            public string Name = "Color";

            [Tooltip("Color to count.")]
            public Color Color = Color.red;
        }

        private const string PREPARE_SHADER = "Hidden/Paint3D/CountPrepare";
        private static readonly int MaskId = Shader.PropertyToID("_Paint3DMask");

        [Header("Palette")]
        [SerializeField] private List<ColorEntry> _colors = new List<ColorEntry>
        {
            new ColorEntry { Name = "Red", Color = Color.red },
            new ColorEntry { Name = "Blue", Color = Color.blue }
        };

        [Tooltip("Max RGB distance (0..1.73) for a texel to match a palette color. Larger = everything matches the nearest color.")]
        [SerializeField, Range(0f, 1.75f)] private float _tolerance = 0.5f;

        [Header("Sampling")]
        [Tooltip("Index into the paintable's texture slots.")]
        [SerializeField, Min(0)] private int _textureIndex;

        [Tooltip("Readback resolution (square). Higher = more precise, slower.")]
        [SerializeField, Range(16, 512)] private int _sampleResolution = 128;

        [Tooltip("Minimum seconds between counts.")]
        [SerializeField, Min(0f)] private float _interval = 0.25f;

        private Paintable _paintable;
        private Material _prepareMaterial;
        private RenderTexture _sampleTexture;
        private float[] _ratios = Array.Empty<float>();
        private Color32[] _pixels;
        private readonly List<Color> _palette = new List<Color>();
        private int _countedVersion = -1;
        private float _nextCountTime;
        private bool _requestPending;

        /// <summary>Raised after new ratios were computed.</summary>
        public event Action<PaintColorCounter> Counted;

        public IReadOnlyList<ColorEntry> Colors => _colors;

        /// <summary>Fraction (0..1) of covered texels per palette entry; same order as <see cref="Colors"/>.</summary>
        public IReadOnlyList<float> Ratios => _ratios;

        /// <summary>Number of covered texels in the last count.</summary>
        public int CoveredTexels { get; private set; }

        public float GetRatio(int index) => index >= 0 && index < _ratios.Length ? _ratios[index] : 0f;

        public float GetRatio(string colorName)
        {
            for (int i = 0; i < _colors.Count; i++)
            {
                if (_colors[i].Name == colorName)
                {
                    return GetRatio(i);
                }
            }
            return 0f;
        }

        private void Awake()
        {
            _paintable = GetComponent<Paintable>();
        }

        private void OnDestroy()
        {
            PaintTextureUtility.Destroy(_sampleTexture);
            PaintTextureUtility.Destroy(_prepareMaterial);
        }

        private void Update()
        {
            if (_requestPending || Time.unscaledTime < _nextCountTime || !TryGetSlot(out PaintableTexture slot) || slot.Version == _countedVersion)
            {
                return;
            }

            CountNow(slot);
        }

        /// <summary>Forces a count of the current paint (result arrives via <see cref="Counted"/>).</summary>
        public void RequestCount()
        {
            if (!_requestPending && TryGetSlot(out PaintableTexture slot))
            {
                CountNow(slot);
            }
        }

        private bool TryGetSlot(out PaintableTexture slot)
        {
            slot = null;
            if (_paintable == null || !_paintable.IsActivated || _textureIndex >= _paintable.Textures.Count)
            {
                return false;
            }

            slot = _paintable.Textures[_textureIndex];
            return slot != null && slot.IsAllocated;
        }

        private void CountNow(PaintableTexture slot)
        {
            if (_prepareMaterial == null)
            {
                Shader shader = PaintShaders.Find(PREPARE_SHADER);
                if (shader == null)
                {
                    return;
                }
                _prepareMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (_sampleTexture == null || _sampleTexture.width != _sampleResolution || _sampleTexture.sRGB != slot.Current.sRGB)
            {
                PaintTextureUtility.Destroy(_sampleTexture);
                _sampleTexture = PaintTextureUtility.Create(_sampleResolution, _sampleResolution, slot.Current.sRGB, "Paint3D_ColorCount");
                _sampleTexture.filterMode = FilterMode.Point;
            }

            // Writes rgb = paint, a = UV coverage so only real surface texels are counted.
            _prepareMaterial.SetTexture(MaskId, slot.UVMask);
            Graphics.Blit(slot.Current, _sampleTexture, _prepareMaterial);

            _countedVersion = slot.Version;
            _nextCountTime = Time.unscaledTime + _interval;

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                _requestPending = true;
                AsyncGPUReadback.Request(_sampleTexture, 0, TextureFormat.RGBA32, OnReadback);
            }
            else
            {
                Texture2D readback = PaintTextureUtility.ReadToTexture2D(_sampleTexture);
                Process(readback.GetPixels32());
                PaintTextureUtility.Destroy(readback);
            }
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            _requestPending = false;
            if (this == null || request.hasError)
            {
                return;
            }

            NativeArray<Color32> data = request.GetData<Color32>();
            if (_pixels == null || _pixels.Length != data.Length)
            {
                _pixels = new Color32[data.Length];
            }
            data.CopyTo(_pixels);
            Process(_pixels);
        }

        private void Process(Color32[] pixels)
        {
            if (_ratios.Length != _colors.Count)
            {
                _ratios = new float[_colors.Count];
            }

            _palette.Clear();
            foreach (ColorEntry entry in _colors)
            {
                _palette.Add(entry.Color);
            }

            CoveredTexels = ColorCounting.Count(pixels, _palette, _ratios, _tolerance);
            Counted?.Invoke(this);
        }
    }
}

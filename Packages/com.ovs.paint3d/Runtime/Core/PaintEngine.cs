using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using BlendFactor = UnityEngine.Rendering.BlendMode;

namespace Paint3D
{
    /// <summary>
    /// GPU paint pipeline. Each dab renders the target mesh in texture (UV) space with a brush shader that
    /// evaluates the brush against the world position of every texel, then optionally runs a dilation pass
    /// so paint bleeds across UV seams. Uses only vertex/fragment shaders, so it runs on desktop, mobile and WebGL.
    /// </summary>
    public static class PaintEngine
    {
        private static CommandBuffer _commandBuffer;
        private static MaterialPropertyBlock _properties;
        private static Material _dilateMaterial;
        private static Material _uvMaskMaterial;
        private static readonly Dictionary<(Shader, PaintBlendMode), Material> _materials = new Dictionary<(Shader, PaintBlendMode), Material>();
        private static readonly List<Matrix4x4> _reflections = new List<Matrix4x4>();
        private static readonly List<PaintOperation> _operations = new List<PaintOperation>();
        private static readonly List<Paintable> _candidates = new List<Paintable>();

        /// <summary>Raised after a paintable received paint from the engine.</summary>
        public static event Action<Paintable> Painted;

        #region Public API

        /// <summary>Paints one paintable (activating it first in play mode if needed).</summary>
        /// <returns>True when at least one texel draw was issued.</returns>
        public static bool Paint(Paintable target, PaintOperation operation, MirrorSettings mirror = null)
        {
            if (target == null || operation == null)
            {
                return false;
            }

            if (!target.IsActivated && (!Application.isPlaying || !target.Activate()))
            {
                return false;
            }

            BuildOperations(operation, mirror, target.MirrorSpace);
            return PaintOperations(target, _operations);
        }

        /// <summary>
        /// Paints every active paintable overlapping the operation (through painting across objects, collision
        /// painting, dynamic painting...). Returns the number of paintables that received paint.
        /// </summary>
        /// <param name="mirrorSpace">Mirror space shared by all targets; null mirrors each target in its own space.</param>
        public static int PaintAll(PaintOperation operation, MirrorSettings mirror = null, int layerMask = ~0, Transform mirrorSpace = null)
        {
            return PaintAll(Paintable.ActivePaintables, operation, mirror, layerMask, mirrorSpace);
        }

        /// <inheritdoc cref="PaintAll(PaintOperation, MirrorSettings, int, Transform)"/>
        public static int PaintAll(IReadOnlyList<Paintable> candidates, PaintOperation operation, MirrorSettings mirror = null, int layerMask = ~0, Transform mirrorSpace = null)
        {
            if (operation == null || candidates == null)
            {
                return 0;
            }

            // Copy: painting can raise events that enable/disable paintables.
            _candidates.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                _candidates.Add(candidates[i]);
            }

            bool sharedSpace = mirrorSpace != null || mirror == null || !mirror.Enabled || mirror.Pivot != null;
            if (sharedSpace)
            {
                BuildOperations(operation, mirror, mirrorSpace);
            }

            int painted = 0;
            foreach (Paintable target in _candidates)
            {
                if (target == null || !target.IsActivated || (layerMask & (1 << target.gameObject.layer)) == 0)
                {
                    continue;
                }

                if (!sharedSpace)
                {
                    BuildOperations(operation, mirror, target.MirrorSpace);
                }

                if (PaintOperations(target, _operations))
                {
                    painted++;
                }
            }
            return painted;
        }

        #endregion

        #region Pipeline

        private static void BuildOperations(PaintOperation operation, MirrorSettings mirror, Transform space)
        {
            _operations.Clear();
            _operations.Add(operation);

            if (mirror == null || !mirror.Enabled)
            {
                return;
            }

            mirror.GetReflections(space, _reflections);
            foreach (Matrix4x4 reflection in _reflections)
            {
                _operations.Add(operation.Reflect(reflection));
            }
        }

        private static bool PaintOperations(Paintable target, List<PaintOperation> operations)
        {
            Bounds targetBounds = target.WorldBounds;
            bool anyOverlap = false;
            foreach (PaintOperation op in operations)
            {
                if (targetBounds.Intersects(op.WorldBounds))
                {
                    anyOverlap = true;
                    break;
                }
            }
            if (!anyOverlap)
            {
                return false;
            }

            Mesh mesh = target.GetPaintMesh(out Matrix4x4 localToWorld);
            if (mesh == null)
            {
                return false;
            }

            EnsureResources();
            Matrix4x4 normalMatrix = localToWorld.inverse.transpose;
            bool painted = false;

            foreach (PaintableTexture slot in target.Textures)
            {
                if (slot == null || !slot.IsAllocated)
                {
                    continue;
                }

                bool slotPainted = false;
                foreach (PaintOperation op in operations)
                {
                    if (op.Brush.Group != slot.Group || !targetBounds.Intersects(op.WorldBounds))
                    {
                        continue;
                    }

                    Material material = GetMaterial(op.Shader, op.Brush.BlendMode);
                    if (material == null)
                    {
                        continue;
                    }

                    if (!slotPainted)
                    {
                        PaintHistory.RecordBeforePaint(slot);
                        slotPainted = true;
                    }

                    _properties.Clear();
                    _properties.SetMatrix(PaintShaders.Matrix, localToWorld);
                    _properties.SetMatrix(PaintShaders.NormalMatrix, normalMatrix);
                    _properties.SetFloat(PaintShaders.UVChannel, (float)slot.UVChannel);
                    _properties.SetFloat(PaintShaders.Mode, (float)op.Brush.BlendMode);
                    _properties.SetTexture(PaintShaders.Original, slot.Original != null ? slot.Original : Texture2D.whiteTexture);
                    op.SetProperties(_properties);

                    _commandBuffer.SetRenderTarget(slot.Current);
                    _commandBuffer.DrawMesh(mesh, Matrix4x4.identity, material, slot.GetSubMesh(mesh), 0, _properties);
                    Graphics.ExecuteCommandBuffer(_commandBuffer);
                    _commandBuffer.Clear();
                }

                if (!slotPainted)
                {
                    continue;
                }

                if (target.Seamless && slot.UVMask != null)
                {
                    Dilate(slot);
                }

                slot.MarkModified();
                painted = true;
            }

            if (painted)
            {
                target.ApplyPropertyBlocks();
                target.NotifyPainted();
                Painted?.Invoke(target);
            }
            return painted;
        }

        /// <summary>Bleeds island border texels outward (seamless UV painting).</summary>
        private static void Dilate(PaintableTexture slot)
        {
            _commandBuffer.SetGlobalTexture(PaintShaders.Mask, slot.UVMask);
            _commandBuffer.Blit(slot.Current, slot.Temp, _dilateMaterial);
            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
            slot.SwapBuffers();
        }

        /// <summary>Renders a coverage mask of the mesh's UV islands (white inside, black outside).</summary>
        public static RenderTexture RenderUVMask(Mesh mesh, int subMesh, PaintUVChannel channel, int width, int height)
        {
            EnsureResources();
            RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;
            RenderTexture mask = PaintTextureUtility.Create(width, height, false, "Paint3D_UVMask", format);
            mask.filterMode = FilterMode.Point;
            mask.wrapMode = TextureWrapMode.Clamp;

            _properties.Clear();
            _properties.SetFloat(PaintShaders.UVChannel, (float)channel);
            _commandBuffer.SetRenderTarget(mask);
            _commandBuffer.ClearRenderTarget(false, true, Color.clear);
            if (mesh != null && _uvMaskMaterial != null)
            {
                _commandBuffer.DrawMesh(mesh, Matrix4x4.identity, _uvMaskMaterial, subMesh, 0, _properties);
            }
            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
            return mask;
        }

        #endregion

        #region Resources

        private static void EnsureResources()
        {
            _commandBuffer ??= new CommandBuffer { name = "Paint3D" };
            _properties ??= new MaterialPropertyBlock();

            if (_dilateMaterial == null)
            {
                Shader dilate = PaintShaders.Find(PaintShaders.DILATE);
                if (dilate != null)
                {
                    _dilateMaterial = new Material(dilate) { hideFlags = HideFlags.HideAndDontSave };
                }
            }

            if (_uvMaskMaterial == null)
            {
                Shader uvMask = PaintShaders.Find(PaintShaders.UV_MASK);
                if (uvMask != null)
                {
                    _uvMaskMaterial = new Material(uvMask) { hideFlags = HideFlags.HideAndDontSave };
                }
            }
        }

        /// <summary>Returns a cached material for a shader + blend mode (blend state cannot live in property blocks).</summary>
        public static Material GetMaterial(Shader shader, PaintBlendMode blendMode)
        {
            if (shader == null)
            {
                return null;
            }

            if (_materials.TryGetValue((shader, blendMode), out Material material) && material != null)
            {
                return material;
            }

            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, name = $"{shader.name} ({blendMode})" };
            ConfigureBlend(material, blendMode);
            _materials[(shader, blendMode)] = material;
            return material;
        }

        private static void ConfigureBlend(Material material, PaintBlendMode blendMode)
        {
            BlendFactor src = BlendFactor.SrcAlpha;
            BlendFactor dst = BlendFactor.OneMinusSrcAlpha;
            BlendFactor srcAlpha = BlendFactor.One;
            BlendFactor dstAlpha = BlendFactor.OneMinusSrcAlpha;
            BlendOp op = BlendOp.Add;

            switch (blendMode)
            {
                case PaintBlendMode.Additive:
                    dst = BlendFactor.One;
                    srcAlpha = BlendFactor.Zero;
                    dstAlpha = BlendFactor.One;
                    break;
                case PaintBlendMode.Subtract:
                    dst = BlendFactor.One;
                    srcAlpha = BlendFactor.Zero;
                    dstAlpha = BlendFactor.One;
                    op = BlendOp.ReverseSubtract;
                    break;
                case PaintBlendMode.Multiply:
                    src = BlendFactor.DstColor;
                    dst = BlendFactor.Zero;
                    srcAlpha = BlendFactor.Zero;
                    dstAlpha = BlendFactor.One;
                    break;
            }

            material.SetFloat(PaintShaders.SrcBlend, (float)src);
            material.SetFloat(PaintShaders.DstBlend, (float)dst);
            material.SetFloat(PaintShaders.SrcBlendAlpha, (float)srcAlpha);
            material.SetFloat(PaintShaders.DstBlendAlpha, (float)dstAlpha);
            material.SetFloat(PaintShaders.BlendOp, (float)op);
        }

        #endregion
    }
}

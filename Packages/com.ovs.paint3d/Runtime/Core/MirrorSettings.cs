using System;
using System.Collections.Generic;
using UnityEngine;

namespace Paint3D
{
    [Flags]
    public enum MirrorAxes
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }

    /// <summary>
    /// Smart mirroring: dabs are reflected across the selected local axes of a pivot. When no pivot is assigned,
    /// each paintable is mirrored in its own local space (<see cref="Paintable.MirrorSpace"/>), so symmetric models
    /// stay symmetric no matter how they are placed or rotated in the scene.
    /// </summary>
    [Serializable]
    public class MirrorSettings
    {
        [Tooltip("Local axes to mirror across. Combining axes also paints the diagonal reflections.")]
        [SerializeField] private MirrorAxes _axes = MirrorAxes.None;

        [Tooltip("Optional shared mirror pivot. Leave empty to mirror in each paintable's own space.")]
        [SerializeField] private Transform _pivot;

        public MirrorSettings() { }

        public MirrorSettings(MirrorAxes axes, Transform pivot = null)
        {
            _axes = axes;
            _pivot = pivot;
        }

        public MirrorAxes Axes { get => _axes; set => _axes = value; }
        public Transform Pivot { get => _pivot; set => _pivot = value; }
        public bool Enabled => _axes != MirrorAxes.None;

        /// <summary>Fills <paramref name="results"/> with reflection matrices (identity excluded).</summary>
        public void GetReflections(Transform defaultSpace, List<Matrix4x4> results)
        {
            results.Clear();
            if (_axes == MirrorAxes.None)
            {
                return;
            }

            Transform space = _pivot != null ? _pivot : defaultSpace;
            if (space == null)
            {
                return;
            }

            AppendReflections(space.localToWorldMatrix, _axes, results);
        }

        /// <summary>Appends every non-identity combination of reflections across the given local axes.</summary>
        public static void AppendReflections(Matrix4x4 localToWorld, MirrorAxes axes, List<Matrix4x4> results)
        {
            if (axes == MirrorAxes.None)
            {
                return;
            }

            Matrix4x4 worldToLocal = localToWorld.inverse;
            for (int combo = 1; combo < 8; combo++)
            {
                if ((combo & ~(int)axes) != 0)
                {
                    continue;
                }

                var scale = new Vector3(
                    (combo & (int)MirrorAxes.X) != 0 ? -1f : 1f,
                    (combo & (int)MirrorAxes.Y) != 0 ? -1f : 1f,
                    (combo & (int)MirrorAxes.Z) != 0 ? -1f : 1f);
                results.Add(localToWorld * Matrix4x4.Scale(scale) * worldToLocal);
            }
        }
    }
}

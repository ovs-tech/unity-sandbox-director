using System.Collections.Generic;
using UnityEngine;

namespace Paint3D
{
    public struct PaintHit
    {
        public Paintable Paintable;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
    }

    /// <summary>
    /// Finds the paintable surface under a ray. Uses physics colliders when present (and respects non-paintable
    /// occluders), and falls back to an exact triangle test on the paint mesh, which works without colliders,
    /// in edit mode and on posed skinned meshes. Triangle tests need Read/Write enabled meshes in players.
    /// </summary>
    public static class PaintRaycaster
    {
        public static bool Raycast(Ray ray, out PaintHit hit, float maxDistance = Mathf.Infinity, int layerMask = ~0, bool usePhysics = true)
        {
            return Raycast(ray, Paintable.ActivePaintables, out hit, maxDistance, layerMask, usePhysics);
        }

        public static bool Raycast(Ray ray, IReadOnlyList<Paintable> candidates, out PaintHit hit, float maxDistance = Mathf.Infinity, int layerMask = ~0, bool usePhysics = true)
        {
            hit = default;
            hit.Distance = maxDistance;
            bool found = false;
            float occluderDistance = maxDistance;

            if (usePhysics && Physics.Raycast(ray, out RaycastHit physicsHit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                Paintable paintable = physicsHit.collider.GetComponentInParent<Paintable>();
                if (paintable != null && paintable.IsActivated)
                {
                    hit = new PaintHit
                    {
                        Paintable = paintable,
                        Point = physicsHit.point,
                        Normal = physicsHit.normal,
                        Distance = physicsHit.distance
                    };
                    found = true;
                }
                else
                {
                    occluderDistance = physicsHit.distance;
                }
            }

            if (candidates == null)
            {
                return found;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                Paintable paintable = candidates[i];
                if (paintable == null || !paintable.IsActivated || (layerMask & (1 << paintable.gameObject.layer)) == 0)
                {
                    continue;
                }

                // A mesh collider on the paintable is already exact.
                bool physicsHitThis = found && hit.Paintable == paintable;
                if (physicsHitThis && paintable.TryGetComponent(out MeshCollider _))
                {
                    continue;
                }

                if (!paintable.WorldBounds.IntersectRay(ray, out float boundsDistance) || (!physicsHitThis && boundsDistance > hit.Distance))
                {
                    continue;
                }

                // Primitive colliders (e.g. capsules on characters) only approximate the surface, so an exact mesh hit
                // on the same paintable replaces the physics hit.
                if (RaycastMesh(ray, paintable, out PaintHit meshHit)
                    && (physicsHitThis || meshHit.Distance < hit.Distance)
                    && meshHit.Distance <= occluderDistance)
                {
                    hit = meshHit;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>Exact ray / triangle test against a paintable's paint mesh.</summary>
        public static bool RaycastMesh(Ray ray, Paintable paintable, out PaintHit hit)
        {
            hit = default;
            if (!paintable.TryGetRaycastGeometry(out List<Vector3> vertices, out List<int> triangles, out Matrix4x4 localToWorld))
            {
                return false;
            }

            Matrix4x4 worldToLocal = localToWorld.inverse;
            Vector3 origin = worldToLocal.MultiplyPoint3x4(ray.origin);
            Vector3 direction = worldToLocal.MultiplyVector(ray.direction);

            float bestT = float.MaxValue;
            int bestTriangle = -1;
            for (int i = 0; i + 2 < triangles.Count; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                if (IntersectTriangle(origin, direction, a, b, c, out float t) && t < bestT)
                {
                    bestT = t;
                    bestTriangle = i;
                }
            }

            if (bestTriangle < 0)
            {
                return false;
            }

            Vector3 va = vertices[triangles[bestTriangle]];
            Vector3 vb = vertices[triangles[bestTriangle + 1]];
            Vector3 vc = vertices[triangles[bestTriangle + 2]];
            Vector3 localPoint = origin + (direction * bestT);
            Vector3 localNormal = Vector3.Cross(vb - va, vc - va);

            Vector3 worldPoint = localToWorld.MultiplyPoint3x4(localPoint);
            Vector3 worldNormal = worldToLocal.transpose.MultiplyVector(localNormal).normalized;
            if (Vector3.Dot(worldNormal, ray.direction) > 0f)
            {
                worldNormal = -worldNormal;
            }

            hit = new PaintHit
            {
                Paintable = paintable,
                Point = worldPoint,
                Normal = worldNormal,
                Distance = Vector3.Distance(ray.origin, worldPoint)
            };
            return true;
        }

        /// <summary>Möller–Trumbore, two sided.</summary>
        public static bool IntersectTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            const float EPSILON = 1e-8f;
            t = 0f;
            Vector3 edge1 = b - a;
            Vector3 edge2 = c - a;
            Vector3 p = Vector3.Cross(direction, edge2);
            float det = Vector3.Dot(edge1, p);
            if (det > -EPSILON && det < EPSILON)
            {
                return false;
            }

            float invDet = 1f / det;
            Vector3 s = origin - a;
            float u = Vector3.Dot(s, p) * invDet;
            if (u < 0f || u > 1f)
            {
                return false;
            }

            Vector3 q = Vector3.Cross(s, edge1);
            float v = Vector3.Dot(direction, q) * invDet;
            if (v < 0f || u + v > 1f)
            {
                return false;
            }

            t = Vector3.Dot(edge2, q) * invDet;
            return t > EPSILON;
        }
    }
}

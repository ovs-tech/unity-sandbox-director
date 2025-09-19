using UnityEngine;
using UnityEditor;
using MiniTimeline.Core;
using MiniTimeline.Demo;

namespace MiniTimeline.Editor
{
    /// <summary>
    /// Editor utilities for Mini Timeline system
    /// Provides menu items for quick setup and testing
    /// </summary>
    public static class MiniTimelineEditorUtils
    {
        [MenuItem("Mini Timeline/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            // Create director
            var directorObject = new GameObject("Mini Timeline Director");
            var director = directorObject.AddComponent<MiniTimelineDirector>();
            director.Length = 10f;
            director.PlaybackSpeed = 1f;
            director.Loop = true;
            
            // Create demo character (simple cube with components)
            var characterObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            characterObject.name = "Demo Character";
            characterObject.transform.position = Vector3.zero;
            
            // Add Animator component
            var animator = characterObject.AddComponent<Animator>();
            
            // Create a simple SkinnedMeshRenderer setup for morph testing
            // (In a real project, you'd use a proper character model)
            var meshFilter = characterObject.GetComponent<MeshFilter>();
            var mesh = meshFilter.sharedMesh;
            
            // Replace MeshRenderer with SkinnedMeshRenderer for morph testing
            var meshRenderer = characterObject.GetComponent<MeshRenderer>();
            var material = meshRenderer.sharedMaterial;
            Object.DestroyImmediate(meshRenderer);
            Object.DestroyImmediate(meshFilter);
            
            var skinnedRenderer = characterObject.AddComponent<SkinnedMeshRenderer>();
            skinnedRenderer.sharedMaterial = material;
            
            // Create a simple mesh with blendshapes for testing
            var testMesh = CreateTestMeshWithBlendshapes();
            skinnedRenderer.sharedMesh = testMesh;
            
            // Create demo script
            var demoScript = directorObject.AddComponent<MiniTimelineDemo>();
            
            // Configure demo script via reflection to set private fields
            var directorField = typeof(MiniTimelineDemo).GetField("director", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var characterField = typeof(MiniTimelineDemo).GetField("characterObject", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            directorField?.SetValue(demoScript, director);
            characterField?.SetValue(demoScript, characterObject);
            
            // Position camera for better view
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0, 0, -5);
                camera.transform.LookAt(characterObject.transform);
            }
            
            Debug.Log("[MiniTimelineEditorUtils] Demo scene created successfully!");
            Debug.Log("- Press Space to play/pause");
            Debug.Log("- Press S to stop");
            Debug.Log("- Press R to restart");
            Debug.Log("- Check the GUI in play mode for more controls");
            
            // Select the director for easy access
            Selection.activeGameObject = directorObject;
        }
        
        [MenuItem("Mini Timeline/Test Serialization")]
        public static void TestSerialization()
        {
            var project = MiniTimeline.Serialization.SampleProjectCreator.CreateSampleProject();
            
            // Test JSON serialization
            string json = MiniTimeline.Serialization.ProjectSerializer.SaveToJson(project);
            Debug.Log($"[MiniTimelineEditorUtils] Serialized project:\n{json}");
            
            // Test deserialization
            var loadedProject = MiniTimeline.Serialization.ProjectSerializer.LoadFromJson(json);
            
            if (loadedProject != null)
            {
                Debug.Log($"[MiniTimelineEditorUtils] Successfully loaded project: {loadedProject.name}");
                Debug.Log($"- Length: {loadedProject.length}s");
                Debug.Log($"- Tracks: {loadedProject.tracks.Count}");
                Debug.Log($"- Frame Rate: {loadedProject.frameRate} FPS");
            }
            else
            {
                Debug.LogError("[MiniTimelineEditorUtils] Failed to load project from JSON");
            }
        }
        
        /// <summary>
        /// Create a simple test mesh with blendshapes
        /// </summary>
        private static Mesh CreateTestMeshWithBlendshapes()
        {
            // Create a simple cube mesh with a "Smile" blendshape
            // In a real project, you'd import models with proper blendshapes
            
            var mesh = new Mesh();
            mesh.name = "Test Mesh with Blendshapes";
            
            // Simple cube vertices
            var vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };
            
            mesh.vertices = vertices;
            
            // Simple cube triangles (just front face for simplicity)
            var triangles = new int[]
            {
                0, 1, 2, 0, 2, 3, // front
                1, 5, 6, 1, 6, 2, // right
                5, 4, 7, 5, 7, 6, // back
                4, 0, 3, 4, 3, 7, // left
                3, 2, 6, 3, 6, 7, // top
                4, 5, 1, 4, 1, 0  // bottom
            };
            
            mesh.triangles = triangles;
            
            // Add a simple "Smile" blendshape (just move top vertices)
            var smileVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                smileVertices[i] = vertices[i];
                // Move top vertices slightly for "smile" effect
                if (vertices[i].y > 0)
                {
                    smileVertices[i].y += 0.1f;
                }
            }
            
            mesh.AddBlendShapeFrame("Smile", 100f, smileVertices, null, null);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
    }
}
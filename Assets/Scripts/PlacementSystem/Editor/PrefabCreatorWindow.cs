using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Core.Components;

namespace Systems.PlacementSystem.Editor
{
    public class PrefabCreatorWindow : EditorWindow
    {
        private GameObject _targetPrefab;
        private Vector2 _scrollPosition;
        
        // Cache for editors and serialized objects to draw them nicely
        private UnityEditor.Editor _PartEditor;

        [MenuItem("Window/Placement System/Prefab Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabCreatorWindow>("Prefab Creator Hub");
            window.minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Placement Asset Hub", EditorStyles.whiteLargeLabel);
            EditorGUILayout.HelpBox("Drag a GameObject or Prefab here to configure it for the Placement System.", MessageType.Info);
            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            _targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Object", _targetPrefab, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshEditor();
            }

            if (_targetPrefab == null)
            {
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawComponentSetupSection();
            
            if (_targetPrefab.GetComponent<PlacementPart>() != null)
            {
                GUILayout.Space(15);
                DrawSocketManagementSection();
                
                GUILayout.Space(15);
                DrawValidationRulesSection();
                
                GUILayout.Space(15);
                DrawGhostGenerationSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawComponentSetupSection()
        {
            EditorGUILayout.LabelField("1. Core Components", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool hasPlaceable = _targetPrefab.GetComponent<PlacementPart>() != null;
            bool hasCollider = _targetPrefab.GetComponentInChildren<Collider>() != null;

            DrawStatus("Part Component", hasPlaceable);
            DrawStatus("Collider (for socket snapping / raycasts)", hasCollider);

            if (!hasPlaceable || !hasCollider)
            {
                if (GUILayout.Button("Auto-Fix Missing Components", GUILayout.Height(30)))
                {
                    if (!hasPlaceable)
                        _targetPrefab.AddComponent<PlacementPart>();
                    
                    if (!hasCollider)
                    {
                        // Add a BoxCollider that encompasses the renderer bounds
                        var col = _targetPrefab.AddComponent<BoxCollider>();
                        var renderers = _targetPrefab.GetComponentsInChildren<Renderer>();
                        if (renderers.Length > 0)
                        {
                            Bounds bounds = renderers[0].bounds;
                            for (int i = 1; i < renderers.Length; i++)
                            {
                                bounds.Encapsulate(renderers[i].bounds);
                            }
                            col.center = _targetPrefab.transform.InverseTransformPoint(bounds.center);
                            col.size = bounds.size;
                        }
                    }
                    RefreshEditor();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSocketManagementSection()
        {
            EditorGUILayout.LabelField("2. Socket Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var sockets = _targetPrefab.GetComponentsInChildren<Socket>();
            
            EditorGUILayout.LabelField($"Attached Sockets: {sockets.Length}");
            
            if (GUILayout.Button("Add New Socket", GUILayout.Height(25)))
            {
                var socketObj = new GameObject("New Socket");
                socketObj.transform.SetParent(_targetPrefab.transform);
                socketObj.transform.localPosition = Vector3.zero;
                socketObj.AddComponent<Socket>();
                // Sockets need colliders to be detected by the snap system
                var col = socketObj.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.2f;
                UnityEditor.Selection.activeGameObject = socketObj;
            }

            if (GUILayout.Button("Auto-Generate Bounds Sockets (6 Faces)", GUILayout.Height(25)))
            {
                GenerateBoundsSockets();
            }

            // Draw a quick list of sockets
            if (sockets.Length > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Current Sockets:", EditorStyles.miniBoldLabel);
                foreach (var socket in sockets)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(socket.gameObject, typeof(GameObject), true);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        UnityEditor.Selection.activeGameObject = socket.gameObject;
                    }
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        DestroyImmediate(socket.gameObject);
                        GUIUtility.ExitGUI(); // Prevent layout errors after deleting
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationRulesSection()
        {
            EditorGUILayout.LabelField("3. Validation Rules", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (_PartEditor != null)
            {
                // Draw the default inspector for the Part so we can use Unity's list drawing for rules
                _PartEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("Select an object to view rules.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGhostGenerationSection()
        {
            EditorGUILayout.LabelField("4. Ghost Prefab Generation", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox("Ghosts are stripped-down versions of your prefab used for visualizing placement. They have no colliders or gameplay logic, and use a transparent material.", MessageType.Info);

            if (GUILayout.Button("Generate / Update Ghost Prefab", GUILayout.Height(30)))
            {
                GenerateGhostPrefab();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatus(string label, bool isGood)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(isGood ? "✓" : "✗", isGood ? GetGreenStyle() : GetRedStyle(), GUILayout.Width(20));
            GUILayout.Label(label);
            EditorGUILayout.EndHorizontal();
        }

        private GUIStyle GetGreenStyle()
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = Color.green;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        private GUIStyle GetRedStyle()
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = Color.red;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        private void RefreshEditor()
        {
            if (_PartEditor != null)
            {
                DestroyImmediate(_PartEditor);
            }

            if (_targetPrefab != null)
            {
                var placeable = _targetPrefab.GetComponent<PlacementPart>();
                if (placeable != null)
                {
                    _PartEditor = UnityEditor.Editor.CreateEditor(placeable);
                }
            }
        }

        private void OnDestroy()
        {
            if (_PartEditor != null)
            {
                DestroyImmediate(_PartEditor);
            }
        }

        // --- Generator Logic ---

        private void GenerateBoundsSockets()
        {
            var renderers = _targetPrefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("Cannot auto-generate sockets: No renderers found to calculate bounds.");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Convert world bounds back to local space relative to the root prefab
            Vector3 center = _targetPrefab.transform.InverseTransformPoint(bounds.center);
            Vector3 extents = bounds.extents; // extents are half-size

            // Create container
            var container = new GameObject("Sockets");
            container.transform.SetParent(_targetPrefab.transform);
            container.transform.localPosition = Vector3.zero;

            Vector3[] localPositions = new Vector3[]
            {
                center + new Vector3(0, extents.y, 0),  // Top
                center + new Vector3(0, -extents.y, 0), // Bottom
                center + new Vector3(extents.x, 0, 0),  // Right
                center + new Vector3(-extents.x, 0, 0), // Left
                center + new Vector3(0, 0, extents.z),  // Forward
                center + new Vector3(0, 0, -extents.z)  // Back
            };

            string[] names = { "Socket_Top", "Socket_Bottom", "Socket_Right", "Socket_Left", "Socket_Forward", "Socket_Back" };

            for (int i = 0; i < localPositions.Length; i++)
            {
                var socketObj = new GameObject(names[i]);
                socketObj.transform.SetParent(container.transform);
                socketObj.transform.localPosition = localPositions[i];
                
                // Orient sockets to point outward
                var dir = (localPositions[i] - center).normalized;
                if (dir != Vector3.zero)
                {
                    socketObj.transform.rotation = Quaternion.LookRotation(dir);
                }

                socketObj.AddComponent<Socket>();
                var col = socketObj.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.2f;
            }

            Debug.Log($"Generated 6 sockets based on mesh bounds of {_targetPrefab.name}.");
        }

        private void GenerateGhostPrefab()
        {
            // 1. Clone the object in the scene temporarily
            var ghostInstance = Instantiate(_targetPrefab);
            ghostInstance.name = _targetPrefab.name + "_Ghost";

            // 2. Strip bad components (Colliders, rigorous scripts)
            var colliders = ghostInstance.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                DestroyImmediate(col);
            }

            var sockets = ghostInstance.GetComponentsInChildren<Socket>();
            foreach (var socket in sockets)
            {
                DestroyImmediate(socket.gameObject); // Destroy socket children entirely for ghost
            }

            // Remove the main placeable component on the ghost
            var placeable = ghostInstance.GetComponent<PlacementPart>();
            if (placeable != null) DestroyImmediate(placeable);

            // 3. Find/Create common ghost material
            Material ghostMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GhostMaterial.mat");
            if (ghostMat == null)
            {
                // Create a basic transparent blue material if it doesn't exist
                ghostMat = new Material(Shader.Find("Standard"));
                ghostMat.color = new Color(0, 0.5f, 1f, 0.4f);
                // Set to transparent mode
                ghostMat.SetFloat("_Mode", 3);
                ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetInt("_ZWrite", 0);
                ghostMat.DisableKeyword("_ALPHATEST_ON");
                ghostMat.EnableKeyword("_ALPHABLEND_ON");
                ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ghostMat.renderQueue = 3000;
                
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateAsset(ghostMat, "Assets/Materials/GhostMaterial.mat");
            }

            // 4. Assign ghost material
            var renderers = ghostInstance.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                r.sharedMaterials = mats;
            }

            // 5. Save as prefab
            string savePath = EditorUtility.SaveFilePanelInProject("Save Ghost Prefab", ghostInstance.name + ".prefab", "prefab", "Select a location to save the ghost prefab");
            
            if (!string.IsNullOrEmpty(savePath))
            {
                PrefabUtility.SaveAsPrefabAsset(ghostInstance, savePath);
                Debug.Log($"Ghost prefab saved to {savePath}");
                
                // Select the new prefab so the user can inspect it
                UnityEditor.Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
            }

            // 6. Cleanup temp instance
            DestroyImmediate(ghostInstance);
        }
    }
}

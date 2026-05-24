using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPCCC.Common
{
    /// <summary>
    /// Highlight vật thể bằng viền hộp theo BoxCollider thay vì đổi màu toàn bộ mesh.
    /// </summary>
    public class InteractableHighlight : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [Tooltip("Màu của viền highlight.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color m_HighlightColor = new Color(0f, 1f, 0f, 1f);

        [Tooltip("Độ dày của viền highlight theo đơn vị thế giới.")]
        [Min(0.0001f)]
        [SerializeField] private float m_OutlineThickness = 0.01f;

        [Tooltip("Độ nới thêm ra ngoài collider để viền không bị chìm vào mesh.")]
        [Min(0f)]
        [SerializeField] private float m_OutlinePadding = 0.01f;

        [Tooltip("Tự động dùng tất cả BoxCollider trong object và các object con.")]
        [SerializeField] private bool m_IncludeChildBoxColliders = true;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable m_Interactable;
        private readonly List<GameObject> m_OutlineRoots = new List<GameObject>();
        private readonly List<BoxCollider> m_BoxColliders = new List<BoxCollider>();
        private readonly List<List<GameObject>> m_OutlineEdges = new List<List<GameObject>>();
        private Material m_OutlineMaterial;
        private bool m_IsHighlighted;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            CacheBoxColliders();
            CreateOutlineMaterial();
            CreateOutlineObjects();

            m_Interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (m_Interactable != null)
            {
                m_Interactable.hoverEntered.AddListener(OnHoverEnter);
                m_Interactable.hoverExited.AddListener(OnHoverExit);
                m_Interactable.selectEntered.AddListener(OnSelectEnter);
            }
        }

        private void CacheBoxColliders()
        {
            m_BoxColliders.Clear();

            if (m_IncludeChildBoxColliders)
            {
                m_BoxColliders.AddRange(GetComponentsInChildren<BoxCollider>(true));
            }
            else
            {
                BoxCollider boxCollider = GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    m_BoxColliders.Add(boxCollider);
                }
            }

            if (m_BoxColliders.Count == 0)
            {
                Debug.LogWarning($"[InteractableHighlight] {name} không có BoxCollider để tạo viền highlight.");
            }
        }

        private void CreateOutlineMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                Debug.LogWarning("[InteractableHighlight] Không tìm thấy shader phù hợp để vẽ viền highlight.");
                return;
            }

            m_OutlineMaterial = new Material(shader);
            if (m_OutlineMaterial.HasProperty(BaseColorId))
            {
                m_OutlineMaterial.SetColor(BaseColorId, m_HighlightColor);
            }

            if (m_OutlineMaterial.HasProperty(ColorId))
            {
                m_OutlineMaterial.SetColor(ColorId, m_HighlightColor);
            }
        }

        private void CreateOutlineObjects()
        {
            foreach (BoxCollider boxCollider in m_BoxColliders)
            {
                if (boxCollider == null)
                {
                    continue;
                }

                GameObject outlineRoot = new GameObject($"{boxCollider.name}_HighlightOutline");
                outlineRoot.transform.position = boxCollider.transform.position;
                outlineRoot.transform.rotation = boxCollider.transform.rotation;
                outlineRoot.transform.localScale = Vector3.one;
                outlineRoot.SetActive(false);

                List<GameObject> edgeObjects = new List<GameObject>(12);
                Vector3[] corners = GetBoxCorners(boxCollider);
                int[,] edges =
                {
                    { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
                    { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
                    { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
                };

                for (int edgeIndex = 0; edgeIndex < edges.GetLength(0); edgeIndex++)
                {
                    GameObject edgeObject = CreateEdgeCube(outlineRoot.transform);
                    edgeObjects.Add(edgeObject);
                }

                m_OutlineRoots.Add(outlineRoot);
                m_OutlineEdges.Add(edgeObjects);
            }
        }

        private GameObject CreateEdgeCube(Transform parent)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "HighlightEdge";
            edge.transform.SetParent(parent, false);

            edge.transform.localPosition = Vector3.zero;
            edge.transform.localRotation = Quaternion.identity;
            edge.transform.localScale = Vector3.one;

            Collider edgeCollider = edge.GetComponent<Collider>();
            if (edgeCollider != null)
            {
                Destroy(edgeCollider);
            }

            Renderer edgeRenderer = edge.GetComponent<Renderer>();
            if (edgeRenderer != null)
            {
                if (m_OutlineMaterial != null)
                {
                    edgeRenderer.sharedMaterial = m_OutlineMaterial;
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null)
                    {
                        shader = Shader.Find("Unlit/Color");
                    }

                    if (shader != null)
                    {
                        Material fallbackMaterial = new Material(shader);
                        if (fallbackMaterial.HasProperty("_BaseColor"))
                        {
                            fallbackMaterial.SetColor("_BaseColor", m_HighlightColor);
                        }

                        if (fallbackMaterial.HasProperty("_Color"))
                        {
                            fallbackMaterial.SetColor("_Color", m_HighlightColor);
                        }

                        edgeRenderer.sharedMaterial = fallbackMaterial;
                    }
                }
            }

            return edge;
        }

        private Vector3[] GetBoxCorners(BoxCollider boxCollider)
        {
            Vector3 center = boxCollider.center;
            Vector3 extents = (boxCollider.size * 0.5f) + Vector3.one * m_OutlinePadding;

            Vector3[] localCorners =
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z)
            };

            Vector3[] worldCorners = new Vector3[localCorners.Length];
            for (int i = 0; i < localCorners.Length; i++)
            {
                worldCorners[i] = boxCollider.transform.TransformPoint(localCorners[i]);
            }

            return worldCorners;
        }

        private void LateUpdate()
        {
            if (!m_IsHighlighted)
            {
                return;
            }

            RefreshOutlineGeometry();
        }

        private void RefreshOutlineGeometry()
        {
            for (int colliderIndex = 0; colliderIndex < m_BoxColliders.Count; colliderIndex++)
            {
                BoxCollider boxCollider = m_BoxColliders[colliderIndex];
                if (boxCollider == null || colliderIndex >= m_OutlineRoots.Count || colliderIndex >= m_OutlineEdges.Count)
                {
                    continue;
                }

                GameObject outlineRoot = m_OutlineRoots[colliderIndex];
                List<GameObject> edgeObjects = m_OutlineEdges[colliderIndex];
                if (outlineRoot == null || edgeObjects == null || edgeObjects.Count != 12)
                {
                    continue;
                }

                outlineRoot.transform.position = boxCollider.transform.position;
                outlineRoot.transform.rotation = boxCollider.transform.rotation;

                Vector3[] corners = GetBoxCorners(boxCollider);
                int[,] edges =
                {
                    { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
                    { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
                    { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
                };

                for (int edgeIndex = 0; edgeIndex < edgeObjects.Count; edgeIndex++)
                {
                    GameObject edge = edgeObjects[edgeIndex];
                    if (edge == null)
                    {
                        continue;
                    }

                    Vector3 start = corners[edges[edgeIndex, 0]];
                    Vector3 end = corners[edges[edgeIndex, 1]];
                    Vector3 direction = end - start;
                    float length = direction.magnitude;
                    Vector3 midpoint = (start + end) * 0.5f;

                    edge.transform.position = midpoint;
                    edge.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
                    edge.transform.localScale = new Vector3(length, m_OutlineThickness, m_OutlineThickness);
                }
            }
        }

        private void OnDestroy()
        {
            if (m_Interactable != null)
            {
                m_Interactable.hoverEntered.RemoveListener(OnHoverEnter);
                m_Interactable.hoverExited.RemoveListener(OnHoverExit);
                m_Interactable.selectEntered.RemoveListener(OnSelectEnter);
            }

            for (int i = 0; i < m_OutlineRoots.Count; i++)
            {
                if (m_OutlineRoots[i] != null)
                {
                    Destroy(m_OutlineRoots[i]);
                }
            }

            for (int i = 0; i < m_OutlineEdges.Count; i++)
            {
                List<GameObject> edgeObjects = m_OutlineEdges[i];
                if (edgeObjects == null)
                {
                    continue;
                }

                for (int j = 0; j < edgeObjects.Count; j++)
                {
                    if (edgeObjects[j] != null)
                    {
                        Destroy(edgeObjects[j]);
                    }
                }
            }

            if (m_OutlineMaterial != null)
            {
                Destroy(m_OutlineMaterial);
            }
        }

        private void OnHoverEnter(HoverEnterEventArgs args)
        {
            EnableHighlight();
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            DisableHighlight();
        }

        private void OnSelectEnter(SelectEnterEventArgs args)
        {
            DisableHighlight();
        }

        public void EnableHighlight()
        {
            if (m_IsHighlighted)
            {
                return;
            }

            m_IsHighlighted = true;

            for (int i = 0; i < m_OutlineRoots.Count; i++)
            {
                if (m_OutlineRoots[i] != null)
                {
                    m_OutlineRoots[i].SetActive(true);
                }
            }

            RefreshOutlineGeometry();
        }

        public void DisableHighlight()
        {
            if (!m_IsHighlighted)
            {
                return;
            }

            m_IsHighlighted = false;

            for (int i = 0; i < m_OutlineRoots.Count; i++)
            {
                if (m_OutlineRoots[i] != null)
                {
                    m_OutlineRoots[i].SetActive(false);
                }
            }
        }
    }
}

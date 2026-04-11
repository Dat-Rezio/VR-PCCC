using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRPCCC.Scenario2
{
    /// <summary>
    /// Xử lý khi người dùng cầm bình bột ABC (lựa chọn sai).
    /// Thông báo cho ScenarioManager để trừ điểm và hiển thị cảnh báo.
    /// Gắn lên Prefab bình bột ABC, thêm XRGrabInteractable.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ABCExtinguisher : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Tham Chiếu")]
        [Tooltip("ScenarioManager trung tâm.")]
        [SerializeField] FirefightingScenarioManager m_Manager;

        [Tooltip("Nếu true, sẽ đặt lại bình về vị trí ban đầu sau khi thả ra.")]
        [SerializeField] bool m_ResetPositionOnRelease = true;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        XRGrabInteractable m_Grab;
        Vector3    m_InitialPosition;
        Quaternion m_InitialRotation;
        bool       m_Warned;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_InitialPosition = transform.position;
            m_InitialRotation = transform.rotation;
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnGrabbed);
            m_Grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnGrabbed);
            m_Grab.selectExited.RemoveListener(OnReleased);
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Callbacks
        // ──────────────────────────────────────────────────────────────────── //

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_Manager?.OnExtinguisherGrabbed(isCO2: false);
            Debug.Log("[ABCExtinguisher] ❌ Người dùng cầm bình bột ABC - Sai lựa chọn.");
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (!m_ResetPositionOnRelease) return;

            // Trả bình về vị trí ban đầu sau 1 giây
            Invoke(nameof(ResetPosition), 1f);
        }

        void ResetPosition()
        {
            if (m_Grab.isSelected) return; // Đừng reset nếu đang cầm

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity        = Vector3.zero;
                rb.angularVelocity  = Vector3.zero;
                rb.isKinematic      = true;
            }

            transform.SetPositionAndRotation(m_InitialPosition, m_InitialRotation);

            if (rb != null) rb.isKinematic = false;
            m_Warned = false;
        }

        /// <summary>Đặt lại trạng thái cảnh báo (khi reset kịch bản).</summary>
        public void ResetExtinguisher()
        {
            ResetPosition();
            m_Warned = false;
        }
    }
}

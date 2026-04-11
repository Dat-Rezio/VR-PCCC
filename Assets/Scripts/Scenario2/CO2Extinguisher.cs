using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRPCCC.Scenario2
{
    /// <summary>
    /// Logic bình chữa cháy CO₂.
    /// Triển khai đầy đủ 4 bước kỹ thuật:
    ///   1. Kiểm tra khoảng cách đến lửa
    ///   2. Rút chốt an toàn
    ///   3. Hướng vòi (Raycast)
    ///   4. Phun liên tục (giữ Trigger)
    ///
    /// An toàn VR:
    ///   • Cảnh báo khi tay chạm vào loa vòi (bỏng lạnh CO₂)
    ///   • Không cho phép phun khi chưa rút chốt
    ///
    /// Gắn script này lên Prefab bình CO₂ đã có XRGrabInteractable.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class CO2Extinguisher : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Tham Chiếu Scene")]
        [Tooltip("ScenarioManager trung tâm.")]
        [SerializeField] FirefightingScenarioManager m_Manager;

        [Tooltip("FireSource - đám lửa cần dập.")]
        [SerializeField] FireSource m_FireSource;

        [Tooltip("HUD để hiển thị cảnh báo.")]
        [SerializeField] ScenarioHUD m_HUD;

        [Header("Điểm Phun / Vòi")]
        [Tooltip("Transform đầu loa vòi phun (nơi phát Raycast và hiệu ứng khí).")]
        [SerializeField] Transform m_NozzleTip;

        [Tooltip("Độ dài Raycast từ đầu vòi (m).")]
        [SerializeField] float m_NozzleRayLength = 5f;

        [Tooltip("Layer Mask cho đối tượng lửa (Root_Fire collider).")]
        [SerializeField] LayerMask m_FireLayerMask = ~0; // Mặc định = tất cả

        [Header("An Toàn: Loa Vòi (Horn) – Bỏng Lạnh CO₂")]
        [Tooltip("SphereCollider trigger ở đầu loa vòi. Phát hiện tay cầm vào vùng nguy hiểm.")]
        [SerializeField] SphereCollider m_HornDangerZone;

        [Tooltip("Bán kính vùng nguy hiểm quanh loa vòi (m). Nhỏ ~0.05m.")]
        [SerializeField] float m_HornDangerRadius = 0.06f;

        [Tooltip("Tag của tay cầm VR (Controller/Hand).")]
        [SerializeField] string m_HandTag = "PlayerHand";

        [Header("Chốt An Toàn (Safety Pin)")]
        [Tooltip("XRSimpleInteractable của chốt an toàn. Player grab để rút chốt.")]
        [SerializeField] XRSimpleInteractable m_PinInteractable;

        [Tooltip("GameObject của chốt an toàn (sẽ ẩn khi kéo ra).")]
        [SerializeField] GameObject m_PinObject;

        [Header("Hiệu Ứng CO₂")]
        [Tooltip("ParticleSystem sương mù trắng CO₂ (Shape=Cone, màu trắng đục).")]
        [SerializeField] ParticleSystem m_CO2SprayVFX;

        [Tooltip("AudioSource + CO2 hissing clip.")]
        [SerializeField] AudioSource m_SprayAudio;

        [Tooltip("Clip âm thanh xịt CO₂ đặc trưng.")]
        [SerializeField] AudioClip m_CO2HissingClip;

        [Header("Cài Đặt Khoảng Cách")]
        [Tooltip("Khoảng cách tối thiểu từ người dùng đến lửa (m). Dùng bởi ScenarioManager.")]
        [SerializeField] float m_MinFireDistance = 2f;

        [Tooltip("Khoảng cách tối đa từ người dùng đến lửa (m). Dùng bởi ScenarioManager.")]
        [SerializeField] float m_MaxFireDistance = 3f;

        // Expose cho ScenarioManager nếu cần đọc ngưỡng từ bên ngoài
        public float MinFireDistance => m_MinFireDistance;
        public float MaxFireDistance => m_MaxFireDistance;

        [Header("Haptic Feedback")]
        [Tooltip("Cường độ rung khi bóp cò khi chưa rút chốt (0–1).")]
        [SerializeField] float m_LockHapticAmplitude = 0.8f;

        [Tooltip("Thời lượng rung (giây).")]
        [SerializeField] float m_LockHapticDuration = 0.3f;

        [Tooltip("Cường độ rung cảnh báo loa vòi.")]
        [SerializeField] float m_HornHapticAmplitude = 1.0f;

        [Tooltip("Thời lượng rung cảnh báo loa vòi.")]
        [SerializeField] float m_HornHapticDuration = 0.5f;

        [Header("Events")]
        public UnityEvent OnPinPulled;
        public UnityEvent OnSprayStart;
        public UnityEvent OnSprayStop;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        XRGrabInteractable       m_Grab;
        IXRSelectInteractor      m_HoldingInteractor;

        bool  m_IsGrabbed;
        bool  m_IsPinPulled;
        bool  m_IsSpraying;
        bool  m_PinLockWarningShown;
        float m_DistanceCheckCooldown;
        float m_AimWarningCooldown;
        bool  m_HornWarningShown;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();

            // Cấu hình vùng nguy hiểm loa vòi
            if (m_HornDangerZone != null)
            {
                m_HornDangerZone.isTrigger = true;
                m_HornDangerZone.radius    = m_HornDangerRadius;
            }

            // Đảm bảo VFX tắt ban đầu
            if (m_CO2SprayVFX != null) m_CO2SprayVFX.Stop();
            if (m_SprayAudio  != null) m_SprayAudio.Stop();
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnGrabbed);
            m_Grab.selectExited.AddListener(OnReleased);

            if (m_PinInteractable != null)
                m_PinInteractable.selectEntered.AddListener(OnPinGrabbed);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnGrabbed);
            m_Grab.selectExited.RemoveListener(OnReleased);

            if (m_PinInteractable != null)
                m_PinInteractable.selectEntered.RemoveListener(OnPinGrabbed);
        }

        void Update()
        {
            if (!m_IsGrabbed) return;

            m_DistanceCheckCooldown -= Time.deltaTime;
            m_AimWarningCooldown    -= Time.deltaTime;

            // Đọc giá trị trigger từ interactor đang cầm
            float triggerValue = GetTriggerValue();
            bool  isTriggerPressed = triggerValue > 0.5f;

            var state = m_Manager ? m_Manager.CurrentState : FirefightingScenarioManager.ScenarioState.Idle;

            switch (state)
            {
                // ── Check 1: Khoảng cách ──────────────────────────────────
                case FirefightingScenarioManager.ScenarioState.CheckDistance:
                    HandleDistanceCheck();
                    break;

                // ── Check 2: Rút chốt + Lock khi bóp cò sớm ──────────────
                case FirefightingScenarioManager.ScenarioState.PullPin:
                    if (isTriggerPressed && !m_IsPinPulled)
                        HandleLockedTrigger();
                    break;

                // ── Check 3: Hướng vòi ────────────────────────────────────
                case FirefightingScenarioManager.ScenarioState.AimNozzle:
                    HandleNozzleAim();
                    // Vẫn kiểm tra trigger bóp sớm
                    if (isTriggerPressed && !m_IsPinPulled)
                        HandleLockedTrigger();
                    break;

                // ── Check 4: Phun ─────────────────────────────────────────
                case FirefightingScenarioManager.ScenarioState.Spraying:
                    HandleSpray(isTriggerPressed);
                    break;

                default:
                    StopSpray();
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  XR Interactable Callbacks
        // ──────────────────────────────────────────────────────────────────── //

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_IsGrabbed        = true;
            m_HoldingInteractor = args.interactorObject;
            m_Manager?.OnExtinguisherGrabbed(isCO2: true);
            Debug.Log("[CO2] 🤲 Đã cầm bình CO₂.");
        }

        void OnReleased(SelectExitEventArgs args)
        {
            m_IsGrabbed         = false;
            m_HoldingInteractor = null;
            StopSpray();
        }

        void OnPinGrabbed(SelectEnterEventArgs args)
        {
            if (m_IsPinPulled) return;

            m_IsPinPulled = true;

            // Ẩn chốt
            if (m_PinObject != null) m_PinObject.SetActive(false);

            m_Manager?.OnPinPulled();
            OnPinPulled?.Invoke();
            Debug.Log("[CO2] 📌 Đã rút chốt an toàn!");
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Physics Trigger – Horn Danger Zone
        // ──────────────────────────────────────────────────────────────────── //

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(m_HandTag)) return;
            if (m_HornWarningShown) return;

            m_HornWarningShown = true;

            // Cảnh báo bỏng lạnh
            m_HUD?.ShowWarning(
                "⚠️ NGUY HIỂM!\nKhông cầm vào loa vòi!\nCO₂ cực lạnh, gây bỏng lạnh ngay lập tức.",
                4f
            );

            // Haptic mạnh để nhắc nhở
            SendHapticToHand(m_HornHapticAmplitude, m_HornHapticDuration);
            Debug.Log("[CO2] ⚠️ Tay chạm loa vòi - Cảnh báo bỏng lạnh CO₂!");
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(m_HandTag))
                m_HornWarningShown = false;
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Step Handlers
        // ──────────────────────────────────────────────────────────────────── //

        void HandleDistanceCheck()
        {
            if (m_DistanceCheckCooldown > 0f || m_FireSource == null) return;
            m_DistanceCheckCooldown = 1.5f; // poll mỗi 1.5 giây

            float dist = Vector3.Distance(
                Camera.main != null ? Camera.main.transform.position : transform.position,
                m_FireSource.transform.position
            );

            m_Manager?.OnDistanceCheck(dist);
        }

        void HandleLockedTrigger()
        {
            if (m_PinLockWarningShown) return;
            m_PinLockWarningShown = true;

            m_HUD?.ShowWarning("🔒 Cò đang bị khóa!\nHãy rút chốt an toàn trước.", 3f);
            SendHapticToHand(m_LockHapticAmplitude, m_LockHapticDuration);
            Debug.Log("[CO2] 🔒 Bóp cò khi chưa rút chốt - Haptic feedback.");

            // Reset cờ sau vài giây để có thể cảnh báo lại
            StartCoroutine(ResetPinWarning());
        }

        void HandleNozzleAim()
        {
            if (m_NozzleTip == null) return;

            bool onTarget = PerformNozzleRaycast(out RaycastHit hit);
            m_Manager?.OnNozzleAimedUpdate(onTarget);

            if (!onTarget && m_AimWarningCooldown <= 0f)
            {
                m_AimWarningCooldown = 2f;
                m_HUD?.ShowWarning("↩️ Hướng vòi vào <b>gốc lửa</b>, không phải ngọn lửa!", 2f);
            }
        }

        void HandleSpray(bool isTriggerPressed)
        {
            if (m_FireSource == null) return;

            // Kiểm tra tiếp tục nhắm đúng
            bool stillOnTarget = PerformNozzleRaycast(out _);

            if (isTriggerPressed && stillOnTarget)
            {
                StartSpray();
                m_FireSource.ApplyExtinguishing(Time.deltaTime);
            }
            else
            {
                StopSpray();

                if (!stillOnTarget && m_AimWarningCooldown <= 0f && isTriggerPressed)
                {
                    m_AimWarningCooldown = 2f;
                    m_HUD?.ShowWarning("↩️ Hướng về gốc lửa và giữ nút bóp cò!", 2f);
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Spray VFX / SFX
        // ──────────────────────────────────────────────────────────────────── //

        void StartSpray()
        {
            if (m_IsSpraying) return;
            m_IsSpraying = true;

            if (m_CO2SprayVFX != null && !m_CO2SprayVFX.isPlaying)
                m_CO2SprayVFX.Play();

            if (m_SprayAudio != null && m_CO2HissingClip != null && !m_SprayAudio.isPlaying)
            {
                m_SprayAudio.clip = m_CO2HissingClip;
                m_SprayAudio.loop = true;
                m_SprayAudio.Play();
            }

            OnSprayStart?.Invoke();
        }

        void StopSpray()
        {
            if (!m_IsSpraying) return;
            m_IsSpraying = false;

            if (m_CO2SprayVFX != null && m_CO2SprayVFX.isPlaying)
                m_CO2SprayVFX.Stop();

            if (m_SprayAudio != null && m_SprayAudio.isPlaying)
                m_SprayAudio.Stop();

            OnSprayStop?.Invoke();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Raycast
        // ──────────────────────────────────────────────────────────────────── //

        bool PerformNozzleRaycast(out RaycastHit hit)
        {
            if (m_NozzleTip == null) { hit = default; return false; }

            bool onTarget = Physics.Raycast(
                m_NozzleTip.position,
                m_NozzleTip.forward,
                out hit,
                m_NozzleRayLength,
                m_FireLayerMask
            );

            // Chấp nhận collider có tag "Root_Fire"
            if (onTarget && !hit.collider.CompareTag("Root_Fire"))
                onTarget = false;

#if UNITY_EDITOR
            Debug.DrawRay(
                m_NozzleTip.position,
                m_NozzleTip.forward * m_NozzleRayLength,
                onTarget ? Color.green : Color.yellow
            );
#endif
            return onTarget;
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Haptic Feedback
        // ──────────────────────────────────────────────────────────────────── //

        void SendHapticToHand(float amplitude, float duration)
        {
            if (m_HoldingInteractor == null) return;

            // XRI 3.x: IXRSelectInteractor có thể là XRBaseInputInteractor
            if (m_HoldingInteractor is XRBaseInputInteractor inputInteractor)
            {
                inputInteractor.SendHapticImpulse(amplitude, duration);
            }
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Helpers
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>
        /// Đọc giá trị trigger từ interactor đang cầm bình.
        /// Tương thích cả Input System và Legacy Input.
        /// </summary>
        float GetTriggerValue()
        {
            if (m_HoldingInteractor == null) return 0f;

            // XRRayInteractor không dùng trigger trong context phun bình này
            if (m_HoldingInteractor is XRRayInteractor)
                return 0f;

            // XRI 3.x: XRBaseInputInteractor có property `activateInput` kiểu XRInputButtonReader
            // Dùng ReadValue() để lấy giá trị float của nút kích hoạt (trigger)
            if (m_HoldingInteractor is XRBaseInputInteractor baseInteractor)
            {
                return baseInteractor.activateInput.ReadValue();
            }

            return 0f;
        }

        System.Collections.IEnumerator ResetPinWarning()
        {
            yield return new WaitForSeconds(3f);
            m_PinLockWarningShown = false;
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public Reset
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>Đặt lại bình về trạng thái ban đầu (chốt chưa rút).</summary>
        public void ResetExtinguisher()
        {
            m_IsPinPulled         = false;
            m_PinLockWarningShown = false;
            m_HornWarningShown    = false;
            StopSpray();

            if (m_PinObject != null) m_PinObject.SetActive(true);
        }
    }
}

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRPCCC.Scenario2
{
    /// <summary>
    /// Logic bình chữa cháy dạng bột ABC.
    /// Giống với CO2 nhưng không có cảnh báo bỏng lạnh ở vòi.
    /// Gắn script này lên Prefab bình ABC.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ABCExtinguisher : MonoBehaviour
    {
        [Header("Tham Chiếu Scene")]
        [Tooltip("ScenarioManager trung tâm.")]
        [SerializeField] FirefightingScenarioManager m_Manager;

        [Tooltip("FireSource fallback (Dùng khi test lẻ không có Manager).")]
        [SerializeField] FireSource m_FireSource;

        /// <summary>Lấy đám lửa đang cháy hiện tại từ Manager (Phase 1 hay Phase 2).</summary>
        FireSource ActiveFire => m_Manager != null ? m_Manager.GetActiveFire() : m_FireSource;

        [Tooltip("HUD để hiển thị cảnh báo.")]
        [SerializeField] ScenarioHUD m_HUD;

        [Header("Điểm Phun / Vòi")]
        [Tooltip("Transform đầu vòi phun (nơi phát Raycast và hiệu ứng).")]
        [SerializeField] Transform m_NozzleTip;

        [Tooltip("Độ dài Raycast từ đầu vòi (m).")]
        [SerializeField] float m_NozzleRayLength = 5f;

        [Tooltip("Layer Mask cho đối tượng lửa (Root_Fire collider).")]
        [SerializeField] LayerMask m_FireLayerMask = ~0; 

        [Header("Chốt An Toàn (Safety Pin)")]
        [Tooltip("XRSimpleInteractable của chốt an toàn. Player grab để rút chốt.")]
        [SerializeField] XRSimpleInteractable m_PinInteractable;

        [Tooltip("GameObject của chốt an toàn (sẽ ẩn khi kéo ra).")]
        [SerializeField] GameObject m_PinObject;

        [Header("Hiệu Ứng Phun Bột ABC")]
        [Tooltip("ParticleSystem bột trắng/vàng đục ABC.")]
        [SerializeField] ParticleSystem m_ABCSprayVFX;

        [Tooltip("AudioSource âm thanh xịt.")]
        [SerializeField] AudioSource m_SprayAudio;

        [Tooltip("Clip âm thanh xịt đặc trưng.")]
        [SerializeField] AudioClip m_ABCHissingClip;

        [Header("Cài Đặt Khoảng Cách")]
        [SerializeField] float m_MinFireDistance = 2f;
        [SerializeField] float m_MaxFireDistance = 3f;

        public float MinFireDistance => m_MinFireDistance;
        public float MaxFireDistance => m_MaxFireDistance;

        [Header("Haptic Feedback")]
        [Tooltip("Cường độ rung khi bóp cò khi chưa rút chốt (0–1).")]
        [SerializeField] float m_LockHapticAmplitude = 0.8f;
        [SerializeField] float m_LockHapticDuration = 0.3f;

        [Header("Events")]
        public UnityEvent OnPinPulled;
        public UnityEvent OnSprayStart;
        public UnityEvent OnSprayStop;

        XRGrabInteractable       m_Grab;
        IXRSelectInteractor      m_HoldingInteractor;

        bool  m_IsGrabbed;
        bool  m_IsPinPulled;
        bool  m_IsSpraying;
        bool  m_PinLockWarningShown;
        float m_DistanceCheckCooldown;
        float m_AimWarningCooldown;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();

            if (m_ABCSprayVFX != null) m_ABCSprayVFX.Stop();
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

            float triggerValue = GetTriggerValue();
            bool  isTriggerPressed = triggerValue > 0.5f;

            var state = m_Manager ? m_Manager.CurrentState : FirefightingScenarioManager.ScenarioState.Idle;

            switch (state)
            {
                case FirefightingScenarioManager.ScenarioState.CheckDistance:
                    HandleDistanceCheck();
                    break;
                case FirefightingScenarioManager.ScenarioState.PullPin:
                    if (isTriggerPressed && !m_IsPinPulled)
                        HandleLockedTrigger();
                    break;
                case FirefightingScenarioManager.ScenarioState.AimNozzle:
                    HandleNozzleAim();
                    if (isTriggerPressed && !m_IsPinPulled)
                        HandleLockedTrigger();
                    break;
                case FirefightingScenarioManager.ScenarioState.Spraying:
                    HandleSpray(isTriggerPressed);
                    break;
                default:
                    StopSpray();
                    break;
            }
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_IsGrabbed        = true;
            m_HoldingInteractor = args.interactorObject;
            m_Manager?.OnExtinguisherGrabbed(isCO2: false); // Đây là bình ABC
            Debug.Log("[ABC] 🤲 Đã cầm bình bột ABC.");
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
            if (m_PinObject != null) m_PinObject.SetActive(false);

            m_Manager?.OnPinPulled();
            OnPinPulled?.Invoke();
            Debug.Log("[ABC] 📌 Đã rút chốt an toàn!");
        }

        void HandleDistanceCheck()
        {
            if (m_DistanceCheckCooldown > 0f || ActiveFire == null) return;
            m_DistanceCheckCooldown = 1.5f;

            float dist = Vector3.Distance(
                Camera.main != null ? Camera.main.transform.position : transform.position,
                ActiveFire.transform.position
            );

            m_Manager?.OnDistanceCheck(dist);
        }

        void HandleLockedTrigger()
        {
            if (m_PinLockWarningShown) return;
            m_PinLockWarningShown = true;

            m_HUD?.ShowWarning("🔒 Cò đang bị khóa!\nHãy rút chốt an toàn trước.", 3f);
            SendHapticToHand(m_LockHapticAmplitude, m_LockHapticDuration);
            Debug.Log("[ABC] 🔒 Bóp cò khi chưa rút chốt - Haptic feedback.");

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
                m_HUD?.ShowWarning("↩️ Hướng vòi phun vào <b>gốc lửa</b>!", 2f);
            }
        }

        void HandleSpray(bool isTriggerPressed)
        {
            if (ActiveFire == null) return;

            bool stillOnTarget = PerformNozzleRaycast(out _);

            if (isTriggerPressed && stillOnTarget)
            {
                StartSpray();
                ActiveFire.ApplyExtinguishing(Time.deltaTime);
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

        void StartSpray()
        {
            if (m_IsSpraying) return;
            m_IsSpraying = true;

            if (m_ABCSprayVFX != null && !m_ABCSprayVFX.isPlaying)
                m_ABCSprayVFX.Play();

            if (m_SprayAudio != null && m_ABCHissingClip != null && !m_SprayAudio.isPlaying)
            {
                m_SprayAudio.clip = m_ABCHissingClip;
                m_SprayAudio.loop = true;
                m_SprayAudio.Play();
            }

            OnSprayStart?.Invoke();
        }

        void StopSpray()
        {
            if (!m_IsSpraying) return;
            m_IsSpraying = false;

            if (m_ABCSprayVFX != null && m_ABCSprayVFX.isPlaying)
                m_ABCSprayVFX.Stop();

            if (m_SprayAudio != null && m_SprayAudio.isPlaying)
                m_SprayAudio.Stop();

            OnSprayStop?.Invoke();
        }

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

        void SendHapticToHand(float amplitude, float duration)
        {
            if (m_HoldingInteractor == null) return;

            if (m_HoldingInteractor is XRBaseInputInteractor inputInteractor)
            {
                inputInteractor.SendHapticImpulse(amplitude, duration);
            }
        }

        float GetTriggerValue()
        {
            if (m_HoldingInteractor == null) return 0f;

            if (m_HoldingInteractor is XRRayInteractor)
                return 0f;

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

        public void ResetExtinguisher()
        {
            m_IsPinPulled         = false;
            m_PinLockWarningShown = false;
            StopSpray();

            if (m_PinObject != null) m_PinObject.SetActive(true);
        }
    }
}

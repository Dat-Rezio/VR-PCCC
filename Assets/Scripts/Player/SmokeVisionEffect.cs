using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VRPCCC.Player
{
    public class SmokeVisionEffect : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Inspector Fields (Giữ nguyên như cũ)
        // ------------------------------------------------------------------ //

        [Header("UI References")]
        [SerializeField] CanvasGroup m_SmokeCanvasGroup;
        [SerializeField] Image m_SmokeImage;

        [Header("Màu Khói")]
        [SerializeField] Color m_SmokeColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] Color m_DangerColor = new Color(0.05f, 0.05f, 0.05f, 1f);

        [Header("Mức Opacity Khói")]
        [Range(0f, 1f)]
        [SerializeField] float m_StandingAlpha = 0.85f;
        // Đã bỏ m_DuckingAlpha vì logic mới không cần khói khi cúi

        [SerializeField] float m_FadeInSpeed = 1.2f;
        [SerializeField] float m_FadeOutSpeed = 2.5f;

        [Header("Hiệu Ứng Nhấp Nháy")]
        [SerializeField] bool m_EnableFlicker = true;
        [Range(0f, 0.15f)]
        [SerializeField] float m_FlickerAmplitude = 0.05f;
        [Range(0.5f, 10f)]
        [SerializeField] float m_FlickerSpeed = 3f;

        [Header("Hiệu Ứng Vignette Cảnh Báo")]
        [SerializeField] Image m_DangerVignetteImage;

        // ------------------------------------------------------------------ //
        //  Runtime State
        // ------------------------------------------------------------------ //

        float m_TargetAlpha;        
        float m_CurrentAlpha;       
        float m_SmokeDensity;       
        bool m_InSmokeZone;         
        [HideInInspector] public bool m_IsDucking; // Ẩn khỏi Inspector để tránh tick nhầm

        Coroutine m_DangerFlashCoroutine;

        // ------------------------------------------------------------------ //
        //  Unity Lifecycle
        // ------------------------------------------------------------------ //

        void Awake()
        {
            m_IsDucking = false;
            m_CurrentAlpha = 0f;
            m_TargetAlpha = 0f;

            if (m_SmokeCanvasGroup != null)
            {
                m_SmokeCanvasGroup.alpha = 0f;
                // TẮT HẲN Canvas khi mới bắt đầu game để tiết kiệm hiệu năng
                m_SmokeCanvasGroup.gameObject.SetActive(false); 
            }

            if (m_SmokeImage != null)
                m_SmokeImage.color = m_SmokeColor;

            if (m_DangerVignetteImage != null)
            {
                var c = m_DangerVignetteImage.color;
                m_DangerVignetteImage.color = new Color(c.r, c.g, c.b, 0f);
            }
        }

        void Update()
        {
            UpdateAlphaTarget();
            SmoothAlpha();

            if (m_EnableFlicker && m_InSmokeZone && !m_IsDucking)
                ApplyFlicker();
        }

        // ------------------------------------------------------------------ //
        //  Public API (Giữ nguyên các hàm kết nối sự kiện)
        // ------------------------------------------------------------------ //

        public void OnPlayerDuck()
        {
            m_IsDucking = true;
            Debug.Log("[SmokeVisionEffect] Cúi người → Tắt hiệu ứng khói.");
        }

        public void OnPlayerStand()
        {
            m_IsDucking = false;
            Debug.Log("[SmokeVisionEffect] Đứng dậy → Kiểm tra lại hiệu ứng khói.");
        }

        public void BeginSmokeExposure(float density)
        {
            m_InSmokeZone = true;
            m_SmokeDensity = density;

            if (m_SmokeImage != null)
                m_SmokeImage.color = Color.Lerp(m_SmokeColor, m_DangerColor, density);

            Debug.Log($"[SmokeVisionEffect] Vào vùng khói (density={density:F2})");
        }

        public void EndSmokeExposure()
        {
            m_InSmokeZone = false;
            m_SmokeDensity = 0f;
            Debug.Log("[SmokeVisionEffect] Thoát vùng khói.");
        }

        public void TriggerDangerWarning(float intensity = 1f)
        {
            if (m_DangerVignetteImage == null) return;

            if (m_DangerFlashCoroutine != null)
                StopCoroutine(m_DangerFlashCoroutine);

            m_DangerFlashCoroutine = StartCoroutine(FlashDangerVignette(intensity));
        }

        public void ClearEffect()
        {
            m_InSmokeZone = false;
            m_IsDucking = false;
            m_SmokeDensity = 0f;
            m_TargetAlpha = 0f;
        }

        // ------------------------------------------------------------------ //
        //  Internal Logic (ĐÃ CẬP NHẬT THEO LOGIC MỚI)
        // ------------------------------------------------------------------ //

        void UpdateAlphaTarget()
        {
            // KIỂM TRA NGẶT NGHÈO 2 ĐIỀU KIỆN
            if (m_InSmokeZone && !m_IsDucking)
            {
                // Thỏa mãn cả 2: Đang trong khói VÀ Đang đứng thẳng
                m_TargetAlpha = m_StandingAlpha * m_SmokeDensity;
            }
            else
            {
                // Không thỏa mãn (Ra khỏi khói HOẶC Đang cúi người) -> Không có khói
                m_TargetAlpha = 0f;
            }
        }

        void SmoothAlpha()
        {
            if (m_SmokeCanvasGroup == null) return;

            // Xử lý bật/tắt GameObject của Canvas để tối ưu VR
            if (m_TargetAlpha > 0f && !m_SmokeCanvasGroup.gameObject.activeSelf)
            {
                // Bật Canvas lên ngay khi mục tiêu > 0
                m_SmokeCanvasGroup.gameObject.SetActive(true);
            }

            float speed = m_CurrentAlpha < m_TargetAlpha ? m_FadeInSpeed : m_FadeOutSpeed;
            m_CurrentAlpha = Mathf.MoveTowards(m_CurrentAlpha, m_TargetAlpha, speed * Time.deltaTime);
            m_SmokeCanvasGroup.alpha = m_CurrentAlpha;

            // Tắt hẳn Canvas đi khi độ mờ đã lùi về 0 (đã trong suốt hoàn toàn)
            if (m_CurrentAlpha <= 0f && m_SmokeCanvasGroup.gameObject.activeSelf)
            {
                m_SmokeCanvasGroup.gameObject.SetActive(false);
            }
        }

        void ApplyFlicker()
        {
            if (m_SmokeCanvasGroup == null || !m_SmokeCanvasGroup.gameObject.activeSelf) return;
            float flicker = Mathf.Sin(Time.time * m_FlickerSpeed * Mathf.PI * 2f) * m_FlickerAmplitude;
            m_SmokeCanvasGroup.alpha = Mathf.Clamp01(m_CurrentAlpha + flicker);
        }

        // ------------------------------------------------------------------ //
        //  IEnumerator FlashDangerVignette (Giữ nguyên như cũ)
        // ------------------------------------------------------------------ //
        IEnumerator FlashDangerVignette(float intensity)
        {
            if (m_DangerVignetteImage == null) yield break;

            var baseColor = m_DangerVignetteImage.color;
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                var c = m_DangerVignetteImage.color;
                c.a = Mathf.Lerp(0f, intensity * 0.7f, t / 0.3f);
                m_DangerVignetteImage.color = c;
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                var c = m_DangerVignetteImage.color;
                c.a = Mathf.Lerp(intensity * 0.7f, 0f, t / 0.5f);
                m_DangerVignetteImage.color = c;
                yield return null;
            }

            var finalColor = m_DangerVignetteImage.color;
            finalColor.a = 0f;
            m_DangerVignetteImage.color = finalColor;
        }
    }
}
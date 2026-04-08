using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VRPCCC.Player
{
    /// <summary>
    /// Hiệu ứng màn hình khi người chơi ở trong vùng khói.
    /// Sử dụng Canvas Overlay với Image (khói) + CanvasGroup (opacity).
    /// Kết hợp với DuckingDetector: khi cúi người → khói mờ dần; khi đứng → khói đậm dần.
    ///
    /// Setup:
    ///   1. Tạo Canvas (Render Mode: World Space hoặc Screen Space - Camera) con của Main Camera.
    ///   2. Thêm Image (fullscreen, texture khói hoặc màu xám đen). 
    ///   3. Gán Image và CanvasGroup vào script này.
    ///   4. Kết nối DuckingDetector.OnDuckStart → SmokeVisionEffect.OnPlayerDuck()
    ///              DuckingDetector.OnDuckEnd   → SmokeVisionEffect.OnPlayerStand()
    ///              SmokeZone.OnPlayerEnterSmoke → SmokeVisionEffect.BeginSmokeExposure()
    ///              SmokeZone.OnPlayerExitSmoke  → SmokeVisionEffect.EndSmokeExposure()
    /// </summary>
    public class SmokeVisionEffect : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Inspector Fields
        // ------------------------------------------------------------------ //

        [Header("UI References")]
        [Tooltip("CanvasGroup bao quanh Image khói để điều khiển alpha.")]
        [SerializeField] CanvasGroup m_SmokeCanvasGroup;

        [Tooltip("Image phủ màn hình (khói). Nên là màu xám/đen hoặc texture khói.")]
        [SerializeField] Image m_SmokeImage;

        [Header("Màu Khói")]
        [Tooltip("Màu khói khi ở mức mặc định.")]
        [SerializeField] Color m_SmokeColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        [Tooltip("Màu khi khói rất dày (nguy hiểm cao).")]
        [SerializeField] Color m_DangerColor = new Color(0.05f, 0.05f, 0.05f, 1f);

        [Header("Mức Opacity Khói")]
        [Tooltip("Alpha khi người chơi đứng thẳng trong khói (0-1). Mặc định: gần như không thấy gì.")]
        [Range(0f, 1f)]
        [SerializeField] float m_StandingAlpha = 0.85f;

        [Tooltip("Alpha khi người chơi cúi người né khói. Mặc định: trong suốt (an toàn).")]
        [Range(0f, 1f)]
        [SerializeField] float m_DuckingAlpha = 0.05f;

        [Tooltip("Tốc độ làm đậm màn hình khi vào khói (alpha/giây).")]
        [SerializeField] float m_FadeInSpeed = 1.2f;

        [Tooltip("Tốc độ làm mờ khi cúi người hoặc thoát khói (alpha/giây).")]
        [SerializeField] float m_FadeOutSpeed = 2.5f;

        [Header("Hiệu Ứng Nhấp Nháy")]
        [Tooltip("Khi người chơi ở trong khói, màn hình sẽ nhấp nháy nhẹ (giả lập cay mắt).")]
        [SerializeField] bool m_EnableFlicker = true;

        [Tooltip("Biên độ nhấp nháy (thêm/bớt vào alpha hiện tại).")]
        [Range(0f, 0.15f)]
        [SerializeField] float m_FlickerAmplitude = 0.05f;

        [Tooltip("Tốc độ nhấp nháy (Hz).")]
        [Range(0.5f, 10f)]
        [SerializeField] float m_FlickerSpeed = 3f;

        [Header("Hiệu Ứng Vignette Cảnh Báo")]
        [Tooltip("Hiệu ứng đỏ viền khi sắp hết oxygen / nguy hiểm cao.")]
        [SerializeField] Image m_DangerVignetteImage;

        // ------------------------------------------------------------------ //
        //  Runtime State
        // ------------------------------------------------------------------ //

        float m_TargetAlpha;        // alpha mục tiêu
        float m_CurrentAlpha;       // alpha hiện tại
        float m_SmokeDensity;       // mật độ khói từ SmokeZone (0-1)
        bool m_InSmokeZone;         // đang trong vùng khói
        bool m_IsDucking;           // đang cúi người

        Coroutine m_DangerFlashCoroutine;

        // ------------------------------------------------------------------ //
        //  Unity Lifecycle
        // ------------------------------------------------------------------ //

        void Awake()
        {
            // Khởi tạo trạng thái ban đầu: không có khói
            if (m_SmokeCanvasGroup != null)
                m_SmokeCanvasGroup.alpha = 0f;

            m_CurrentAlpha = 0f;
            m_TargetAlpha = 0f;

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
        //  Public API — kết nối với Events của DuckingDetector & SmokeZone
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Gọi khi DuckingDetector.OnDuckStart kích hoạt.
        /// Người chơi CÚI NGƯỜI — mờ màn hình khói dần.
        /// </summary>
        public void OnPlayerDuck()
        {
            m_IsDucking = true;
            Debug.Log("[SmokeVisionEffect] Cúi người → Giảm hiệu ứng khói.");
        }

        /// <summary>
        /// Gọi khi DuckingDetector.OnDuckEnd kích hoạt.
        /// Người chơi ĐỨNG DẬY — tăng hiệu ứng khói trở lại (nếu còn trong zone).
        /// </summary>
        public void OnPlayerStand()
        {
            m_IsDucking = false;
            Debug.Log("[SmokeVisionEffect] Đứng dậy → Tăng hiệu ứng khói.");
        }

        /// <summary>
        /// Gọi khi SmokeZone.OnPlayerEnterSmoke kích hoạt.
        /// Truyền vào mật độ khói (0-1).
        /// </summary>
        public void BeginSmokeExposure(float density)
        {
            m_InSmokeZone = true;
            m_SmokeDensity = density;

            // Đổi màu theo mật độ
            if (m_SmokeImage != null)
                m_SmokeImage.color = Color.Lerp(m_SmokeColor, m_DangerColor, density);

            Debug.Log($"[SmokeVisionEffect] Vào vùng khói (density={density:F2})");
        }

        /// <summary>
        /// Gọi khi SmokeZone.OnPlayerExitSmoke kích hoạt.
        /// Người chơi thoát vùng khói (cúi thấp hoặc ra khỏi zone).
        /// </summary>
        public void EndSmokeExposure()
        {
            m_InSmokeZone = false;
            m_SmokeDensity = 0f;
            Debug.Log("[SmokeVisionEffect] Thoát vùng khói.");
        }

        /// <summary>
        /// Kích hoạt hiệu ứng vignette đỏ cảnh báo nguy hiểm cao.
        /// </summary>
        public void TriggerDangerWarning(float intensity = 1f)
        {
            if (m_DangerVignetteImage == null) return;

            if (m_DangerFlashCoroutine != null)
                StopCoroutine(m_DangerFlashCoroutine);

            m_DangerFlashCoroutine = StartCoroutine(FlashDangerVignette(intensity));
        }

        /// <summary>Tắt hiệu ứng tức thì (dùng khi kịch bản kết thúc).</summary>
        public void ClearEffect()
        {
            m_InSmokeZone = false;
            m_IsDucking = false;
            m_SmokeDensity = 0f;
            m_TargetAlpha = 0f;
        }

        // ------------------------------------------------------------------ //
        //  Internal Logic
        // ------------------------------------------------------------------ //

        void UpdateAlphaTarget()
        {
            if (!m_InSmokeZone)
            {
                // Ngoài vùng khói → không hiệu ứng
                m_TargetAlpha = 0f;
            }
            else if (m_IsDucking)
            {
                // Cúi người trong vùng khói → hiệu ứng rất nhẹ (gần như thoát)
                m_TargetAlpha = m_DuckingAlpha * m_SmokeDensity;
            }
            else
            {
                // Đứng thẳng trong vùng khói → hiệu ứng đầy đủ theo density
                m_TargetAlpha = m_StandingAlpha * m_SmokeDensity;
            }
        }

        void SmoothAlpha()
        {
            if (m_SmokeCanvasGroup == null) return;

            float speed = m_CurrentAlpha < m_TargetAlpha ? m_FadeInSpeed : m_FadeOutSpeed;
            m_CurrentAlpha = Mathf.MoveTowards(m_CurrentAlpha, m_TargetAlpha, speed * Time.deltaTime);
            m_SmokeCanvasGroup.alpha = m_CurrentAlpha;
        }

        void ApplyFlicker()
        {
            if (m_SmokeCanvasGroup == null) return;
            float flicker = Mathf.Sin(Time.time * m_FlickerSpeed * Mathf.PI * 2f) * m_FlickerAmplitude;
            m_SmokeCanvasGroup.alpha = Mathf.Clamp01(m_CurrentAlpha + flicker);
        }

        IEnumerator FlashDangerVignette(float intensity)
        {
            if (m_DangerVignetteImage == null) yield break;

            var baseColor = m_DangerVignetteImage.color;

            // Flash in
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

            // Flash out
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

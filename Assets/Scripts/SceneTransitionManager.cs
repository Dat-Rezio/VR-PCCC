using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace VRPCCC.Core
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Cài đặt")]
        [Tooltip("Thời gian chờ trước khi chuyển scene (giây)")]
        public float delayTime = 1.5f;

        /// <summary>
        /// Chuyển đến một Scene cụ thể bằng tên (Chuyển ngay lập tức)
        /// </summary>
        public void LoadSceneByName(string sceneName)
        {
            Debug.Log($"[SceneTransition] Đang tải scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Chuyển đến Scene tiếp theo dựa theo số thứ tự trong Build Settings
        /// </summary>
        public void LoadNextScene()
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"[SceneTransition] Đang tải scene thứ tự: {nextIndex}");
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("[SceneTransition] Đây đã là Scene cuối cùng trong danh sách Build Settings!");
            }
        }

        /// <summary>
        /// Tải lại Scene hiện tại (Dùng cho nút Chơi lại / Reset)
        /// </summary>
        public void ReloadCurrentScene()
        {
            Debug.Log("[SceneTransition] Đang tải lại scene hiện tại...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Thoát hoàn toàn game (Dùng cho nút Quit)
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[SceneTransition] Đã thoát game!");
            Application.Quit();
        }

        // ---------------------------------------------------------
        // CÁC HÀM CÓ ĐỘ TRỄ (TỐT CHO VR)
        // ---------------------------------------------------------

        public void LoadSceneByNameDelayed(string sceneName)
        {
            StartCoroutine(LoadDelayedCoroutine(sceneName));
        }

        public void ReloadCurrentSceneDelayed()
        {
            StartCoroutine(LoadIndexDelayedCoroutine(SceneManager.GetActiveScene().buildIndex));
        }

        private IEnumerator LoadDelayedCoroutine(string sceneName)
        {
            // Nếu bạn có script Fade màn hình đen, hãy gọi nó ở đây
            yield return new WaitForSeconds(delayTime);
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator LoadIndexDelayedCoroutine(int buildIndex)
        {
            yield return new WaitForSeconds(delayTime);
            SceneManager.LoadScene(buildIndex);
        }
    }
}
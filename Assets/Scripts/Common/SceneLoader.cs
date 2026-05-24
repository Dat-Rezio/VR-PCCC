using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Cấu hình Scene")]
    [Tooltip("Nhập tên Scene bạn muốn chuyển đến. Chú ý: Cần add Scene vào Build Settings.")]
    public string sceneToLoad;

    /// <summary>
    /// Chuyển scene dựa trên tên đã nhập ở biến sceneToLoad trong Inspector.
    /// Phù hợp khi bạn gắn script này trực tiếp vào từng nút bấm.
    /// </summary>
    public void LoadConfiguredScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log("Đang tải Scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Lỗi: Chưa nhập tên Scene trong script SceneLoader!");
        }
    }

    /// <summary>
    /// Chuyển scene bằng cách nhận tên trực tiếp từ sự kiện OnClick() của UI.
    /// Phù hợp khi bạn để script này ở một GameObject quản lý chung (GameManager).
    /// </summary>
    /// <param name="sceneName">Tên của Scene cần tải</param>
    public void LoadSceneByName(string sceneName)
    {
        Debug.Log("Đang tải Scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Thêm thư viện để nhận diện nút bấm VR

public class VRMenuManager : MonoBehaviour
{
    [Header("Menu Settings")]
    [Tooltip("Kéo GameObject chứa Canvas Menu của bạn vào đây")]
    public GameObject menuCanvas; 
    
    [Tooltip("Nhập đúng tên Scene của MenuHub vào đây")]
    public string menuHubSceneName = "MenuHub";

    [Header("Input Settings")]
    [Tooltip("Gán nút bấm (Input Action) trên tay cầm VR để bật/tắt Menu")]
    public InputActionProperty toggleMenuInput;

    private void OnEnable()
    {
        // Kích hoạt lắng nghe nút bấm khi Script bật
        toggleMenuInput.action.Enable();
        // Gắn sự kiện: khi nút được nhấn (performed) thì gọi hàm ToggleMenu
        toggleMenuInput.action.performed += ToggleMenu;
    }

    private void OnDisable()
    {
        // Hủy lắng nghe khi Script tắt để tránh lỗi rò rỉ bộ nhớ
        toggleMenuInput.action.performed -= ToggleMenu;
        toggleMenuInput.action.Disable();
    }

    private void Start()
    {
        // Tuỳ chọn: Tắt Menu ngay khi mới vào game (bỏ comment dòng dưới nếu muốn)
        // if (menuCanvas != null) menuCanvas.SetActive(false);
    }

    // Hàm xử lý bật/tắt Menu
    private void ToggleMenu(InputAction.CallbackContext context)
    {
        if (menuCanvas != null)
        {
            // Kiểm tra xem Canvas đang bật hay tắt, và đảo ngược trạng thái đó
            bool isActive = menuCanvas.activeSelf;
            menuCanvas.SetActive(!isActive);
        }
        else
        {
            Debug.LogWarning("Bạn chưa gán Canvas vào script VRMenuManager!");
        }
    }

    // Hàm load lại scene hiện tại
    public void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    // Hàm quay về MenuHub
    public void GoToMenuHub()
    {
        SceneManager.LoadScene(menuHubSceneName);
    }
}
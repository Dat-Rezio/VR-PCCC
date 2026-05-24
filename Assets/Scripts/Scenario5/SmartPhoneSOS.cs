using UnityEngine;
using TMPro; // Bắt buộc thêm thư viện này để điều khiển chữ TextMeshPro

[RequireComponent(typeof(AudioSource))]
public class SmartphoneSOS : MonoBehaviour
{
    [Header("Cài đặt Đèn Flash")]
    public GameObject flashLight;
    [Tooltip("Kéo object Text (TMP) nằm bên trong nút Flash vào đây")]
    public TextMeshProUGUI flashButtonText; 
    public bool isFlashOn = false;

    [Header("Âm thanh Cuộc gọi")]
    public AudioClip operatorVoice;
    private AudioSource audioSource;
    public bool isCalling = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Đảm bảo đèn tắt lúc đầu và set đúng chữ
        if (flashLight != null) flashLight.SetActive(false);
        UpdateFlashButtonUI();
    }

    // --- Hàm xử lý bật/tắt Flashlight (Gắn vào sự kiện OnClick của Button) ---
    public void ToggleFlashlightFromUI()
    {
        isFlashOn = !isFlashOn;
        
        // Bật/tắt nguồn sáng
        if (flashLight != null)
        {
            flashLight.SetActive(isFlashOn);
        }

        // Cập nhật chữ trên màn hình
        UpdateFlashButtonUI();
    }

    private void UpdateFlashButtonUI()
    {
        if (flashButtonText != null)
        {
            if (isFlashOn)
            {
                flashButtonText.text = "Tắt Flash";
                flashButtonText.color = Color.red; // (Tùy chọn) Đổi sang chữ đỏ khi đang bật
            }
            else
            {
                flashButtonText.text = "Bật Flash";
                flashButtonText.color = Color.black; // Trở lại màu trắng khi tắt
            }
        }
    }

    // --- Hàm xử lý Gọi 114 giữ nguyên ---
    public void Call114()
    {
        if (!isCalling)
        {
            isCalling = true;
            Debug.Log("Đang kết nối 114...");
            Scenario5Manager.Instance.CompleteTask(1); // Hoàn thành Task 1 khi gọi 114 thành công
            
            if (audioSource != null && operatorVoice != null)
            {
                audioSource.clip = operatorVoice;
                audioSource.Play();
            }
        }
    }
}
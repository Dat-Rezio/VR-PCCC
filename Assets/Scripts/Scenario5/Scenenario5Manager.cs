using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Thêm thư viện quản lý Scene

public class Scenario5Manager : MonoBehaviour
{
    public static Scenario5Manager Instance; 

    [Header("UI Cài đặt")]
    public TextMeshProUGUI instructionText;
    [Tooltip("Panel hiện ra khi kết thúc kịch bản")]
    public GameObject completionPanel; 
    [Tooltip("Tên của Scene bạn muốn chuyển tới (VD: MainMenu)")]
    public string nextSceneName = "MainMenu";

    [Header("Âm thanh Nền (Background)")]
    public AudioSource fireAlarmSource;
    public AudioSource bgmSource;
    
    [Header("Âm thanh Kết thúc (Finale)")]
    public AudioSource rescueSirenSource;
    public bool stopFireAlarmOnWin = true;

    [Header("Sự kiện Kịch bản")]
    public UnityEvent[] onTaskStartEvents; 

    private int currentTaskIndex = 0;

    private string[] instructions = new string[]
    {
        "Có cháy! Hãy rời khỏi nhà ngay lập tức!\n <color=green>Kiểm tra nhiệt độ cửa</color> trước khi lao ra ngoài.", // Task 0: Yêu cầu sờ cửa
        "Cửa rất nóng, bên ngoài rất nguy hiểm!\n Hãy <color=green>gọi 114</color> và cố gắng chờ cứu hộ.", // Task 1: Yêu cầu bấm gọi 114
        "Đã gọi cứu hộ thành công!\n Hãy <color=green>đảm bảo an toàn cho bản thân</color> trước.\n Khói đang tràn vào nhà, chặn khói từ bên ngoài tràn vào bằng khăn ướt.", // Task 2: Yêu cầu nhúng khăn
        "Khăn đã được nhúng ướt!\n Hãy mang chiếc khăn ướt ra chặn kín khe cửa chính để cản khói.", // Task 3: Yêu cầu chặn khe cửa
        "Khói đã được chặn!\n Hãy <color=green> cầm điện thoại ra ban công bật đèn Flash </color> để ra hiệu cầu cứu.", // Task 4: Yêu cầu ra ban công bật flash
        "Bạn đã sống sót!\n Lực lượng cứu hộ đã giải cứu bạn thành công." // Task 5: Hoàn thành
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Tắt Panel hoàn thành và bật Text hướng dẫn lúc mới vào game
        if (completionPanel != null) completionPanel.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(true);

        UpdateInstructionUI();
        StartBackgroundAudio();
    }

    private void StartBackgroundAudio()
    {
        if (fireAlarmSource != null)
        {
            fireAlarmSource.loop = true;
            fireAlarmSource.Play();
        }
        
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void CompleteTask(int taskIndexToComplete)
    {
        if (currentTaskIndex == taskIndexToComplete)
        {
            currentTaskIndex++; 
            UpdateInstructionUI();
            
            // Xử lý riêng khi hoàn thành nhiệm vụ cuối cùng (Task 4 xong -> index lên 5)
            if (currentTaskIndex == 5)
            {
                PlayFinaleAudio();
                ShowCompletionPanel();
            }
        }
    }

    private void PlayFinaleAudio()
    {
        if (rescueSirenSource != null)
        {
            rescueSirenSource.Play();
        }

        if (stopFireAlarmOnWin && fireAlarmSource != null)
        {
            fireAlarmSource.Stop();
        }

        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    private void ShowCompletionPanel()
    {
        // Ẩn dòng chữ hướng dẫn đi cho gọn
        if (instructionText != null) instructionText.gameObject.SetActive(false);

        // Bật Panel chứa nút chuyển Scene
        if (completionPanel != null) completionPanel.SetActive(true);
    }

    // Hàm này sẽ được gán vào nút bấm trên giao diện
    public void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Chưa điền tên Scene tiếp theo!");
        }
    }

    private void UpdateInstructionUI()
    {
        if (instructionText != null && currentTaskIndex < instructions.Length)
        {
            instructionText.text = "NHIỆM VỤ:\n" + instructions[currentTaskIndex];
        }

        if (currentTaskIndex < onTaskStartEvents.Length && onTaskStartEvents[currentTaskIndex] != null)
        {
            onTaskStartEvents[currentTaskIndex].Invoke();
        }
    }
}
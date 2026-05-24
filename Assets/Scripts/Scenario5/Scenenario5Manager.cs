using UnityEngine;
using TMPro;

public class Scenario5Manager : MonoBehaviour
{
    public static Scenario5Manager Instance; 

    [Header("UI Cài đặt")]
    public TextMeshProUGUI instructionText;

    private int currentTaskIndex = 0;

    // Mảng 6 câu lệnh đã được tinh chỉnh lại logic dẫn dắt
    private string[] instructions = new string[]
    {
        "Có cháy! Hãy rời khỏi nhà ngay lập tức!\n Kiểm tra nhiệt độ cửa trước khi lao ra ngoài.", // Task 0: Yêu cầu sờ cửa
        "Cửa rất nóng, bên ngoài rất nguy hiểm!\n Hãy gọi 114 và cố gắng chờ cứu hộ.", // Task 1: Yêu cầu bấm gọi 114
        "Đã gọi cứu hộ thành công!\n Hãy đảm bảo an toàn cho bản thân trước.\n Khói đang tràn vào nhà, chặn khói từ bên ngoài tràn vào bằng khăn ướt.", // Task 2: Yêu cầu nhúng khăn
        "Khăn đã được nhúng ướt!\n Hãy mang chiếc khăn ướt ra chặn kín khe cửa chính để cản khói.", // Task 3: Yêu cầu chặn khe cửa
        "Khói đã được chặn!\n Hãy cầm điện thoại ra ban công bật đèn Flash để ra hiệu cầu cứu.", // Task 4: Yêu cầu ra ban công bật flash
        "Bạn đã sống sót!\n Lực lượng cứu hộ đã giải cứu bạn thành công." // Task 5: Hoàn thành
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateInstructionUI();
    }

    public void CompleteTask(int taskIndexToComplete)
    {
        if (currentTaskIndex == taskIndexToComplete)
        {
            currentTaskIndex++; 
            UpdateInstructionUI();
            Debug.Log("Đã hoàn thành Task: " + taskIndexToComplete);
        }
    }

    private void UpdateInstructionUI()
    {
        if (instructionText != null && currentTaskIndex < instructions.Length)
        {
            instructionText.text = "NHIỆM VỤ:\n" + instructions[currentTaskIndex];
        }
    }
}
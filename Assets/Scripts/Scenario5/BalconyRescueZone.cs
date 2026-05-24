using UnityEngine;

public class BalconyRescueZone : MonoBehaviour
{
    [Header("Tham chiếu Hệ thống")]
    [Tooltip("Kéo object Điện thoại vào đây")]
    public SmartphoneSOS playerPhone;

    [Header("Cài đặt Cứu hộ")]
    [Tooltip("Thời gian (giây) cần giữ đèn Flash nháy ở ban công")]
    public float signalTimeRequired = 3f; 
    
    [Header("Hiệu ứng Kết thúc")]
    [Tooltip("File âm thanh tiếng còi xe cứu hỏa vọng đến")]
    public AudioSource rescueAudio;

    private float currentSignalTime = 0f;
    private bool isPlayerInZone = false;
    private bool hasWon = false;

    private void OnTriggerEnter(Collider other)
    {
        // Nhận diện phần đầu (Camera VR) của người chơi bước vào ban công
        if (other.CompareTag("MainCamera"))
        {
            isPlayerInZone = true;
            Debug.Log("Người chơi đã ra ban công!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            isPlayerInZone = false;
            currentSignalTime = 0f; // Reset thời gian nếu chạy trở lại vào nhà
            Debug.Log("Người chơi đã rời ban công!");
        }
    }

    void Update()
    {
        // Nếu đã thắng rồi thì không kiểm tra nữa
        if (hasWon) return;

        // Nếu người chơi ĐANG Ở BAN CÔNG và ĐIỆN THOẠI ĐANG BẬT FLASH và đã GỌI 114
        if (isPlayerInZone && playerPhone != null && playerPhone.isFlashOn && playerPhone.isCalling)
        {
            currentSignalTime += Time.deltaTime; // Tăng dần thời gian đếm ngược
            
            if (currentSignalTime >= signalTimeRequired)
            {
                Scenario5Manager.Instance.CompleteTask(4); // Hoàn thành Task 4 khi phát tín hiệu cứu hộ thành công
                TriggerWinSequence();
            }
        }
        else
        {
            // Nếu lỡ tay tắt đèn thì thời gian reset về 0
            currentSignalTime = 0f; 
        }
    }

    private void TriggerWinSequence()
    {
        hasWon = true;
        Debug.Log("SỨ MỆNH HOÀN THÀNH: Đã phát tín hiệu cứu hộ thành công!");

        // Phát tiếng còi xe chữa cháy
        if (rescueAudio != null)
        {
            rescueAudio.Play();
        }

        Scenario5Manager.Instance.CompleteTask(5); // Hoàn thành Task 5 khi hoàn thành trò chơi

        // Tùy chọn: Bạn có thể gọi GameManager ở đây để hiện UI chúc mừng, 
        // hoặc làm hiệu ứng đèn pha chói lóa rọi thẳng vào ban công.
    }
}
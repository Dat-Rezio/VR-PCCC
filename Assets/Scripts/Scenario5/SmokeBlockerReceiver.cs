using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class SmokeBlockerReceiver : MonoBehaviour
{
    [Header("Hiệu ứng Khói")]
    [Tooltip("Kéo Particle System khói đen vào đây")]
    public GameObject smokeEffect;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor;

    void Awake()
    {
        socketInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    void OnEnable()
    {
        // Lắng nghe sự kiện khi có vật phẩm gắn vào Socket
        socketInteractor.selectEntered.AddListener(OnItemSnapped);
        
        // Lắng nghe sự kiện khi vật phẩm bị lấy ra khỏi Socket
        socketInteractor.selectExited.AddListener(OnItemRemoved);
    }

    void OnDisable()
    {
        socketInteractor.selectEntered.RemoveListener(OnItemSnapped);
        socketInteractor.selectExited.RemoveListener(OnItemRemoved);
    }

    private void OnItemSnapped(SelectEnterEventArgs args)
    {
        // Lấy object vừa được gắn vào
        GameObject snappedObject = args.interactableObject.transform.gameObject;

        // Kiểm tra xem object đó có script TowelItem không
        TowelItem towel = snappedObject.GetComponent<TowelItem>();

        if (towel != null)
        {
            if (towel.isWet)
            {
                Debug.Log("Khăn ướt đã chèn cửa! Cản khói thành công.");
                Scenario5Manager.Instance.CompleteTask(3); // Hoàn thành Task 3 khi chặn khe cửa thành công
                // Tắt khói
                if (smokeEffect != null) smokeEffect.SetActive(false);
                
                // (Tùy chọn) Gọi hàm dừng giảm thanh Oxy ở đây
            }
            else
            {
                Debug.Log("Sai lầm: Khăn khô không thể cản được khói và nhiệt!");
                // Để nguyên khói, có thể thêm hiệu ứng lửa cháy lan vào cái khăn khô
            }
        }
    }

    private void OnItemRemoved(SelectExitEventArgs args)
    {
        // Nếu người chơi rút khăn ra, bật lại khói
        if (smokeEffect != null) smokeEffect.SetActive(true);
    }
}
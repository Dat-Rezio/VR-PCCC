using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

public class PlayerSpawner : MonoBehaviour
{
    public XROrigin xrOrigin;
    public Transform spawnPoint;

    void Start()
    {
        // Bắt đầu tiến trình đợi thay vì chạy ngay lập tức
        StartCoroutine(SpawnPlayerRoutine());
    }

    IEnumerator SpawnPlayerRoutine()
    {
        // Đợi 0.1 giây để đảm bảo hệ thống tracking của kính VR đã hoàn toàn kích hoạt
        yield return new WaitForSeconds(0.1f); 

        if (xrOrigin != null && spawnPoint != null)
        {
            // 1. Tìm xem có CharacterController không, nếu có thì tạm tắt đi
            CharacterController cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) 
            {
                cc.enabled = false;
            }

            // 2. Dịch chuyển XR Origin về đúng vị trí SpawnPoint
            xrOrigin.MoveCameraToWorldLocation(spawnPoint.position);
            xrOrigin.MatchOriginUpCameraForward(spawnPoint.up, spawnPoint.forward);

            // 3. Đợi thêm 1 frame cho vật lý ổn định rồi bật lại Character Controller
            yield return new WaitForEndOfFrame();
            if (cc != null) 
            {
                cc.enabled = true;
            }
        }
    }
}
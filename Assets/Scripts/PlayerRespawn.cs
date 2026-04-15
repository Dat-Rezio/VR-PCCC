using UnityEngine;


namespace VRPCCC.Player
{
    public class PlayerRespawn : MonoBehaviour
    {
        [Header("Cấu hình Dịch chuyển")]
        [Tooltip("Kéo Teleportation Provider từ XR Origin vào đây")]
        public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider;
        
        [Tooltip("Vị trí xuất phát điểm để người chơi chơi lại từ đầu")]
        public Transform respawnPoint;

        [Header("Hệ thống Sức khỏe")]
        [Tooltip("Kéo script SmokeHealthSystem vào đây để reset lại oxy")]
        public SmokeHealthSystem healthSystem;

        /// <summary>
        /// Hàm này sẽ được gọi khi sự kiện OnKnockout kích hoạt
        /// </summary>
        public void Respawn()
        {
            if (teleportationProvider != null && respawnPoint != null)
            {
                // Thực hiện dịch chuyển về điểm xuất phát
                UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest()
                {
                    destinationPosition = respawnPoint.position,
                    destinationRotation = respawnPoint.rotation,
                    matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward
                };
                
                teleportationProvider.QueueTeleportRequest(request);
                Debug.Log("[PlayerRespawn] Người chơi đã ngất. Dịch chuyển về điểm xuất phát.");
            }

            // Hồi phục lại toàn bộ Oxy và tắt trạng thái ngất
            if (healthSystem != null)
            {
                healthSystem.ResetOxygen();
                Debug.Log("[PlayerRespawn] Đã reset hệ thống Oxygen.");
            }
            
            // TODO: Ở đây bạn có thể thêm logic bật UI thông báo "Bạn đã ngất do ngạt khói. Hãy thử lại!"
        }
    }
}
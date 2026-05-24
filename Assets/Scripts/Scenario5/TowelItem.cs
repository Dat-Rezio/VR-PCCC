using UnityEngine;

using System.Collections.Generic; // Cần thiết để sử dụng List

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class TowelItem : MonoBehaviour
{
    [Header("Trạng thái Khăn")]
    public bool isWet = false;

    [Header("Hiệu ứng Thị giác")]
    [Tooltip("Kéo Object CON (chứa Mesh Renderer của chiếc khăn) vào ô này")]
    public Renderer targetRenderer; 
    [Tooltip("Material ướt chồng lên (nên dùng shader Transparent hoặc Overlay)")]
    public Material wetMaterial; 

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu chạm vào object có Tag là "Water"
        if (other.CompareTag("Water") && !isWet)
        {
            isWet = true;
            Scenario5Manager.Instance.CompleteTask(2); // Hoàn thành Task 2 khi làm ướt khăn
            if (targetRenderer != null && wetMaterial != null)
            {
                // 1. Lấy danh sách các material hiện tại của mesh
                List<Material> currentMaterials = new List<Material>(targetRenderer.materials);
                
                // 2. Thêm material ướt vào cuối danh sách
                currentMaterials.Add(wetMaterial);
                
                // 3. Gán mảng mới trở lại cho Renderer
                targetRenderer.materials = currentMaterials.ToArray();
                
                Debug.Log("Khăn đã được làm ướt (chồng thêm Material thành công)!");
            }
            else
            {
                Debug.LogWarning("Chưa gán Target Renderer hoặc Wet Material!");
            }
        }
    }
}
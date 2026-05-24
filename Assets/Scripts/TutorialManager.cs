using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace VRPCCC.UI
{
    [System.Serializable]
    public class TutorialStep
    {
        [Header("Nội dung")]
        [TextArea(3, 5)]
        public string instructionText;

        [Header("Vị trí hiển thị Canvas")]
        [Tooltip("Kéo một Empty Object làm mốc vị trí vào đây. Nếu để trống, Canvas sẽ giữ nguyên vị trí cũ.")]
        public Transform canvasLocation;

        [Header("Điều kiện qua bài (Action ID)")]
        [Tooltip("Nhập một mã hành động (Ví dụ: 'TouchDoor'). Để trống nếu bước này KHÔNG cần làm gì mà được Next luôn.")]
        public string requiredActionID;

        [Header("Sự kiện Tự động (Tùy chọn)")]
        public UnityEvent onStepStart;
    }

    public class TutorialManager : MonoBehaviour
    {
        [Header("Kết nối UI")]
        public GameObject tutorialCanvas;
        public TextMeshProUGUI instructionTextUI;
        public Button nextButton;
        public Button prevButton;

        [Header("Cài đặt Bật/Tắt (Tùy chọn)")]
        public InputActionReference toggleAction;

        [Header("Danh sách hướng dẫn")]
        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

        private int currentIndex = 0;
        private bool isCurrentActionCompleted = false;
        
        // "Sổ tay" ghi nhớ tất cả những hành động người chơi đã làm (dù làm sớm)
        private HashSet<string> completedActions = new HashSet<string>();

        private void OnEnable()
        {
            if (toggleAction != null)
            {
                toggleAction.action.Enable();
                toggleAction.action.performed += OnToggleTriggered;
            }
        }

        private void OnDisable()
        {
            if (toggleAction != null) toggleAction.action.performed -= OnToggleTriggered;
        }

        private void OnToggleTriggered(InputAction.CallbackContext context)
        {
            if (tutorialCanvas != null) tutorialCanvas.SetActive(!tutorialCanvas.activeSelf);
        }

        void Start()
        {
            if (nextButton != null) nextButton.onClick.AddListener(NextStep);
            if (prevButton != null) prevButton.onClick.AddListener(PrevStep);

            ShowStep(0);
        }

        public void ShowStep(int index)
        {
            if (tutorialSteps == null || tutorialSteps.Count == 0) return;

            currentIndex = Mathf.Clamp(index, 0, tutorialSteps.Count - 1);
            TutorialStep currentStep = tutorialSteps[currentIndex];

            if (instructionTextUI != null) instructionTextUI.text = currentStep.instructionText;

            // DI CHUYỂN CANVAS TỚI VỊ TRÍ ĐÃ CÀI ĐẶT
            if (currentStep.canvasLocation != null && tutorialCanvas != null)
            {
                tutorialCanvas.transform.position = currentStep.canvasLocation.position;
                tutorialCanvas.transform.rotation = currentStep.canvasLocation.rotation;
            }

            // KIỂM TRA ĐIỀU KIỆN
            if (string.IsNullOrEmpty(currentStep.requiredActionID))
            {
                // Nếu ô mã trống -> Không cần làm gì, cho qua luôn
                isCurrentActionCompleted = true;
            }
            else
            {
                // Nếu có mã -> Kiểm tra trong sổ tay xem người chơi đã làm việc này chưa
                isCurrentActionCompleted = completedActions.Contains(currentStep.requiredActionID);
            }

            // Tự động hiện lại bảng nếu nó đang bị ẩn
            if (tutorialCanvas != null && !tutorialCanvas.activeSelf) tutorialCanvas.SetActive(true);

            currentStep.onStepStart?.Invoke();
            UpdateButtons();
        }

        public void NextStep()
        {
            if (currentIndex < tutorialSteps.Count - 1 && isCurrentActionCompleted)
            {
                ShowStep(currentIndex + 1);
            }
        }

        public void PrevStep()
        {
            if (currentIndex > 0) ShowStep(currentIndex - 1);
        }

        public void CompleteAction(string actionID)
        {
            if (string.IsNullOrEmpty(actionID)) return;

            // 1. Ghi vào sổ tay là đã làm
            if (!completedActions.Contains(actionID))
            {
                completedActions.Add(actionID);
            }

            // 2. Kiểm tra xem bước hiện tại có đang chờ hành động này không
            TutorialStep currentStep = tutorialSteps[currentIndex];
            if (currentStep.requiredActionID == actionID)
            {
                isCurrentActionCompleted = true;
                UpdateButtons();
                
                Debug.Log($"[Tutorial] Đã mở khóa bước bằng hành động: {actionID}");
            }

            // Tự động bật bảng hướng dẫn lên để báo hiệu
            if (tutorialCanvas != null && !tutorialCanvas.activeSelf)
            {
                tutorialCanvas.SetActive(true);
            }
        }

        private void UpdateButtons()
        {
            if (prevButton != null) prevButton.interactable = (currentIndex > 0);
            if (nextButton != null)
            {
                bool isLastStep = currentIndex >= tutorialSteps.Count - 1;
                nextButton.interactable = !isLastStep && isCurrentActionCompleted;
            }
        }
    }
}
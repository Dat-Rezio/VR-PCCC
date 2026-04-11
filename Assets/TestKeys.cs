using UnityEngine;
using UnityEngine.InputSystem;   // ← Input System mới
using VRPCCC.Scenario2;

public class TestKeys : MonoBehaviour
{
    [SerializeField] FirefightingScenarioManager manager;
    [SerializeField] FireSource fireSource;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return; // không có bàn phím thì bỏ qua

        if (kb.digit1Key.wasPressedThisFrame) { Debug.Log("TEST: OnFireIgnited");              manager.OnFireIgnited(); }
        if (kb.digit2Key.wasPressedThisFrame) { Debug.Log("TEST: ApproachCabinet");            manager.OnPlayerApproachCabinet(); }
        if (kb.digit3Key.wasPressedThisFrame) { Debug.Log("TEST: GrabCO2");                    manager.OnExtinguisherGrabbed(isCO2: true); }
        if (kb.digit4Key.wasPressedThisFrame) { Debug.Log("TEST: Distance OK");                manager.OnDistanceCheck(2.5f); }
        if (kb.digit5Key.wasPressedThisFrame) { Debug.Log("TEST: PinPulled");                  manager.OnPinPulled(); }
        if (kb.digit6Key.wasPressedThisFrame) { Debug.Log("TEST: Aimed");                      manager.OnNozzleAimedUpdate(true); }
        if (kb.digit7Key.wasPressedThisFrame) { Debug.Log("TEST: Extinguish!");                fireSource.ApplyExtinguishing(99f); }
        if (kb.rKey.wasPressedThisFrame)      { Debug.Log("TEST: Reset");                      manager.ResetScenario(); }
    }
}

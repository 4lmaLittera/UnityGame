using UnityEngine;
using UnityEngine.UI;

public class ThrowCooldownUI : MonoBehaviour
{
    public enum SlotType { Primary, Secondary }

    [SerializeField] private ThrowingSystem _throwingSystem;
    [SerializeField] private Image _cooldownBar;
    [SerializeField] private SlotType _targetSlot;

    void Update()
    {
        if (_throwingSystem == null || _cooldownBar == null) return;

        float ratio = (_targetSlot == SlotType.Primary) 
            ? _throwingSystem.PrimaryCooldownRatio 
            : _throwingSystem.SecondaryCooldownRatio;

        _cooldownBar.fillAmount = ratio;
    }
}
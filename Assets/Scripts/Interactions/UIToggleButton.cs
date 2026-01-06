using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIToggleButton : MonoBehaviour
{
    [SerializeField] UnityEvent onToggleOn;
    [SerializeField] UnityEvent onToggleOff;
    [SerializeField] UIToggleButton[] toggleGroup;
    private bool isToggledOn = false;

    Image buttonImage;

    Color defaultColor;
    [SerializeField] Color toggledOnColor;
    void Start()
    {
        buttonImage = GetComponent<Image>();
        defaultColor = buttonImage.color;
    }

    public void ToggleFromGroup(bool toggleOn)
    {
        this.isToggledOn = toggleOn;
        UpdateToggle();
    }

    void UpdateToggle()
    {
        if (isToggledOn)
        {
            buttonImage.color = defaultColor;
            onToggleOff?.Invoke();
        }
        else
        {
            buttonImage.color = toggledOnColor;
            onToggleOn?.Invoke();
        }

    }

    public void Toggle()
    {
        isToggledOn = !isToggledOn;
        UpdateToggle();
        foreach (var toggle in toggleGroup)
        {
            if (toggle != this)
            {
                toggle.ToggleFromGroup(!isToggledOn);
            }
        }
    }


}

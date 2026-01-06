using UnityEngine;
using UnityEngine.EventSystems;


public class UIMenuControls : MonoBehaviour
{

    [SerializeField] GameObject firstSelectedButton;
    void Start()
    {
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    // Update is called once per frame
    void Update()
    {

    }
}

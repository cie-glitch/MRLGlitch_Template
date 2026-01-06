using UnityEngine;
using System.Collections.Generic;   
using TMPro;

public class StatusText : MonoBehaviour
{
    public static StatusText Instance;

    List<string> statusMessages = new List<string>();
    private int MAX_LINES = 3;

    TMP_Text statusText;

    void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Get the TMP_Text component
        statusText = GetComponent<TMP_Text>();
        if (statusText == null)
        {
            Debug.LogError("StatusText: No TMP_Text component found!");
        }
    }

    // Update is called once per frame
    public void Print(string input)
    {
        statusMessages.Add(input);
        if (statusMessages.Count > MAX_LINES)
        {
            statusMessages.RemoveAt(0);
        }
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = string.Join("\n", statusMessages);
        }
    }
}

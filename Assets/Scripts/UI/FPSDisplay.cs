using UnityEngine;
using TMPro; // Se usi TextMeshPro, altrimenti usa UI.Text

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI fpsText; // Assegna un oggetto UI di tipo TextMeshProUGUI
    private float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }
}
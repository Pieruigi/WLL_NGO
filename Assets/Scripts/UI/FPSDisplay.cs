#if !UNITY_SERVER
using UnityEngine;
using TMPro;
using Unity.Services.Matchmaker.Models;
using WLL_NGO.Netcode; // Se usi TextMeshPro, altrimenti usa UI.Text

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI fpsText; // Assegna un oggetto UI di tipo TextMeshProUGUI
    private float deltaTime = 0.0f;

    public TMP_Text selectedPlayerField;

    void Start()
    {
        //Application.targetFrameRate = -1;
    }
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";

        //selectedPlayerField.text = TeamController.HomeTeam.SelectedPlayer.gameObject.name;
    }
}
#endif
// 12/12/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour
{
    public GameManager gameManager;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnStartGameClicked);
    }

    private void OnStartGameClicked()
    {
        if (gameManager != null)
        {
            gameManager.HostStartGame();
        }
    }
}

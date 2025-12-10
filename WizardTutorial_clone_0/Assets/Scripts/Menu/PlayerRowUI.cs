using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerRowUI : MonoBehaviour
{
    [Header("Anzeige")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bidText;

    [Header("Eingabe (Nur für eigenen Spieler sichtbar)")]
    [SerializeField] private GameObject inputContainer;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button confirmButton;

    private int _tempBid = 0;
    private WizardPlayerData _myData;

    private void Start()
    {
        if (minusButton) minusButton.onClick.AddListener(() => ChangeBid(-1));
        if (plusButton) plusButton.onClick.AddListener(() => ChangeBid(1));
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmBid);
    }

    public void SetupRow(WizardPlayerData data)
    {
        _myData = data;

        // 1. Text Update
        if (nameText) nameText.text = data.playerName.ToString();
        if (scoreText) scoreText.text = data.score.ToString();

        // 2. Ansage & Stiche anzeigen
        if (data.hasBidded)
        {
            if (bidText)
            {
                bidText.text = $"{data.tricksTaken} / {data.currentBid}";

                bool isRoundOver = false;
                if (GameManager.Instance != null)
                {
                    isRoundOver = GameManager.Instance.currentGameState.Value == GameState.Scoring;
                }

                if (isRoundOver)
                {
                    if (data.tricksTaken == data.currentBid) bidText.color = Color.green;
                    else bidText.color = Color.red;
                }
                else
                {
                    bidText.color = Color.white;
                }
            }
        }
        else
        {
            if (bidText) bidText.text = "-";
        }

        // 3. Eingabe-Logik: Wann darf ich tippen?
        bool isMyRow = data.clientId == NetworkManager.Singleton.LocalClientId;
        bool isBiddingPhase = false;

        // --- HIER IST DIE WICHTIGE ÄNDERUNG ---
        bool isMyTurn = false;

        if (GameManager.Instance != null)
        {
            isBiddingPhase = GameManager.Instance.currentGameState.Value == GameState.Bidding;
            // Wir fragen den GameManager: Bin ich dran?
            isMyTurn = GameManager.Instance.IsPlayerTurn(data.clientId);
        }

        // Zeige Buttons nur, wenn ich dran bin UND es meine Zeile ist
        if (isMyRow && isBiddingPhase && !data.hasBidded && isMyTurn)
        {
            if (inputContainer) inputContainer.SetActive(true);
            if (bidText) bidText.text = _tempBid.ToString();
        }
        else
        {
            if (inputContainer) inputContainer.SetActive(false);
        }
    }

    private void ChangeBid(int change)
    {
        _tempBid += change;
        if (_tempBid < 0) _tempBid = 0;
        if (bidText) bidText.text = _tempBid.ToString();
    }

    private void ConfirmBid()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitBidServerRpc(_tempBid);
        }
        if (inputContainer) inputContainer.SetActive(false);
    }
}
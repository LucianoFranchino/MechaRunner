using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pantalla de la tienda (panel del menú principal): pestañas CHARACTERS / UPGRADES, contador de monedas
/// y volver. En personajes muestra la vista previa grande del elegido a la izquierda.
/// </summary>
public class ShopScreen : MonoBehaviour
{
    [Header("Pestañas")]
    [SerializeField] private GameObject charactersPanel;
    [SerializeField] private GameObject upgradesPanel;
    [SerializeField] private Button charactersTab;
    [SerializeField] private Button upgradesTab;
    [SerializeField] private TMP_Text charactersTabLabel;
    [SerializeField] private TMP_Text upgradesTabLabel;
    [SerializeField] private Button backButton;

    [Header("Monedas")]
    [SerializeField] private TMP_Text coinsText;

    [Header("Personajes")]
    [SerializeField] private CharacterCard[] cards;
    [SerializeField] private Image[] previewParts;
    [SerializeField] private TMP_Text previewName;
    [SerializeField] private TMP_Text previewStatus;
    [SerializeField] private GameObject previewLock;

    [Header("Mejoras")]
    [SerializeField] private UpgradeRow[] rows;

    [Header("Estilo de pestañas")]
    [SerializeField] private Color activeTab = new(1f, 0.76f, 0.1f);
    [SerializeField] private Color inactiveTab = new(0.6f, 0.65f, 0.72f);

    private CharacterCard shown;

    private void Awake()
    {
        charactersTab.onClick.AddListener(() => ShowTab(false));
        upgradesTab.onClick.AddListener(() => ShowTab(true));
        backButton.onClick.AddListener(Close);
        foreach (var card in cards) card.Bind(this);
    }

    private void OnEnable()
    {
        SaveSystem.Saved += RefreshAll;
        RefreshAll();
    }

    private void OnDisable()
    {
        SaveSystem.Saved -= RefreshAll;
    }

    private void Update()
    {
        // Atrás de Android / Escape cierra la tienda
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        ShowTab(true);
    }

    public void Close() => gameObject.SetActive(false);

    public void ShowTab(bool upgrades)
    {
        upgradesPanel.SetActive(upgrades);
        charactersPanel.SetActive(!upgrades);
        upgradesTabLabel.color = upgrades ? activeTab : inactiveTab;
        charactersTabLabel.color = upgrades ? inactiveTab : activeTab;
        if (!upgrades && shown == null)
        {
            foreach (var c in cards) if (c.CharacterId == ShopService.SelectedCharacter) shown = c;
            if (shown == null && cards.Length > 0) shown = cards[0];
        }
        RefreshAll();
    }

    public void ShowCharacter(CharacterCard card)
    {
        shown = card;
        // Un personaje disponible se equipa al tocarlo
        var def = card.Def;
        if (def != null && !def.comingSoon && ShopService.IsOwned(def.id)) ShopService.Select(def.id);
        RefreshAll();
    }

    private void RefreshAll()
    {
        coinsText.SetText("{0}", SaveSystem.Data.progress.coins);
        foreach (var row in rows) if (row.isActiveAndEnabled) row.Refresh();
        foreach (var card in cards) card.Refresh(card == shown);

        if (shown != null)
        {
            var def = shown.Def;
            bool soon = def == null || def.comingSoon;
            previewName.SetText(soon ? "???" : def.displayName);
            previewStatus.SetText(soon ? "COMING SOON" : ShopService.SelectedCharacter == def.id ? "EQUIPPED" : "AVAILABLE");
            foreach (var p in previewParts) if (p != null) p.color = soon ? new Color(0.05f, 0.06f, 0.1f, 0.92f) : Color.white;
            if (previewLock != null) previewLock.SetActive(soon);
        }
    }
}

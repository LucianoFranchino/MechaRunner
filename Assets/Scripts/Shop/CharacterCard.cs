using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarjeta de un personaje en la grilla: retrato, nombre y estado (EQUIPPED / AVAILABLE / COMING SOON).
/// Los personajes en desarrollo se muestran como silueta con candado. Tocarla muestra su vista previa.
/// </summary>
public class CharacterCard : MonoBehaviour
{
    [SerializeField] private string characterId;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image[] portraitParts;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Image statusBar;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private Button button;

    private static readonly Color Silhouette = new(0.05f, 0.06f, 0.1f, 0.92f);
    private static readonly Color Equipped = new(0.16f, 0.55f, 0.9f);
    private static readonly Color Available = new(0.2f, 0.7f, 0.35f);
    private static readonly Color Locked = new(0.3f, 0.33f, 0.42f);

    private ShopScreen screen;

    public string CharacterId => characterId;

    public void Bind(ShopScreen owner)
    {
        screen = owner;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => screen.ShowCharacter(this));
    }

    public ShopCatalog.CharacterDef Def
    {
        get
        {
            var c = ShopService.Catalog;
            if (c != null) foreach (var d in c.characters) if (d.id == characterId) return d;
            return null;
        }
    }

    public void Refresh(bool selected)
    {
        var def = Def;
        bool soon = def == null || def.comingSoon;
        bool equipped = !soon && ShopService.SelectedCharacter == characterId;
        nameText.SetText(soon ? "???" : def.displayName);
        statusText.SetText(soon ? "COMING SOON" : equipped ? "EQUIPPED" : "AVAILABLE");
        if (statusBar != null) statusBar.color = soon ? Locked : equipped ? Equipped : Available;
        foreach (var p in portraitParts) if (p != null) p.color = soon ? Silhouette : Color.white;
        if (lockIcon != null) lockIcon.SetActive(soon);
        if (selectionFrame != null) selectionFrame.SetActive(selected);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila de mejora de un power-up: ícono, nombre, descripción, barra de niveles, duración actual → siguiente
/// y botón de compra con el precio (apagado si no alcanza; MAX al llegar al tope).
/// </summary>
public class UpgradeRow : MonoBehaviour
{
    [SerializeField] private PowerUpType type;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Image[] segments;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject priceCoin;
    [SerializeField] private Color filledColor = new(1f, 0.76f, 0.1f);
    [SerializeField] private Color emptyColor = new(0.16f, 0.19f, 0.31f);
    [SerializeField] private Color priceColor = new(1f, 0.76f, 0.1f);
    [SerializeField] private Color cantAffordColor = new(0.9f, 0.28f, 0.23f);
    [SerializeField] private AudioSource buySound;

    public PowerUpType Type => type;

    private void Awake()
    {
        buyButton.onClick.AddListener(Buy);
    }

    private void OnEnable() => Refresh();

    public void Buy()
    {
        if (ShopService.TryBuyUpgrade(type))
        {
            if (buySound != null) { buySound.mute = SaveSystem.Data.settings.sfxMuted; buySound.Play(); }
            Refresh();
        }
    }

    public void Refresh()
    {
        var def = ShopService.Upgrade(type);
        if (def == null) { gameObject.SetActive(false); return; }

        int level = ShopService.Level(type);
        if (icon != null && def.icon != null) icon.sprite = def.icon;
        title.SetText(def.title);
        description.SetText(def.description);
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].gameObject.SetActive(i < def.maxLevel);
            segments[i].color = i < level ? filledColor : emptyColor;
        }

        bool maxed = ShopService.IsMaxed(type);
        int price = ShopService.NextPrice(type);
        durationText.SetText(maxed ? "{0:0}s" : "{0:0}s  >  {1:0}s", def.SecondsAt(level), def.SecondsAt(level + 1));
        priceText.SetText(maxed ? "MAX" : price.ToString());
        if (priceCoin != null) priceCoin.SetActive(!maxed);
        bool affordable = !maxed && ShopService.CanAfford(price);
        buyButton.interactable = affordable;
        // El botón cambia de sprite al apretarlo (no de color): el estado se marca a mano
        if (buyButton.targetGraphic != null) buyButton.targetGraphic.color = affordable || maxed ? Color.white : new Color(0.55f, 0.55f, 0.62f);
        priceText.color = maxed || affordable ? priceColor : cantAffordColor;
    }
}

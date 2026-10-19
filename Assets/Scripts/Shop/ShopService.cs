using System;
using UnityEngine;

/// <summary>
/// Reglas de la tienda: niveles de mejora, precios, compras y personajes. Lee el catálogo
/// (Resources/ShopCatalog) y guarda en SaveSystem (sección shop). Sin UI: la usan la pantalla
/// de la tienda y PowerUpManager (duración de los power-ups).
/// </summary>
public static class ShopService
{
    private static ShopCatalog catalog;

    public static ShopCatalog Catalog
    {
        get
        {
            if (catalog == null) catalog = Resources.Load<ShopCatalog>("ShopCatalog");
            return catalog;
        }
    }

    /// <summary>Algo de la tienda cambió (compra, personaje elegido).</summary>
    public static event Action Changed;

    private static ShopProgress Progress => SaveSystem.Data.shop;

    // ------------------------------------------------------------------ mejoras

    public static ShopCatalog.UpgradeDef Upgrade(PowerUpType type)
    {
        var c = Catalog;
        if (c == null) return null;
        foreach (var u in c.upgrades) if (u.type == type) return u;
        return null;
    }

    public static int Level(PowerUpType type)
    {
        var levels = Progress.upgradeLevels;
        int i = (int)type;
        return levels != null && i < levels.Length ? levels[i] : 0;
    }

    public static bool IsMaxed(PowerUpType type)
    {
        var def = Upgrade(type);
        return def == null || Level(type) >= def.maxLevel;
    }

    /// <summary>Precio del próximo nivel, o -1 si ya está al máximo.</summary>
    public static int NextPrice(PowerUpType type)
    {
        var def = Upgrade(type);
        return def == null || IsMaxed(type) ? -1 : def.PriceFrom(Level(type));
    }

    public static bool CanAfford(int price) => price >= 0 && SaveSystem.Data.progress.coins >= price;

    /// <summary>Multiplicador de duración del power-up según su nivel (1 sin mejoras).</summary>
    public static float DurationMultiplier(PowerUpType type)
    {
        var def = Upgrade(type);
        return def == null ? 1f : 1f + def.bonusPerLevel * Level(type);
    }

    public static bool TryBuyUpgrade(PowerUpType type)
    {
        int price = NextPrice(type);
        if (price < 0 || !CanAfford(price)) return false;

        SaveSystem.Data.progress.coins -= price;
        int i = (int)type;
        var levels = Progress.upgradeLevels ?? new int[0];
        if (levels.Length <= i) Array.Resize(ref levels, i + 1);
        levels[i]++;
        Progress.upgradeLevels = levels;
        SaveSystem.Save();
        Changed?.Invoke();
        return true;
    }

    // ------------------------------------------------------------------ personajes

    public static string SelectedCharacter => string.IsNullOrEmpty(Progress.selectedCharacter) ? "robot" : Progress.selectedCharacter;

    public static bool IsOwned(string id) => Array.IndexOf(Progress.ownedCharacters ?? Array.Empty<string>(), id) >= 0;

    public static bool Select(string id)
    {
        if (!IsOwned(id)) return false;
        Progress.selectedCharacter = id;
        SaveSystem.Save();
        Changed?.Invoke();
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        catalog = null;
        Changed = null;
    }
}

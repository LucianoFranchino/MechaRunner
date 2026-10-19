using System;
using UnityEngine;

/// <summary>
/// Qué se vende en la tienda y a qué precio. Se edita en el Inspector (Assets/Resources/ShopCatalog).
/// </summary>
[CreateAssetMenu(menuName = "MechaRunner/Shop Catalog", fileName = "ShopCatalog")]
public class ShopCatalog : ScriptableObject
{
    [Serializable]
    public class UpgradeDef
    {
        public PowerUpType type;
        public string title = "Power-up";
        [TextArea] public string description = "";
        public Sprite icon;
        [Min(1)] public int maxLevel = 5;
        [Tooltip("Duración extra por nivel, como fracción de la duración base (0.25 = +25% por nivel).")]
        public float bonusPerLevel = 0.25f;
        [Tooltip("Solo para mostrar en la tienda: duración base en segundos.")]
        public float displayBaseSeconds = 8f;
        [Tooltip("Precio del nivel 1; cada nivel siguiente se multiplica por priceGrowth.")]
        [Min(1)] public int basePrice = 200;
        [Min(1f)] public float priceGrowth = 2f;

        /// <summary>Precio para pasar de <paramref name="currentLevel"/> al siguiente (exponencial).</summary>
        public int PriceFrom(int currentLevel) => Mathf.RoundToInt(basePrice * Mathf.Pow(priceGrowth, currentLevel));
        public float SecondsAt(int level) => displayBaseSeconds * (1f + bonusPerLevel * level);
    }

    [Serializable]
    public class CharacterDef
    {
        public string id = "robot";
        public string displayName = "Robot";
        [Tooltip("En desarrollo: se muestra con candado y no se puede comprar.")]
        public bool comingSoon;
        [Min(0)] public int price;
    }

    public UpgradeDef[] upgrades = Array.Empty<UpgradeDef>();
    public CharacterDef[] characters = Array.Empty<CharacterDef>();
}

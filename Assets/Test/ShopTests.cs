using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tienda: precios exponenciales, compras, tope, efecto en la duración real de los power-ups
/// y el flujo de UI desde el menú principal.
/// </summary>
public class ShopTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void RestoreSave()
    {
        TestSaveIsolation.End();
        Time.timeScale = 1f;
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    [Test]
    public void Precios_ExponencialesYDuracionPorNivel()
    {
        var def = ShopService.Upgrade(PowerUpType.CoinMultiplier);
        Assert.IsNotNull(def, "Falta el catálogo en Resources");
        CollectionAssert.AreEqual(new[] { 200, 400, 800, 1600, 3200 }, new[] { def.PriceFrom(0), def.PriceFrom(1), def.PriceFrom(2), def.PriceFrom(3), def.PriceFrom(4) });
        Assert.AreEqual(5, def.maxLevel);
        Assert.AreEqual(8f, def.SecondsAt(0), 1e-4f);
        Assert.AreEqual(18f, def.SecondsAt(5), 1e-4f);
        Assert.AreEqual(1f, ShopService.DurationMultiplier(PowerUpType.CoinMultiplier), 1e-4f);
    }

    [Test]
    public void Comprar_DescuentaSubeDeNivelYGuarda_SinMonedasNo_EnElTopeNo()
    {
        SaveSystem.Data.progress.coins = 1000;
        Assert.IsTrue(ShopService.TryBuyUpgrade(PowerUpType.SuperShoot));
        Assert.AreEqual(800, SaveSystem.Data.progress.coins);
        Assert.AreEqual(1, ShopService.Level(PowerUpType.SuperShoot));
        Assert.AreEqual(1.25f, ShopService.DurationMultiplier(PowerUpType.SuperShoot), 1e-4f);
        Assert.IsFalse(SaveSystem.IsDirty, "La compra se guarda en el momento");

        SaveSystem.Data.progress.coins = 100;
        Assert.IsFalse(ShopService.TryBuyUpgrade(PowerUpType.SuperShoot), "Sin monedas no se compra");
        Assert.AreEqual(100, SaveSystem.Data.progress.coins);

        SaveSystem.Data.shop.upgradeLevels = new[] { 0, 0, 5 };
        SaveSystem.Data.progress.coins = 99999;
        Assert.AreEqual(-1, ShopService.NextPrice(PowerUpType.SuperShoot));
        Assert.IsFalse(ShopService.TryBuyUpgrade(PowerUpType.SuperShoot), "En el tope no se compra más");
    }

    [UnityTest]
    public IEnumerator Mejora_AlargaLaDuracionRealDelPowerUp()
    {
        SaveSystem.Data.shop.upgradeLevels = new int[3];
        SaveSystem.Data.shop.upgradeLevels[(int)PowerUpType.ScoreMultiplier] = 2;
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;

        PowerUpManager.Instance.Activate(PowerUpType.ScoreMultiplier);
        var durations = (Dictionary<PowerUpType, float>)typeof(PowerUpManager).GetField("durations", Private).GetValue(PowerUpManager.Instance);
        Assert.AreEqual(8f * 1.5f, durations[PowerUpType.ScoreMultiplier], 1e-3f, "Nivel 2 = +50% de duración");
    }

    [UnityTest]
    public IEnumerator Menu_BotonShopAbreYComprarActualizaTodo()
    {
        SaveSystem.Data.progress.coins = 300;
        TestScenes.Load("MainMenu");
        yield return null;

        var shopButton = GameObject.Find("Canvas").transform.Find("MainMenu/Shop").GetComponent<Button>();
        shopButton.onClick.Invoke();
        var screen = Object.FindFirstObjectByType<ShopScreen>();
        Assert.IsNotNull(screen, "La tienda no se abrió");

        UpgradeRow coinRow = null;
        foreach (var row in screen.GetComponentsInChildren<UpgradeRow>()) if (row.Type == PowerUpType.CoinMultiplier) coinRow = row;
        var buy = (Button)typeof(UpgradeRow).GetField("buyButton", Private).GetValue(coinRow);
        Assert.IsTrue(buy.interactable, "Con 300 monedas se puede comprar el nivel 1 (200)");

        buy.onClick.Invoke();
        Assert.AreEqual(100, SaveSystem.Data.progress.coins);
        Assert.AreEqual(1, ShopService.Level(PowerUpType.CoinMultiplier));
        Assert.IsFalse(buy.interactable, "Con 100 no alcanza para el nivel 2 (400)");
        var coins = (TMP_Text)typeof(ShopScreen).GetField("coinsText", Private).GetValue(screen);
        Assert.AreEqual("100", coins.text, "El contador de la tienda no se actualizó");

        // Personajes: el Robot equipado y los demás bloqueados
        screen.ShowTab(false);
        foreach (var card in screen.GetComponentsInChildren<CharacterCard>())
        {
            var status = (TMP_Text)typeof(CharacterCard).GetField("statusText", Private).GetValue(card);
            Assert.AreEqual(card.CharacterId == "robot" ? "EQUIPPED" : "COMING SOON", status.text, card.CharacterId);
            screen.ShowCharacter(card); // tocar uno bloqueado no cambia el elegido
        }
        Assert.AreEqual("robot", ShopService.SelectedCharacter);

        screen.Close();
        Assert.IsFalse(screen.gameObject.activeSelf);
    }
}

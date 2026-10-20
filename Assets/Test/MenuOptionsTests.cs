using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Panel OPTIONS del menú principal: los toggles cambian el ajuste guardado y lo aplican
/// a la música persistente y a los efectos del menú (sin AudioManager en la escena).
/// </summary>
public class MenuOptionsTests
{
    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void RestoreSave()
    {
        TestSaveIsolation.End();
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    [UnityTest]
    public IEnumerator Options_AbreYMuteaMusicaYSonido()
    {
        SaveSystem.Data.settings.musicMuted = false;
        SaveSystem.Data.settings.sfxMuted = false;
        TestScenes.Load("MainMenu");
        yield return null;

        var canvas = GameObject.Find("Canvas").transform;
        canvas.Find("MainMenu/Options").GetComponent<Button>().onClick.Invoke();
        var options = canvas.Find("Options");
        Assert.IsTrue(options.gameObject.activeSelf, "El panel OPTIONS no se abrió");

        var click = GameObject.Find("Menu Selection Click").GetComponent<AudioSource>();
        Assert.IsFalse(click.mute);

        options.Find("Panel/MusicToggle").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(SaveSystem.Data.settings.musicMuted);
        Assert.IsTrue(MusicTransition.Source.mute, "La música del menú no se muteó");
        Assert.IsFalse(SaveSystem.IsDirty, "El ajuste se guarda en el momento");
        Assert.IsTrue(options.Find("Panel/MusicToggle/Off").gameObject.activeSelf);

        options.Find("Panel/SoundToggle").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(SaveSystem.Data.settings.sfxMuted);
        Assert.IsTrue(click.mute, "Los efectos del menú no respetan el mute de sonido");

        options.Find("Panel/MusicToggle").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(MusicTransition.Source.mute);

        options.Find("Panel/BackButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(options.gameObject.activeSelf);
    }
}

using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Audio: tono progresivo de las monedas (escala ascendente en rachas) y volumen de la música
/// igual en el menú y en el juego.
/// </summary>
public class AudioFeelTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void Cleanup()
    {
        TestSaveIsolation.End();
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    [UnityTest]
    public IEnumerator Monedas_RachaSubeElTonoYSeReiniciaTrasUnaPausa()
    {
        TestScenes.Load("GameLab");
        yield return null;

        var audio = AudioManager.instance;
        var coin = Resources.FindObjectsOfTypeAll<AudioClip>();
        AudioClip clip = null;
        foreach (var c in coin) if (c.name == "PickUpCoin") clip = c;
        Assert.IsNotNull(clip, "Falta el clip de la moneda");
        var sources = (AudioSource[])typeof(AudioManager).GetField("coinSources", Private).GetValue(audio);

        audio.PlayCoin(clip);
        Assert.AreEqual(0, audio.CoinStep, "La primera moneda empieza la escala");
        float first = sources[0].pitch;
        audio.PlayCoin(clip);
        audio.PlayCoin(clip);
        Assert.AreEqual(2, audio.CoinStep);
        Assert.Greater(sources[1].pitch, first, "La segunda moneda tiene que sonar más aguda");
        Assert.Greater(sources[2].pitch, sources[1].pitch, "La tercera moneda tiene que sonar más aguda");
        Assert.Less(first, 1f, "La primera moneda suena más grave que el clip original");

        yield return new WaitForSecondsRealtime(0.6f);
        audio.PlayCoin(clip);
        Assert.AreEqual(0, audio.CoinStep, "Tras una pausa la racha vuelve a empezar");
    }

    [UnityTest]
    public IEnumerator Musica_MismoVolumenEnMenuYJuego()
    {
        TestScenes.Load("MainMenu");
        yield return null;
        Assert.IsNotNull(MusicTransition.Source);
        float menu = MusicTransition.Source.volume;
        Assert.LessOrEqual(menu, 0.25f, "La música del menú está muy fuerte");

        TestScenes.Load("GameLab");
        yield return null;
        yield return null;
        Assert.AreEqual(menu, MusicTransition.Source.volume, 1e-3f, "Al entrar al juego la música cambia de volumen");
    }

    [Test]
    public void Monedas_PasadoElTopeVariaSinSerMasAgudo()
    {
        const int scale = 8, top = 4;
        var seen = new System.Collections.Generic.HashSet<int>();
        for (int step = 0; step < 60; step++)
        {
            int note = AudioManager.CoinNoteIndex(step, scale, top);
            Assert.LessOrEqual(note, scale - 1, "Nunca más agudo que el tope de la escala");
            if (step < scale) Assert.AreEqual(step, note, "Hasta el tope sube de a una nota");
            else
            {
                Assert.GreaterOrEqual(note, scale - top, "Pasado el tope se mueve entre las notas altas");
                seen.Add(note);
                Assert.AreNotEqual(AudioManager.CoinNoteIndex(step - 1, scale, top), note, "No repite la misma nota seguida");
            }
        }
        Assert.AreEqual(top, seen.Count, "Pasado el tope usa todas las notas altas");
    }
}

using TMPro;
using UnityEngine;

/// <summary>
/// Muestra en el menú principal el récord y las monedas guardados.
/// Se refresca al aparecer y cada vez que se escribe el guardado (ej. desde el menú de debug).
/// </summary>
public class MenuStats : MonoBehaviour
{
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private TMP_Text coinsText;

    private void OnEnable()
    {
        SaveSystem.Saved += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        SaveSystem.Saved -= Refresh;
    }

    public void Refresh()
    {
        var progress = SaveSystem.Data.progress;
        if (bestText != null) bestText.SetText("{0}", Mathf.RoundToInt(progress.bestScore));
        if (coinsText != null) coinsText.SetText("{0}", progress.coins);
    }
}

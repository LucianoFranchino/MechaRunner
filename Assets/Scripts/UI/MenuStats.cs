using TMPro;
using UnityEngine;

/// <summary>
/// Muestra en el menú principal el récord y las monedas ya guardados.
/// Solo lectura de PlayerPrefs: no modifica datos.
/// </summary>
public class MenuStats : MonoBehaviour
{
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private TMP_Text coinsText;

    private void Start()
    {
        if (bestText != null) bestText.SetText("{0}", Mathf.RoundToInt(PlayerPrefs.GetFloat(ScoreManager.HighscoreKey, 0f)));
        if (coinsText != null) coinsText.SetText("{0}", CurrencyService.SavedCoins);
    }
}

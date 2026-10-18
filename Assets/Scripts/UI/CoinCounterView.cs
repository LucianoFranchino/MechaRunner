using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Contador de monedas del HUD (vista): muestra el total de CurrencyService cuando cambia.
/// </summary>
public class CoinCounterView : MonoBehaviour
{
    [FormerlySerializedAs("coinScore")]
    [SerializeField] private TMP_Text valueText;

    private CurrencyService service;

    private void Start()
    {
        service = CurrencyService.Instance;
        if (service == null) return;
        service.Changed += Show;
        Show(service.Coins);
    }

    private void OnDestroy()
    {
        if (service != null) service.Changed -= Show;
    }

    private void Show(int coins)
    {
        valueText.SetText("{0}", coins);
    }
}

using UnityEngine;
using UnityEngine.UI;

public class PoweUpBar : MonoBehaviour
{
    [SerializeField] private PowerUpType type;
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject root;

    private void Start()
    {
        root.SetActive(false);

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Activated += OnActivated;
            PowerUpManager.Instance.Ticked += OnTicked;
            PowerUpManager.Instance.Expired += OnExpired;
        }
    }

    private void OnDestroy()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Activated -= OnActivated;
            PowerUpManager.Instance.Ticked -= OnTicked;
            PowerUpManager.Instance.Expired -= OnExpired;
        }
    }

    private void OnActivated(PowerUpType activatedType, float duration)
    {
        if (activatedType != type) return;
        root.SetActive(true);
        fillImage.fillAmount = 1f;
    }

    private void OnTicked(PowerUpType tickedType, float remaining, float duration)
    {
        if (tickedType != type) return;
        fillImage.fillAmount = remaining / duration;
    }

    private void OnExpired(PowerUpType expiredType)
    {
        if (expiredType != type) return;
        root.SetActive(false);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityBar : MonoBehaviour
{
    [SerializeField] private Text healthText;
    [SerializeField] private Image healthFill;
    public List<GameObject> stars = new();

    private RectTransform rectTransform;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    public void Refresh(int health, int maxHealth, int level)
    {
        maxHealth = Mathf.Max(1, maxHealth);
        health = Mathf.Clamp(health, 0, maxHealth);

        if (healthText != null) healthText.text = $"{health}/{maxHealth}";
        if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(health / (float)maxHealth);

        for (int i = 0; i < stars.Count; i++)
        {
            if (stars[i] != null) stars[i].SetActive(i < level);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatItem : MonoBehaviour
{
    [SerializeField] private Image statItem;
    [SerializeField] private Image statIcon;
    [SerializeField] private Image statIconBackGround;
    [SerializeField] private Image Arrow;
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI baseValueText;
    [SerializeField] private TextMeshProUGUI modifierText;

    [SerializeField] private Color positiveTextColor = new Color(0.32f, 1f, 0.4f, 1f);
    [SerializeField] private Color negativeTextColor = new Color(1f, 0.3f, 0.3f, 1f);

    [SerializeField] private Color positiveArrowColor = new Color(0.32f, 1f, 0.4f, 0.5f);
    [SerializeField] private Color negativeArrowColor = new Color(1f, 0.3f, 0.3f, 0.5f);

    [SerializeField] private Color defaultRowColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color positiveRowColor = new Color(0.1f, 0.5f, 0.2f, 0.3f);
    [SerializeField] private Color negativeRowColor = new Color(0.5f, 0.1f, 0.1f, 0.3f);

    [SerializeField] private Color defaultIconBgColor = new Color(0.15f, 0.2f, 0.3f, 1f);
    [SerializeField] private Color positiveIconBgColor = new Color(0.2f, 0.6f, 0.3f, 1f);
    [SerializeField] private Color negativeIconBgColor = new Color(0.6f, 0.2f, 0.2f, 1f);

    public void Setup(Sprite icon, string statName, float baseVal)
    {
        if (statIcon != null) statIcon.sprite = icon;
        if (statNameText != null) statNameText.text = statName;

        UpdateValue(baseVal, 0);
    }

    public void UpdateValue(float baseVal, float modifier)
    {
        if (baseValueText != null)
        {
            baseValueText.text = (baseVal % 1 == 0) ? baseVal.ToString("F0") : baseVal.ToString("F1");
        }

        if (modifier == 0)
        {
            if (modifierText != null) modifierText.text = "";
            if (statItem != null) statItem.color = defaultRowColor;
            if (statIconBackGround != null) statIconBackGround.color = defaultIconBgColor;

            if (Arrow != null) Arrow.gameObject.SetActive(false);
        }
        else if (modifier > 0)
        {
            if (modifierText != null)
            {
                modifierText.text = "+" + ((modifier % 1 == 0) ? modifier.ToString("F0") : modifier.ToString("F1"));
                modifierText.color = positiveTextColor;
            }

            if (statItem != null) statItem.color = positiveRowColor;
            if (statIconBackGround != null) statIconBackGround.color = positiveIconBgColor;

            if (Arrow != null)
            {
                Arrow.gameObject.SetActive(true);
                Arrow.transform.localEulerAngles = Vector3.zero;
                Arrow.color = positiveArrowColor;
            }
        }
        else
        {
            if (modifierText != null)
            {
                modifierText.text = ((modifier % 1 == 0) ? modifier.ToString("F0") : modifier.ToString("F1"));
                modifierText.color = negativeTextColor;
            }

            if (statItem != null) statItem.color = negativeRowColor;
            if (statIconBackGround != null) statIconBackGround.color = negativeIconBgColor;

            if (Arrow != null)
            {
                Arrow.gameObject.SetActive(true);
                Arrow.transform.localEulerAngles = new Vector3(0, 0, 180f);
                Arrow.color = negativeArrowColor;
            }
        }
    }
}
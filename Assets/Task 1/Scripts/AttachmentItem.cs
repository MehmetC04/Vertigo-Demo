using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AttachmentItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button itemButton;
    [SerializeField] private GameObject highlightBorder;
    [SerializeField] private Image Background;
    [SerializeField] private TextMeshProUGUI nameText;

    public Color Level1 = Color.white;
    public Color Level2 = Color.green;
    public Color Level3 = Color.blue;
    public Color Level4 = Color.magenta;
    public Color Level5 = Color.yellow;

    private AttachmentData myData;
    private Action<AttachmentData, AttachmentItem> onClickCallback;

    public void Setup(AttachmentData data, Action<AttachmentData, AttachmentItem> callback)
    {
        myData = data;
        onClickCallback = callback;

        if (iconImage != null && data.attachmentIcon != null)
        {
            iconImage.sprite = data.attachmentIcon;
        }

        if (nameText != null)
        {
            nameText.text = data.attachmentName;
        }

        if (Background != null)
        {
            switch (data.attachmentLvl)
            {
                case 1: Background.color = Level1; break;
                case 2: Background.color = Level2; break;
                case 3: Background.color = Level3; break;
                case 4: Background.color = Level4; break;
                case 5: Background.color = Level5; break;
            }
        }

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(OnClicked);

        SetHighlight(false);
    }

    private void OnClicked()
    {
        onClickCallback?.Invoke(myData, this);
    }

    public void SetHighlight(bool isSelected)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isSelected);
        }
    }
}
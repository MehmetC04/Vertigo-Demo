using UnityEngine;
using UnityEngine.UI;
using System;

public class CategoryItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button categoryButton;
    [SerializeField] private GameObject highlightBorder;

    private AttachmentCategory myCategory;
    private Action<CategoryItem> onClickCallback;
    private Sprite defaultIconSprite;

    public AttachmentCategory Category => myCategory;

    public void Setup(AttachmentCategory category, Sprite defaultIcon, Action<CategoryItem> callback)
    {
        myCategory = category;
        defaultIconSprite = defaultIcon;
        onClickCallback = callback;

        if (iconImage != null && defaultIcon != null)
        {
            iconImage.sprite = defaultIcon;
        }

        categoryButton.onClick.RemoveAllListeners();
        categoryButton.onClick.AddListener(OnClicked);

        SetHighlight(false);
    }

    private void OnClicked()
    {
        onClickCallback?.Invoke(this);
    }

    public void SetHighlight(bool isSelected)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isSelected);
        }
    }

    public void UpdateIcon(Sprite newIcon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = newIcon != null ? newIcon : defaultIconSprite;
        }
    }
}
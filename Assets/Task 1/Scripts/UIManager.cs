using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public struct CategoryConfig
{
    public AttachmentCategory category;
    public Sprite defaultIcon;
}

[System.Serializable]
public struct StatConfig
{
    public StatType statType;
    public string displayName;
    public Sprite icon;
    public float baseValue;
}

public class UIManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator[] uiAnimators;
    [SerializeField] private Button backgroundButton;

    [Header("UI Element Swapping")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private string defaultTitle = "INVENTORY";
    [SerializeField] private string attachmentTitle = "ATTACHMENTS";
    [SerializeField] private GameObject[] objectsToHideOnOpen;
    [SerializeField] private Button equipButton;

    [Header("Category List")]
    [SerializeField] private Transform categoryListParent;
    [SerializeField] private CategoryItem categoryPrefab;
    [SerializeField] private CategoryConfig[] categoryConfigs;

    [Header("Attachment List")]
    [SerializeField] private Transform listParent;
    [SerializeField] private AttachmentItem itemPrefab;
    [SerializeField] private AttachmentData[] database;

    [Header("Stats Panel")]
    [SerializeField] private Transform statListParent;
    [SerializeField] private StatItem statItemPrefab;
    [SerializeField] private StatConfig[] statConfigs;

    private readonly int stateAttachmentSelectedHash = Animator.StringToHash("State_AttachmentSelected");
    private bool isMenuOpen;

    private List<CategoryItem> categoryPool = new List<CategoryItem>();
    private List<AttachmentItem> itemPool = new List<AttachmentItem>();
    private Dictionary<StatType, StatItem> activeStatItems = new Dictionary<StatType, StatItem>();

    private AttachmentItem currentSelectedItem;
    private CategoryItem currentSelectedCategory;

    private void Start()
    {
        backgroundButton.onClick.AddListener(CloseAttachmentMenu);
        equipButton.onClick.AddListener(OnEquipButtonClicked);
        headerText.text = defaultTitle;

        InitializeStats();
        InitializeCategories();
        InitializeDefaultAttachments();

        equipButton.interactable = false;
    }

    private void OnDestroy()
    {
        backgroundButton.onClick.RemoveListener(CloseAttachmentMenu);
        equipButton.onClick.RemoveListener(OnEquipButtonClicked);
    }

    private void InitializeStats()
    {
        for (int i = 0; i < statConfigs.Length; i++)
        {
            StatItem newStatUI = Instantiate(statItemPrefab, statListParent);
            newStatUI.Setup(statConfigs[i].icon, statConfigs[i].displayName, statConfigs[i].baseValue);
            activeStatItems.Add(statConfigs[i].statType, newStatUI);
        }
    }

    private void UpdateStatsPanel(AttachmentData previewData)
    {
        var equipped = WeaponManager.Instance.GetEquippedData();

        for (int i = 0; i < statConfigs.Length; i++)
        {
            StatConfig config = statConfigs[i];

            float currentTotal = config.baseValue;
            foreach (var eqData in equipped.Values)
            {
                foreach (var mod in eqData.modifiers)
                {
                    if (mod.statType == config.statType)
                        currentTotal += mod.value;
                }
            }

            float displayModifier = 0f;

            if (previewData != null && !WeaponManager.Instance.IsEquipped(previewData))
            {
                float newTotal = config.baseValue;

                foreach (var eqData in equipped.Values)
                {
                    if (eqData.category == previewData.category)
                        continue;

                    foreach (var mod in eqData.modifiers)
                    {
                        if (mod.statType == config.statType)
                            newTotal += mod.value;
                    }
                }

                foreach (var mod in previewData.modifiers)
                {
                    if (mod.statType == config.statType)
                        newTotal += mod.value;
                }

                displayModifier = newTotal - currentTotal;
            }

            if (activeStatItems.TryGetValue(config.statType, out StatItem itemUI))
            {
                itemUI.UpdateValue(currentTotal, displayModifier);
            }
        }
    }

    private void InitializeDefaultAttachments()
    {
        foreach (var data in database)
        {
            if (data.attachmentLvl == 1)
            {
                WeaponManager.Instance.EquipWithoutPreview(data);

                foreach (var catItem in categoryPool)
                {
                    if (catItem.Category == data.category)
                    {
                        catItem.UpdateIcon(data.attachmentIcon);
                    }
                }
            }
        }
        UpdateStatsPanel(null);
    }

    private void InitializeCategories()
    {
        for (int i = 0; i < categoryConfigs.Length; i++)
        {
            CategoryItem catItem = Instantiate(categoryPrefab, categoryListParent);
            catItem.Setup(categoryConfigs[i].category, categoryConfigs[i].defaultIcon, OnCategoryButtonClicked);
            categoryPool.Add(catItem);
        }
    }

    private void OnCategoryButtonClicked(CategoryItem clickedCategory)
    {
        if (CameraController.Instance != null) CameraController.Instance.ResetToDefault();
        if (currentSelectedCategory != null && currentSelectedCategory != clickedCategory)
        {
            WeaponManager.Instance.CancelPreview(currentSelectedCategory.Category);
            currentSelectedCategory.SetHighlight(false);
        }

        clickedCategory.SetHighlight(true);
        currentSelectedCategory = clickedCategory;

        equipButton.interactable = false;

        if (!isMenuOpen)
        {
            ToggleUIVisibility(true);
            isMenuOpen = true;
        }

        PopulateList(clickedCategory.Category);
    }

    public void CloseAttachmentMenu()
    {
        if (CameraController.Instance != null) CameraController.Instance.ResetToDefault();
        if (isMenuOpen)
        {
            ToggleUIVisibility(false);
            isMenuOpen = false;
            equipButton.interactable = false;

            if (currentSelectedCategory != null)
            {
                WeaponManager.Instance.CancelPreview(currentSelectedCategory.Category);
                currentSelectedCategory.SetHighlight(false);
                currentSelectedCategory = null;
            }

            if (currentSelectedItem != null)
            {
                currentSelectedItem.SetHighlight(false);
                currentSelectedItem = null;
            }

            UpdateStatsPanel(null);
        }
    }

    private void ToggleUIVisibility(bool isOpen)
    {
        for (int i = 0; i < uiAnimators.Length; i++)
        {
            if (uiAnimators[i] != null)
                uiAnimators[i].SetBool(stateAttachmentSelectedHash, isOpen);
        }

        headerText.text = isOpen ? attachmentTitle : defaultTitle;

        for (int i = 0; i < objectsToHideOnOpen.Length; i++)
        {
            if (objectsToHideOnOpen[i] != null)
                objectsToHideOnOpen[i].SetActive(!isOpen);
        }
    }

    private void PopulateList(AttachmentCategory category)
    {
        foreach (var item in itemPool)
        {
            item.gameObject.SetActive(false);
        }

        int poolIndex = 0;

        AttachmentData equippedData = null;
        if (WeaponManager.Instance.GetEquippedData().TryGetValue(category, out AttachmentData eqData))
        {
            equippedData = eqData;
        }

        for (int i = 0; i < database.Length; i++)
        {
            if (database[i].category == category)
            {
                AttachmentItem itemUI;

                if (poolIndex >= itemPool.Count)
                {
                    itemUI = Instantiate(itemPrefab, listParent);
                    itemPool.Add(itemUI);
                }
                else
                {
                    itemUI = itemPool[poolIndex];
                    itemUI.gameObject.SetActive(true);
                }

                itemUI.Setup(database[i], OnAttachmentSelected);

                if (equippedData != null && database[i] == equippedData)
                {
                    itemUI.SetHighlight(true);
                    currentSelectedItem = itemUI;
                }
                else
                {
                    itemUI.SetHighlight(false);
                }

                poolIndex++;
            }
        }

        UpdateStatsPanel(null);
    }

    private void OnAttachmentSelected(AttachmentData data, AttachmentItem clickedItem)
    {
        if (currentSelectedItem != null)
        {
            currentSelectedItem.SetHighlight(false);
        }

        clickedItem.SetHighlight(true);
        currentSelectedItem = clickedItem;

        WeaponManager.Instance.PreviewAttachment(data);
        WeaponManager.Instance.UpdateCameraFocus(data);

        equipButton.interactable = !WeaponManager.Instance.IsEquipped(data);
        UpdateStatsPanel(data);
    }

    private void OnEquipButtonClicked()
    {
        WeaponManager.Instance.EquipAttachment();

        equipButton.interactable = false;

        if (currentSelectedCategory != null)
        {
            var equipped = WeaponManager.Instance.GetEquippedData();
            if (equipped.TryGetValue(currentSelectedCategory.Category, out AttachmentData eqData))
            {
                currentSelectedCategory.UpdateIcon(eqData.attachmentIcon);
            }
        }

        UpdateStatsPanel(null);
    }
}
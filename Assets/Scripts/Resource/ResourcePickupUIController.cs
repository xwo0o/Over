using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourcePickupUIController : MonoBehaviour
{
    public GameObject pickupUIPanel;
    public TextMeshProUGUI amountText;

    void Awake()
    {
        AutoFindUIElements();
        if (pickupUIPanel != null)
        {
            pickupUIPanel.SetActive(false);
        }
    }

    private void AutoFindUIElements()
    {
        if (pickupUIPanel == null)
        {
            pickupUIPanel = FindUIElement("PickupPanel") ??
                            FindUIElement("Panel") ??
                            FindUIElement("UIPanel");
        }

        if (amountText == null)
        {
            amountText = FindTextMeshProElement("AmountText") ??
                         FindTextMeshProElement("Amount") ??
                         FindTextMeshProElement("Count");
        }
    }

    private GameObject FindUIElement(string name)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private TextMeshProUGUI FindTextMeshProElement(string name)
    {
        TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.name == name)
            {
                return text;
            }
        }
        return null;
    }

    public void ShowPickupUI()
    {
        if (pickupUIPanel != null)
        {
            pickupUIPanel.SetActive(true);
        }
    }

    public void HidePickupUI()
    {
        if (pickupUIPanel != null)
        {
            pickupUIPanel.SetActive(false);
        }
    }
}

using UnityEngine;
using TMPro;
using Fearfront.Common;

public class UIWood : MonoBehaviour
{
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text stoneText;   // ← nou

    private PlayerInventory inv;

    private void Start()
    {
        inv = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
        Debug.Log(inv == null ? "inv ESTE NULL" : "inv OK");

        woodText.text = inv.Get(ResourceType.Tree).ToString();
        stoneText.text = inv.Get(ResourceType.Stone).ToString();   
        if (woodText == null) return;

        if (inv == null)
        {
            inv = FindFirstObjectByType<PlayerInventory>();
        }

        if (inv != null)
        {
            woodText.text = inv.Get(ResourceType.Tree).ToString();
        }
    }
}
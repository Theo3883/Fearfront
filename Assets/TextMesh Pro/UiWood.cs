using UnityEngine;
using TMPro;
using Fearfront.Common;

public class UIWood : MonoBehaviour
{
    [SerializeField] private TMP_Text woodText;

    private PlayerInventory inv;

    private void Start()
    {
        inv = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
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
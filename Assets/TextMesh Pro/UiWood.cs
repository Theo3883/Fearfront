using UnityEngine;
using TMPro;
using Fearfront.Common;

public class UIWood : MonoBehaviour
{
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text stoneText;

    private PlayerInventory inv;

    private void Start()
    {
        inv = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
        woodText.text = inv.Get(ResourceType.Tree).ToString();
        stoneText.text = inv.Get(ResourceType.Stone).ToString();   
        if (woodText == null) return;

        if (inv != null)
        {
            woodText.text = inv.Get(ResourceType.Tree).ToString();
        }
    }
}
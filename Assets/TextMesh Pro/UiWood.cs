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
        woodText.text = inv.Get(ResourceType.Tree).ToString();
    }
}
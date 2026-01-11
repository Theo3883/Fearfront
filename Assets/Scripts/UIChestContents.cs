using UnityEngine;
using TMPro;
using Fearfront.Common;

/// <summary>
/// UI similar to UIWood, but for a chest: shows what is stored inside the chest.
/// Put this on a world-space UI under the chest, assign TMP_Text fields.
/// </summary>
public class UIChestContents : MonoBehaviour
{
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text stoneText;
    [SerializeField] private ChestStorage chest;

    private void Awake()
    {
        if (chest == null)
        {
            chest = GetComponentInParent<ChestStorage>();
        }
    }

    private void OnEnable()
    {
        if (chest != null)
            chest.OnStoredChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (chest != null)
            chest.OnStoredChanged -= Refresh;
    }

    private void Refresh()
    {
        if (chest == null) return;
        if (woodText != null) woodText.text = chest.GetStored(ResourceType.Tree).ToString();
        if (stoneText != null) stoneText.text = chest.GetStored(ResourceType.Stone).ToString();
    }
}


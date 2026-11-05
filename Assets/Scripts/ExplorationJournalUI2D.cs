using System;
using UnityEngine;
using UnityEngine.UI;

namespace YearEnd
{
    [Serializable]
    public class JournalItemInfo2D
    {
        public string itemName;
        public Sprite icon;
        [TextArea(2, 4)] public string shortDescription;
        [TextArea(6, 16)] public string detailedDescription;
    }

    public sealed class ExplorationJournalUI2D : MonoBehaviour
    {
        public GameObject overviewPanel;
        public GameObject detailPanel;
        public Button[] slotButtons = new Button[8];
        public Image[] slotIcons = new Image[8];
        public Text[] slotNames = new Text[8];
        public Text[] unknownMarks = new Text[8];
        public Image detailIcon;
        public Text detailTitle;
        public Text detailShort;
        public Text detailBody;
        public ScrollRect detailScroll;
        public JournalItemInfo2D[] items = new JournalItemInfo2D[8];

        private int lastDetailIndex = -1;

        private void Awake()
        {
            for (int i = 0; i < slotButtons.Length && i < 8; i++)
            {
                int index = i;
                if (slotButtons[i] != null)
                    slotButtons[i].onClick.AddListener(() =>
                    {
                        var session = GameSession2D.Instance;
                        if (session != null) session.OpenJournalItem(index);
                    });
            }
        }
        private void LateUpdate()
        {
            var s = GameSession2D.Instance;
            if (s == null || !s.JournalOpen) { lastDetailIndex = -1; return; }

            bool details = s.JournalDetailIndex >= 0;
            if (overviewPanel != null && overviewPanel.activeSelf == details)
                overviewPanel.SetActive(!details);
            if (detailPanel != null && detailPanel.activeSelf != details)
                detailPanel.SetActive(details);

            if (details)
            {
                int i = s.JournalDetailIndex;
                if (lastDetailIndex != i)
                {
                    ShowDetails(i);
                    lastDetailIndex = i;
                }
            }
            else
            {
                lastDetailIndex = -1;
                for (int i = 0; i < 8; i++)
                {
                    bool owned = i < 4
                        ? s.State.Has((Relic)i)
                        : s.State.HasHidden((HiddenRelic)(i - 4));
                    if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                        slotButtons[i].interactable = owned;
                    if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                    {
                        Sprite icon = items != null && i < items.Length && items[i] != null ? items[i].icon : null;
                        slotIcons[i].sprite = icon;
                        slotIcons[i].gameObject.SetActive(owned && icon != null);
                    }
                    if (slotNames != null && i < slotNames.Length && slotNames[i] != null)
                    {
                        var it = items != null && i < items.Length ? items[i] : null;
                        slotNames[i].text = it != null ? it.itemName : "";
                        slotNames[i].gameObject.SetActive(owned);
                    }
                    if (unknownMarks != null && i < unknownMarks.Length && unknownMarks[i] != null)
                        unknownMarks[i].gameObject.SetActive(!owned);
                }
            }
        }
        private void ShowDetails(int index)
        {
            if (items == null || index < 0 || index >= items.Length || items[index] == null) return;
            var it = items[index];
            if (detailTitle != null) detailTitle.text = it.itemName;
            if (detailShort != null) detailShort.text = it.shortDescription;
            if (detailBody != null) detailBody.text = it.detailedDescription;
            if (detailIcon != null)
            {
                detailIcon.sprite = it.icon;
                detailIcon.gameObject.SetActive(it.icon != null);
            }
            if (detailScroll != null) detailScroll.verticalNormalizedPosition = 1f;
        }
    }
}

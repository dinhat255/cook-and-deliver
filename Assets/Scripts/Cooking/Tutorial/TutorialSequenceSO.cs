using System.Collections.Generic;
using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Tutorial
{
    // ScriptableObject holding the Day 1 Cooking guided tutorial steps and contextual soft hints
    [CreateAssetMenu(fileName = "Day1CookingTutorial", menuName = "Cooking/Tutorial/Cooking Sequence", order = 10)]
    public class TutorialSequenceSO : ScriptableObject
    {
        [Header("Guided Sequence")]
        public List<TutorialStepData> steps = new List<TutorialStepData>();

        [Header("Post-Tutorial Transition")]
        public RecipeSO postTutorialStarterRecipe;

        [Header("Day 1 Soft Contextual Hints")]
        public RecipeSO panHintRecipe;
        public string[] panHintAnchorIds;
        [TextArea(2, 3)]
        public string panHintText = "Bánh gạo cần xào trên Chảo sốt (nấu từng phần một)!";

        public string[] burntHintAnchorIds;
        [TextArea(2, 3)]
        public string burntHintText = "Món bị cháy rồi! Chạm Thùng rác rồi chạm vào bếp để dọn sạch.";

        public string[] shipOverlayHintAnchorIds;
        [TextArea(2, 3)]
        public string shipOverlayHintText = "Đủ 3 hộp ship! Chạm ĐI GIAO HÀNG để lên đường.";
    }
}

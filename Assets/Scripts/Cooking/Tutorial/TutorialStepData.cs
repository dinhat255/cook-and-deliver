using System;
using System.Collections.Generic;
using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Tutorial
{
    public enum TutorialCompletionRule
    {
        StationIngredientPlaced,
        BoardIngredientCount,
        BoardSelected,
        DishCompleted,
        DishServed
    }

    public enum TooltipPlacement
    {
        Auto,
        Top,
        Middle,
        Bottom
    }

    [Serializable]
    public class TutorialIngredientGoal
    {
        public string ingredientId;
        public int requiredAmount = 1;
    }

    [Serializable]
    public class TutorialStepData
    {
        [Header("Identification & Prompt")]
        public string stepId;
        [TextArea(2, 4)]
        public string instructionText;

        [Header("UI Targeting")]
        public string[] targetAnchorIds;
        public bool blockOutsideInputs = true;
        public bool showSpotlight = true;
        public bool showHandPointer = true;
        public TooltipPlacement tooltipPlacement = TooltipPlacement.Auto;

        [Header("Optional Scripted Order On Step Enter")]
        public bool spawnOrderBeforeStep;
        public RecipeSO orderToSpawn;
        public bool orderIsShip;
        public int orderPreferredLane = -1;

        [Header("Completion Condition")]
        public TutorialCompletionRule completionRule = TutorialCompletionRule.BoardIngredientCount;
        public int targetBoardIndex;
        public List<TutorialIngredientGoal> ingredientGoals = new List<TutorialIngredientGoal>();
    }
}

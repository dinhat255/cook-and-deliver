using System;
using System.Collections.Generic;
using CookAndDeliver.SaveLoad.Core;
using UnityEngine;

namespace CookAndDeliver.SaveLoad.Objects
{
    [Serializable]
    public class GameProgressSLO : ISaveLoadObject
    {
        [SerializeField] private int _totalCoins = 0;
        [SerializeField] private int _highestCombo = 0;
        [SerializeField] private int _totalOrdersCompleted = 0;
        [SerializeField] private List<string> _unlockedRecipeIds = new List<string>();

        public int TotalCoins
        {
            get => _totalCoins;
            set => _totalCoins = Mathf.Max(0, value);
        }

        public int HighestCombo
        {
            get => _highestCombo;
            set => _highestCombo = Mathf.Max(0, value);
        }

        public int TotalOrdersCompleted
        {
            get => _totalOrdersCompleted;
            set => _totalOrdersCompleted = Mathf.Max(0, value);
        }

        public IReadOnlyList<string> UnlockedRecipeIds => _unlockedRecipeIds;

        public GameProgressSLO()
        {
            _totalCoins = 0;
            _highestCombo = 0;
            _totalOrdersCompleted = 0;
            _unlockedRecipeIds = new List<string>();
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId) || _unlockedRecipeIds == null) return false;
            return _unlockedRecipeIds.Contains(recipeId);
        }

        public bool UnlockRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;

            if (_unlockedRecipeIds == null)
            {
                _unlockedRecipeIds = new List<string>();
            }

            if (!_unlockedRecipeIds.Contains(recipeId))
            {
                _unlockedRecipeIds.Add(recipeId);
                return true;
            }

            return false;
        }

        public void AddCoins(int amount)
        {
            TotalCoins += amount;
        }

        public bool TryDeductCoins(int cost)
        {
            if (TotalCoins < cost) return false;
            TotalCoins -= cost;
            return true;
        }

        public void RecordCombo(int combo)
        {
            if (combo > _highestCombo)
            {
                _highestCombo = combo;
            }
        }

        public void RecordCompletedOrder()
        {
            _totalOrdersCompleted++;
        }

        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }
    }
}

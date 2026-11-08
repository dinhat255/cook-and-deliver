using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookAndDeliver.SaveLoad.Core
{
    [Serializable]
    public class SaveTree
    {
        [Serializable]
        public struct SaveNode
        {
            public string Key;
            public string Payload;
        }

        [SerializeField]
        private List<SaveNode> _saveNodes;

        public SaveTree()
        {
            _saveNodes = new List<SaveNode>();
        }

        public string Get(string key)
        {
            if (_saveNodes == null) return null;
            int index = _saveNodes.FindIndex(n => n.Key == key);
            return index >= 0 ? _saveNodes[index].Payload : null;
        }

        public void Set(string key, string payload)
        {
            if (_saveNodes == null)
            {
                _saveNodes = new List<SaveNode>();
            }

            int index = _saveNodes.FindIndex(n => n.Key == key);
            if (index >= 0)
            {
                _saveNodes[index] = new SaveNode { Key = key, Payload = payload };
            }
            else
            {
                _saveNodes.Add(new SaveNode { Key = key, Payload = payload });
            }
        }

        public bool Remove(string key)
        {
            if (_saveNodes == null) return false;
            int index = _saveNodes.FindIndex(n => n.Key == key);
            if (index >= 0)
            {
                _saveNodes.RemoveAt(index);
                return true;
            }
            return false;
        }

        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }

        public bool IsEmpty()
        {
            return _saveNodes == null || _saveNodes.Count == 0;
        }

        public void Clear()
        {
            _saveNodes?.Clear();
        }
    }
}

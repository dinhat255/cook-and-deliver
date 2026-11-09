using System;
using System.Collections.Generic;
using System.IO;
using CookAndDeliver.Delivery.Core;
using UnityEngine;

namespace CookAndDeliver.Delivery
{
    [Serializable] public sealed class DeliveryBestScore { public int orderCount; public int score; }
    [Serializable] public sealed class DeliveryProgress
    {
        public int version = 1;
        public int totalDelivered;
        public List<DeliveryBestScore> bestScores = new List<DeliveryBestScore>();
        public int BestFor(int count) { return bestScores.Find(x => x.orderCount == count)?.score ?? 0; }
    }

    public sealed class DeliveryProgressStore
    {
        private readonly string path;
        private readonly HashSet<DeliveryRunResult> recorded = new HashSet<DeliveryRunResult>();
        private bool dirty;
        public DeliveryProgress Data { get; private set; }
        public string LastError { get; private set; }
        public DeliveryProgressStore(string directory)
        {
            path = Path.Combine(directory, "progress.json");
            Data = Read(path) ?? Read(path + ".bak") ?? new DeliveryProgress();
        }

        private DeliveryProgress Read(string file)
        {
            if (!File.Exists(file)) return null;
            try
            {
                var data = JsonUtility.FromJson<DeliveryProgress>(File.ReadAllText(file));
                if (data == null || data.version != 1 || data.totalDelivered < 0 || data.bestScores == null || data.bestScores.Exists(x => x == null || x.orderCount < 1 || x.score < 0))
                    return null;
                return data;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { LastError = e.Message; return null; }
        }

        public bool Record(DeliveryRunResult result)
        {
            if (result == null) return true;
            if (recorded.Add(result))
            {
                Data.totalDelivered += result.DeliveredCount;
                var best = Data.bestScores.Find(x => x.orderCount == result.OrderCount);
                if (best == null) Data.bestScores.Add(new DeliveryBestScore { orderCount = result.OrderCount, score = result.Score });
                else best.score = Math.Max(best.score, result.Score);
                dirty = true;
            }
            if (!dirty) return true;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(path))
                {
                    // Never replace a healthy recovery copy with a corrupt primary file.
                    if (Read(path) != null) File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                }
                File.Move(temp, path);
                LastError = null;
                dirty = false;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { LastError = e.Message; return false; }
        }
    }
}

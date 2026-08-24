using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaRunner.Spawning
{
    /// <summary>
    /// Selección ponderada de una SpawnableEntry válida para la dificultad actual.
    /// Sin dependencias de MonoBehaviour: testeable con NUnit sin escena.
    /// </summary>
    public static class WeightedSpawnSelector
    {
        public static SpawnableEntry SelectEntry(
            IReadOnlyList<SpawnableEntry> entries,
            float currentDifficulty,
            Func<float> random01 = null)
        {
            random01 ??= () => UnityEngine.Random.value;

            var valid = new List<SpawnableEntry>();
            int totalWeight = 0;
            foreach (var entry in entries)
            {
                if (currentDifficulty >= entry.minDifficulty && currentDifficulty <= entry.maxDifficulty)
                {
                    valid.Add(entry);
                    totalWeight += entry.weight;
                }
            }

            if (valid.Count == 0 || totalWeight <= 0)
                return null;

            float roll = random01() * totalWeight;
            float cumulative = 0f;
            foreach (var entry in valid)
            {
                cumulative += entry.weight;
                if (roll < cumulative) return entry;
            }
            return valid[valid.Count - 1];
        }
    }
}
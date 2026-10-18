using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaRunner.Spawning
{
    /// <summary>
    /// Selección ponderada de una SpawnableEntry válida para la dificultad actual.
    /// Sin dependencias de MonoBehaviour: testeable con NUnit sin escena.
    /// No reserva memoria: recorre las entradas dos veces en vez de armar una lista.
    /// </summary>
    public static class WeightedSpawnSelector
    {
        private static readonly Func<float> DefaultRandom = () => UnityEngine.Random.value;

        public static SpawnableEntry SelectEntry(
            IReadOnlyList<SpawnableEntry> entries,
            float currentDifficulty,
            Func<float> random01 = null)
        {
            random01 ??= DefaultRandom;

            int totalWeight = 0;
            SpawnableEntry lastValid = null;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (IsValid(entry, currentDifficulty))
                {
                    totalWeight += entry.weight;
                    lastValid = entry;
                }
            }

            if (lastValid == null || totalWeight <= 0)
                return null;

            float roll = random01() * totalWeight;
            float cumulative = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!IsValid(entry, currentDifficulty)) continue;

                cumulative += entry.weight;
                if (roll < cumulative) return entry;
            }
            return lastValid;
        }

        private static bool IsValid(SpawnableEntry entry, float difficulty) =>
            difficulty >= entry.minDifficulty && difficulty <= entry.maxDifficulty;
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using MechaRunner.Spawning;

public class WeightedSpawnSelectorTests
{
    [Test]
    public void SelectEntry_IgnoraEntradasFueraDeRangoDeDificultad()
    {
        var entries = new List<SpawnableEntry>
        {
            new SpawnableEntry { poolId = "FueraDeRango", weight = 1, minDifficulty = 20f, maxDifficulty = 30f },
            new SpawnableEntry { poolId = "Valida", weight = 1, minDifficulty = 0f, maxDifficulty = 10f },
        };

        var result = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty: 5f, random01: () => 0f);

        Assert.AreEqual("Valida", result.poolId);
    }

    [Test]
    public void SelectEntry_RespetaElPesoRelativo()
    {
        var entries = new List<SpawnableEntry>
        {
            new SpawnableEntry { poolId = "Comun", weight = 3 },
            new SpawnableEntry { poolId = "Raro", weight = 1 },
        };

        // roll = 0.9 * totalWeight(4) = 3.6 -> cae en el segundo (Raro)
        var result = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty: 0f, random01: () => 0.9f);

        Assert.AreEqual("Raro", result.poolId);
    }

    [Test]
    public void SelectEntry_DevuelveNullSiNingunaEntradaEsValida()
    {
        var entries = new List<SpawnableEntry>
        {
            new SpawnableEntry { poolId = "FueraDeRango", weight = 1, minDifficulty = 20f, maxDifficulty = 30f },
        };

        var result = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty: 5f);

        Assert.IsNull(result);
    }
}
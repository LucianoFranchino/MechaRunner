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
            new SpawnableEntry { weight = 1, minDifficulty = 20f, maxDifficulty = 30f },
            new SpawnableEntry { weight = 1, minDifficulty = 0f, maxDifficulty = 10f },
        };

        var result = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty: 5f, random01: () => 0f);

        Assert.AreSame(entries[1], result, "Debe elegir la única entrada válida");
    }

    [Test]
    public void SelectEntry_RespetaElPesoRelativo()
    {
        var entries = new List<SpawnableEntry>
        {
            new SpawnableEntry { weight = 3 },
            new SpawnableEntry { weight = 1 },
        };

        // roll = 0.9 * totalWeight(4) = 3.6 -> cae en el segundo (Raro)
        var result = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty: 0f, random01: () => 0.9f);

        Assert.AreSame(entries[1], result, "Debe caer en la entrada rara");
    }

    [Test]
    public void SelectEntry_DevuelveNullSiNingunaEntradaEsValida()
    {
        var entries = new List<SpawnableEntry>
        {
            new SpawnableEntry { weight = 1, minDifficulty = 20f, maxDifficulty = 30f },
        };

        var result = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty: 5f);

        Assert.IsNull(result);
    }
}
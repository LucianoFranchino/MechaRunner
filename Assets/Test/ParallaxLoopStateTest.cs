using NUnit.Framework;
using MechaRunner.Environment;

public class ParallaxLoopStateTests
{
    [Test]
    public void Advance_MuevesTodosLosSegmentosHaciaLaIzquierda()
    {
        var state = new ParallaxLoopState(new float[] { 0f, 10f }, segmentWidth: 10f, recycleX: -5f);

        state.Advance(2f);

        Assert.AreEqual(-2f, state.SegmentX[0], 0.001f);
        Assert.AreEqual(8f, state.SegmentX[1], 0.001f);
    }

    [Test]
    public void Advance_RecicaSegmentoDetrasDeRecycleX_AlFinalDeLaFila()
    {
        var state = new ParallaxLoopState(new float[] { -4f, 6f }, segmentWidth: 10f, recycleX: -5f);

        var recycled = state.Advance(2f); // segmento 0 pasa a -6, cruza recycleX (-5)

        CollectionAssert.Contains(recycled, 0);
        Assert.AreEqual(14f, state.SegmentX[0], 0.001f); // se reubica después del más a la derecha (4) + segmentWidth
    }

    [Test]
    public void Advance_NoReciclaSiNingunSegmentoCruzoElUmbral()
    {
        var state = new ParallaxLoopState(new float[] { 0f, 10f }, segmentWidth: 10f, recycleX: -5f);

        var recycled = state.Advance(1f);

        Assert.IsEmpty(recycled);
    }
}
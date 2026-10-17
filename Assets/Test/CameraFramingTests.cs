using NUnit.Framework;

public class CameraFramingTests
{
    private const float Size = 3f;
    private const float Bottom = -1f;
    private const float PlayerX = -3.1f;
    private const float Margin = 2f;

    [TestCase(4f / 3f)]
    [TestCase(16f / 10f)]
    [TestCase(16f / 9f)]
    [TestCase(20f / 9f)]
    [TestCase(21f / 9f)]
    public void Compute_JugadorSiempreAlMismoMargenDelBordeIzquierdo(float aspect)
    {
        var f = CameraFraming.Compute(aspect, Size, Bottom, PlayerX, Margin);

        Assert.AreEqual(Margin, PlayerX - f.LeftEdge, 0.001f);
        Assert.Greater(f.RightEdge, PlayerX, "El jugador debe quedar dentro de la pantalla");
    }

    [TestCase(4f / 3f)]
    [TestCase(21f / 9f)]
    public void Compute_BordeInferiorFijoYAltoConstante(float aspect)
    {
        var f = CameraFraming.Compute(aspect, Size, Bottom, PlayerX, Margin);

        Assert.AreEqual(Bottom, f.CameraPosition.y - Size, 0.001f);
    }

    [Test]
    public void Compute_PantallaMasAnchaSoloSumaAnticipacionHaciaLaDerecha()
    {
        var narrow = CameraFraming.Compute(16f / 9f, Size, Bottom, PlayerX, Margin);
        var wide = CameraFraming.Compute(20f / 9f, Size, Bottom, PlayerX, Margin);

        Assert.AreEqual(narrow.LeftEdge, wide.LeftEdge, 0.001f);
        Assert.Greater(wide.RightEdge, narrow.RightEdge);
        Assert.AreEqual(2f * Size * 20f / 9f, wide.RightEdge - wide.LeftEdge, 0.001f);
    }
}

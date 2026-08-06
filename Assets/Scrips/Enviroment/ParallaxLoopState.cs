using System.Collections.Generic;

namespace MechaRunner.Environment
{
    /// <summary>
    /// Lógica pura (sin MonoBehaviour) del loop infinito de una capa de parallax.
    /// Mueve N segmentos hacia la izquierda y recicla, al final de la fila,
    /// cualquiera que quede detrás de recycleX. Testeable con NUnit sin escena.
    /// </summary>
    public struct ParallaxLoopState
    {
        public float[] SegmentX { get; private set; }
        private readonly float segmentWidth;
        private readonly float recycleX;

        public ParallaxLoopState(float[] initialPositions, float segmentWidth, float recycleX)
        {
            SegmentX = (float[])initialPositions.Clone();
            this.segmentWidth = segmentWidth;
            this.recycleX = recycleX;
        }

        /// <summary>
        /// Avanza todos los segmentos "movement" unidades a la izquierda y recicla
        /// los que quedaron atrás. Devuelve los índices reciclados este frame
        /// (útil a futuro para, por ejemplo, cambiarles el sprite al reciclarse).
        /// </summary>
        public List<int> Advance(float movement)
        {
            for (int i = 0; i < SegmentX.Length; i++)
            {
                SegmentX[i] -= movement;
            }

            var recycled = new List<int>();
            for (int i = 0; i < SegmentX.Length; i++)
            {
                if (SegmentX[i] <= recycleX)
                {
                    SegmentX[i] = MaxX() + segmentWidth;
                    recycled.Add(i);
                }
            }
            return recycled;
        }

        private float MaxX()
        {
            float max = float.MinValue;
            foreach (float x in SegmentX)
            {
                if (x > max) max = x;
            }
            return max;
        }
    }
}
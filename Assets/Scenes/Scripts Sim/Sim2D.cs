using System.Collections;
using UnityEngine;
using TMPro;

// Caso de 2 detectores: transmitido y reflejado, cada uno con su propio "clic"
// aleatorio e independiente, sin fotón testigo de por medio. Con eso se calcula
// g2, que clásicamente debería salir cerca de 1 (con solo estos dos detectores
// no alcanza para ver nada distinto a lo clásico; para eso está el caso de 3
// detectores). Portado del programa de Víctor en fortran, que corría 8,000,000
// de bins x 10 experimentos pensado para ejecutarse una sola vez en batch. Acá
// se bajó la escala para que corra al presionar space sin trabar la app, pero
// manteniendo la misma probabilidad de clic por bin (N/NN) que el original.
public class Sim2D : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private GameObject pantalla;
    [SerializeField] private TMP_Text textoResultados;

    [Header("Parámetros de la simulación")]
    [SerializeField] private int nexp = 5;     // experimentos que se promedian cada vez que se enciende
    [SerializeField] private int N = 10000;    // tasa esperada de clics por canal (transmitido/reflejado)
    [SerializeField] private int NN = 800000;  // bins de tiempo; la probabilidad real de clic por bin es N/NN

    [Header("Rendimiento")]
    [SerializeField] private int binsPorFrame = 50000; // bins procesados antes de ceder un frame

    private bool mostrandoResultados = false;
    private Coroutine simulacionActual;

    void Start()
    {
        if (pantalla != null) pantalla.SetActive(false);
    }

    void Update()
    {
        // GetKeyDown en vez de GetKey: se dispara una sola vez por tecleo,
        // no en cada frame mientras se mantiene presionada la tecla.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            mostrandoResultados = !mostrandoResultados;

            if (simulacionActual != null)
            {
                StopCoroutine(simulacionActual);
                simulacionActual = null;
            }

            if (pantalla != null) pantalla.SetActive(mostrandoResultados);

            if (mostrandoResultados)
                simulacionActual = StartCoroutine(EjecutarSimulacion());
            else if (textoResultados != null)
                textoResultados.text = "";
        }
    }

    private IEnumerator EjecutarSimulacion()
    {
        if (textoResultados != null) textoResultados.text = "Midiendo...";

        float g2Promedio = 0f;
        float[] g2 = new float[nexp];

        for (int j = 0; j < nexp; j++)
        {
            // pequeña fluctuación de la tasa entre experimentos, como NR en el .f
            int NR = Random.Range(0, Mathf.Max(1, N / 500));
            float ff = (float)(N + NR) / NN;

            int unost = 0, unosr = 0, nc = 0;

            for (int i = 0; i < NN; i++)
            {
                bool transmitido = Random.value <= ff;
                bool reflejado = Random.value <= ff;

                if (transmitido) unost++;
                if (reflejado) unosr++;
                if (transmitido && reflejado) nc++;

                if (i % binsPorFrame == 0) yield return null;
            }

            g2[j] = (unost > 0 && unosr > 0) ? (nc / (unost * (float)unosr)) * NN : 0f;
            g2Promedio += g2[j] / nexp;
        }

        // desviación del promedio, igual que sigma2 en el .f
        float v2 = 0f;
        for (int k = 0; k < nexp; k++)
            v2 += Mathf.Pow(g2Promedio - g2[k], 2f) / (nexp * (float)nexp);
        float sigma2 = Mathf.Sqrt(v2);

        if (textoResultados != null)
        {
            textoResultados.text =
                "CASO 2 DETECTORES\n\n" +
                "Experimentos: " + nexp + "\n" +
                "g² = " + g2Promedio.ToString("F4") + "\n" +
                "σ(g²) = " + sigma2.ToString("F4");
        }

        simulacionActual = null;
    }
}
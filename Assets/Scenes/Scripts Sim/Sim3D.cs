using System.Collections;
using UnityEngine;
using TMPro;

// Caso de 3 detectores: transmitido, reflejado y testigo (heraldo). El testigo
// además funciona como la señal real: cuando dispara, el divisor de haz lo manda
// a transmitido O a reflejado (nunca a los dos), que es justo el comportamiento
// de un fotón único. Con eso se calculan g2 (sin condicionar a nada) y g3
// (condicionado a que el testigo haya disparado). g2 sale cerca de 1; g3 sale
// muy por debajo de 1, y esa caída es la firma de que el fotón nunca llegó a
// los dos detectores a la vez.
//
// Portado del programa de Víctor en fortran (8,000,000 bins x 10 experimentos
// originalmente, pensado para correr una sola vez en batch). Acá se bajó la
// escala para que corra al presionar space sin trabar la app, manteniendo las
// mismas probabilidades N/NN y NQ/NN del original.
public class Sim3D : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private GameObject pantalla;
    [SerializeField] private TMP_Text textoResultados;

    [Header("Parámetros de la simulación")]
    [SerializeField] private int nexp = 5;     // experimentos que se promedian cada vez que se enciende
    [SerializeField] private int N = 10000;    // tasa esperada en transmitido/reflejado
    [SerializeField] private int NQ = 1000;    // tasa esperada del testigo (bastante menor que N)
    [SerializeField] private int NN = 800000;  // bins de tiempo; probabilidad real de clic por bin es N/NN y NQ/NN

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

        float g2Promedio = 0f, g3Promedio = 0f;
        float[] g2 = new float[nexp];
        float[] g3 = new float[nexp];

        for (int j = 0; j < nexp; j++)
        {
            int NR = Random.Range(0, Mathf.Max(1, N / 500));
            int NQR = Random.Range(0, Mathf.Max(1, NQ / 500));
            float ff = (float)(N + NR) / NN;
            float ffQ = (float)(NQ + NQR) / NN;

            int unost = 0, unosr = 0, unosi = 0;
            int nc = 0, nc123 = 0, nc13 = 0, nc23 = 0;

            for (int i = 0; i < NN; i++)
            {
                bool transmitido = Random.value <= ff;
                bool reflejado = Random.value <= ff;
                bool testigo = Random.value <= ffQ;

                // el divisor de haz solo se tira cuando de verdad hay un fotón
                // testigo que repartir; así no se desperdicia esa tirada en
                // el resto de los bins (la gran mayoría, porque ffQ es chico)
                if (testigo)
                {
                    if (Random.value >= 0.5f) transmitido = true;
                    else reflejado = true;
                }

                if (transmitido) unost++;
                if (reflejado) unosr++;
                if (testigo) unosi++;
                if (transmitido && reflejado) nc++;
                if (testigo && transmitido && reflejado) nc123++;
                if (testigo && transmitido) nc13++;
                if (testigo && reflejado) nc23++;

                if (i % binsPorFrame == 0) yield return null;
            }

            g2[j] = (unost > 0 && unosr > 0) ? (nc / (unost * (float)unosr)) * NN : 0f;
            g3[j] = (nc13 > 0 && nc23 > 0) ? (nc123 * (float)unosi) / (nc13 * (float)nc23) : 0f;

            g2Promedio += g2[j] / nexp;
            g3Promedio += g3[j] / nexp;
        }

        // desviación de los promedios, igual que sigma2/sigma3 en el .f
        float v2 = 0f, v3 = 0f;
        for (int k = 0; k < nexp; k++)
        {
            v2 += Mathf.Pow(g2Promedio - g2[k], 2f) / (nexp * (float)nexp);
            v3 += Mathf.Pow(g3Promedio - g3[k], 2f) / (nexp * (float)nexp);
        }
        float sigma2 = Mathf.Sqrt(v2);
        float sigma3 = Mathf.Sqrt(v3);

        if (textoResultados != null)
        {
            textoResultados.text =
                "CASO 3 DETECTORES\n\n" +
                "Experimentos: " + nexp + "\n\n" +
                "g² = " + g2Promedio.ToString("F4") + "\n" +
                "σ(g²) = " + sigma2.ToString("F4") + "\n\n" +
                "g³ = " + g3Promedio.ToString("F4") + "\n" +
                "σ(g³) = " + sigma3.ToString("F4");
        }

        simulacionActual = null;
    }
}
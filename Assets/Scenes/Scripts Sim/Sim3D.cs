using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Sim3D : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private GameObject pantalla;
    [SerializeField] private TMP_Text textoResultados;

    [Header("Parámetros de la simulación")]
    [SerializeField] private int nexp = 5;
    [SerializeField] private int N = 2500;
    [SerializeField] private int NQ = 250;
    [SerializeField] private int NN = 200000;

    [Header("Rendimiento")]
    [SerializeField] private int binsPorFrame = 20000;

    private bool mostrandoResultados = false;
    private Coroutine simulacionActual;

    void Start()
    {
        if (pantalla != null)
            pantalla.SetActive(false);
    }

    void Update()
    {
        // La simulación se activa una sola vez por pulsación.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            mostrandoResultados = !mostrandoResultados;

            if (simulacionActual != null)
            {
                StopCoroutine(simulacionActual);
                simulacionActual = null;
            }

            if (pantalla != null)
                pantalla.SetActive(mostrandoResultados);

            if (mostrandoResultados)
            {
                simulacionActual = StartCoroutine(EjecutarSimulacion());
            }
            else
            {
                if (textoResultados != null)
                    textoResultados.text = "";
            }
        }
    }

    private IEnumerator EjecutarSimulacion()
    {
        if (textoResultados != null)
            textoResultados.text = "Calculando...";

        double g2Promedio = 0.0;
        double g3Promedio = 0.0;

        double[] g2 = new double[nexp];
        double[] g3 = new double[nexp];

        for (int j = 0; j < nexp; j++)
        {
            // Variaciones aleatorias del programa de Víctor.
            int NR = Random.Range(0, 200);
            int NQR = Random.Range(0, 200);

            double ff = (double)(N + NR) / NN;
            double ffQ = (double)(NQ + NQR) / NN;

            long unosi = 0;
            long unost = 0;
            long unosr = 0;

            long nc = 0;
            long nc123 = 0;
            long nc13 = 0;
            long nc23 = 0;

            for (int i = 0; i < NN; i++)
            {
                // Series transmitida y reflejada.
                bool transmitido = Random.value <= ff;
                bool reflejado = Random.value <= ff;

                // Serie de la señal testigo.
                bool testigo = Random.value <= ffQ;

                if (testigo)
                    unosi++;

                // División de la señal testigo.
                // El fotón va a transmitido O a reflejado.
                bool sit = false;
                bool sir = false;

                if (Random.value >= 0.5f)
                {
                    sit = testigo;
                }
                else
                {
                    sir = testigo;
                }

                // Suma de las series.
                bool st = transmitido || sit;
                bool sr = reflejado || sir;

                if (st)
                    unost++;

                if (sr)
                    unosr++;

                // Coincidencia entre transmitido y reflejado.
                if (st && sr)
                    nc++;

                // Coincidencia de los tres detectores.
                if (testigo && st && sr)
                    nc123++;

                // Coincidencias de dos detectores.
                if (st && testigo)
                    nc13++;

                if (sr && testigo)
                    nc23++;

                if (i % binsPorFrame == 0)
                    yield return null;
            }

            // Cálculo de g2.
            if (unost > 0 && unosr > 0)
            {
                g2[j] =
                    ((double)nc / ((double)unost * unosr)) * NN;
            }
            else
            {
                g2[j] = 0.0;
            }

            // Cálculo de g3 según el Fortran original.
            if (nc13 > 0 && nc23 > 0)
            {
                g3[j] =
                    ((double)nc123 /
                    ((double)nc13 * nc23)) * unosi;
            }
            else
            {
                g3[j] = 0.0;
            }

            g2Promedio += g2[j] / nexp;
            g3Promedio += g3[j] / nexp;
        }

        // Desviación estándar de g2 y g3.
        double v2 = 0.0;
        double v3 = 0.0;

        for (int k = 0; k < nexp; k++)
        {
            v2 +=
                System.Math.Pow(g2Promedio - g2[k], 2.0)
                / ((double)nexp * nexp);

            v3 +=
                System.Math.Pow(g3Promedio - g3[k], 2.0)
                / ((double)nexp * nexp);
        }

        double sigma2 = System.Math.Sqrt(v2);
        double sigma3 = System.Math.Sqrt(v3);

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
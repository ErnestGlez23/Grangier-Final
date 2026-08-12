using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Sim2D : MonoBehaviour
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
        // GetKeyDown hace que la acción ocurra una sola vez
        // por cada pulsación de la tecla.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            mostrandoResultados = !mostrandoResultados;

            // Si hubiera una simulación anterior en ejecución,
            // se detiene antes de iniciar otra.
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
            textoResultados.text = "Midiendo...";

        double g2Promedio = 0.0;
        double[] g2 = new double[nexp];

        for (int j = 0; j < nexp; j++)
        {
            // Variaciones aleatorias utilizadas en el programa de Víctor.
            int NR = Random.Range(0, 200);
            int NQR = Random.Range(0, 200);

            double ff = (double)(N + NR) / NN;
            double ffQ = (double)(NQ + NQR) / NN;

            long unost = 0;
            long unosr = 0;
            long nc = 0;

            for (int i = 0; i < NN; i++)
            {
                // Series transmitida y reflejada.
                bool transmitido = Random.value <= ff;
                bool reflejado = Random.value <= ff;

                // Serie de la señal testigo.
                bool testigo = Random.value <= ffQ;

                // El testigo se dirige al detector transmitido
                // o al reflejado, pero no a ambos.
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

                // Coincidencia entre los dos detectores.
                if (st && sr)
                    nc++;

                // Cada cierto número de intervalos se cede un frame
                // para evitar bloquear la aplicación.
                if (i % binsPorFrame == 0)
                    yield return null;
            }

            // Cálculo de g2 según el programa original.
            if (unost > 0 && unosr > 0)
            {
                g2[j] =
                    ((double)nc / ((double)unost * unosr)) * NN;
            }
            else
            {
                g2[j] = 0.0;
            }

            g2Promedio += g2[j] / nexp;
        }

        // Cálculo de la desviación estándar utilizada en el Fortran.
        double v2 = 0.0;

        for (int k = 0; k < nexp; k++)
        {
            v2 +=
                System.Math.Pow(g2Promedio - g2[k], 2.0)
                / ((double)nexp * nexp);
        }

        double sigma2 = System.Math.Sqrt(v2);

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
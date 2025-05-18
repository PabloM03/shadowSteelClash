using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightManager : MonoBehaviour
{
    public int requiredKnights = 2; // Número requerido de caballeros
    private TeamManager teamManager; // Referencia al script que gestionará los equipos

    private void Start()
    {
        teamManager = GetComponent<TeamManager>();
        StartCoroutine(WaitForKnights());
    }

    private IEnumerator WaitForKnights()
    {
        while (true)
        {
            // Contar los caballeros en la escena
            KnightController[] knightsInScene = FindObjectsOfType<KnightController>();

            if (knightsInScene.Length >= requiredKnights)
            {
                Debug.Log("Suficientes caballeros en la escena. Activando TeamManager.");
                //teamManager.InitializeTeams();
                yield break; // Termina la coroutine
            }

            // Esperar hasta el siguiente frame antes de volver a comprobar
            yield return null;
        }
    }
}

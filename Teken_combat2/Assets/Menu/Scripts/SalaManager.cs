using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SalaManager : MonoBehaviour
{
    private Dictionary<string, Scene> salasCreadas = new();

    public void CrearSala(NetworkConnectionToClient conn)
    {
        string id = GenerarIDUnico();
        string sceneName = "scene1 2";
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += (op) =>
        {
            Scene nuevaSala = SceneManager.GetSceneByName(sceneName);
            salasCreadas.Add(id, nuevaSala);
            GameObject jugador = conn.identity.gameObject;
            SceneManager.MoveGameObjectToScene(jugador, nuevaSala);
            // Puedes mostrar el ID en el canvas desde aquí si quieres
        };
    }

    public void UnirseAleatoria(NetworkConnectionToClient conn)
    {
        foreach (var sala in salasCreadas)
        {
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, sala.Value);
            return;
        }

        CrearSala(conn); // Si no hay ninguna
    }

    public void UnirsePorID(string id, NetworkConnectionToClient conn)
    {
        if (salasCreadas.TryGetValue(id, out var sala))
        {
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, sala);
        }
        else
        {
            Debug.LogWarning($"Sala con ID {id} no encontrada.");
            // Aquí podrías devolver al jugador a otra escena o mostrar un mensaje
        }
    }

    private string GenerarIDUnico()
    {
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string id;
        do
        {
            id = "";
            for (int i = 0; i < 5; i++)
                id += letras[Random.Range(0, letras.Length)];
        } while (salasCreadas.ContainsKey(id));

        return id;
    }
}

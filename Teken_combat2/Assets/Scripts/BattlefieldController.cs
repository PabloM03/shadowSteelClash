using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class BattlefieldController : MonoBehaviour
{
    public Transform knight; // Referencia al caballero en la escena
    public Transform warrokPrefab;
    public Transform mainCamera;
    public bool hasKnight;
    public int numWarroksFriends;
    public int numWarroksEnemies;
    public float warrokPower;
    public Transform place;
    private float limit=5f; 
    public bool newGame=false;

    private List<Transform> friends = new List<Transform>();
    private List<Transform> enemies = new List<Transform>();

    void Start()
    {
        numWarroksFriends = (int)SceneData.numberValue1;
        numWarroksEnemies = (int)SceneData.numberValue2;
        warrokPower = SceneData.numberValue3;
        hasKnight = SceneData.isCheckboxChecked;
        if(numWarroksFriends>100) numWarroksFriends=100;
        if(numWarroksEnemies>100) numWarroksEnemies=100;
            CreateBattlefield();
    }

    void Update()
    {
	if(newGame)
	{
	    newGame=false;
	    Start();
	}
    }

    private void CreateBattlefield()
    {

	if(warrokPower>1) place.localScale*= warrokPower;
        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (warrokPower>1) cameraFollow.offset.z -= warrokPower*2-2;

        // Posicionar y habilitar al caballero si existe
        if (hasKnight && knight != null)
        {
            knight.position = Vector3.zero; // Posicionar en (0, 0, 0)
            knight.gameObject.SetActive(true); // Activar el caballero

            // Añadir knight a la lista de amigos
            friends.Add(knight);
        }

        // Crear Warroks amigos
        for (int i = 0; i < numWarroksFriends; i++)
        {
            Transform warrok = InstantiateWarrok(Vector3.up);
            // Encontrar la barra de vida en el Canvas de knight y warrok
            Image knightLifeBar = knight.Find("Canvas/background/LifeBar").GetComponent<Image>();
            Image warrokLifeBar = warrok.Find("Canvas/background/LifeBar").GetComponent<Image>();

            // Copiar el color de la barra de vida de knight a la de warrok
            warrokLifeBar.color = knightLifeBar.color;
            friends.Add(warrok);
        }

        // Crear Warroks enemigos
        for (int i = 0; i < numWarroksEnemies; i++)
        {
            Transform warrok = InstantiateWarrok(Vector3.down);
            enemies.Add(warrok);
        }

        // Configurar listas en Warroks amigos
        foreach (Transform friendWarrok in friends)
        {
            WarrokController warrokController = friendWarrok.GetComponent<WarrokController>();
            HealthController healthController = friendWarrok.GetComponent<HealthController>();

            if (warrokController != null) warrokController.knights = enemies;
            if (healthController != null) healthController.enemies = enemies;
        }

        // Configurar listas en Warroks enemigos
        foreach (Transform enemyWarrok in enemies)
        {
            WarrokController warrokController = enemyWarrok.GetComponent<WarrokController>();
            HealthController healthController = enemyWarrok.GetComponent<HealthController>();

            if (warrokController != null) warrokController.knights = friends;
            if (healthController != null) healthController.enemies = friends;
        }
        if (hasKnight && knight != null)
        {
            HealthController healthKnight = knight.GetComponent<HealthController>();
            healthKnight.power = 1;
	    if(warrokPower>1) knight.GetComponent<RestrictToCircleMovement>().radius*= warrokPower;
        }
        else
        {
            if (numWarroksFriends > 0 && friends.Count > 0)
            {
                cameraFollow.target = friends[0];
            }
            else if (numWarroksEnemies > 0 && enemies.Count > 0)
            {
                cameraFollow.target = enemies[0];
            }
        }
    }
        private Transform InstantiateWarrok(Vector3 hemisphereDirection)
    {
        // Posición aleatoria en una semiesfera de radio límite
	Vector3 randomPosition = Random.onUnitSphere * 5f;
	randomPosition.y = Mathf.Clamp(randomPosition.y, 0, 5);

        // Instanciar Warrok y aplicar escala y poder
        Transform warrok = Instantiate(warrokPrefab, randomPosition, Quaternion.identity);
        warrok.localScale *= warrokPower;
        warrok.gameObject.SetActive(true);

        // Configurar el poder en el script HealthController
        HealthController healthController = warrok.GetComponent<HealthController>();
        if (healthController != null) healthController.power = warrokPower;

	if(warrokPower>1)
	{
	    limit*=warrokPower;
	    warrok.GetComponent<RestrictToCircleMovement>().radius*= warrokPower;
	}

        return warrok;
    }
}
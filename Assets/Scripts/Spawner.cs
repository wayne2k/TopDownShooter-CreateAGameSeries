using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour 
{
	public event System.Action<int> OnNewWave;

	public Wave[] waves;
	public Enemy enemy;

	LivingEntity playerEntity;
	Transform playerT;

	Wave currentWave;
	int currentWaveNumber;

	int enemiesRemainingToSpawn;
	int enemiesRemainingAlive;
	float nextSpawnTime;

	MapGenerator map;


	float timeBetweenCampingChecks = 2f;
	float campThresholdDistance = 1.5f;
	float nextCampCheckTime;
	Vector3 campPositionOld;
	bool isCamping;

	bool isDisabled;

	void Start ()
	{
		playerEntity = FindObjectOfType <Player> ();
		playerT = playerEntity.transform;

		nextCampCheckTime = timeBetweenCampingChecks + Time.time;
		campPositionOld = playerT.position;

		playerEntity.OnDeath += OnPlayerDeath;


		map = FindObjectOfType <MapGenerator> ();

		NextWave ();
	}

	void Update ()
	{
		if (isDisabled)
			return;

		if (Time.time > nextCampCheckTime)
		{
			nextCampCheckTime = Time.time + timeBetweenCampingChecks;

			isCamping = (Vector3.Distance (playerT.position, campPositionOld) < campThresholdDistance);

			campPositionOld = playerT.position;
		}


		if (enemiesRemainingToSpawn > 0f && Time.time > nextSpawnTime)
		{
			enemiesRemainingToSpawn --;
			nextSpawnTime = Time.time + currentWave.timeBetweenWaves;

			StartCoroutine (SpawnEnemy ());
		}
	}

	IEnumerator SpawnEnemy ()
	{
		float spawnDelay = 1f;
		float tileFlashSpeed = 4f;

		Transform spawnTile = map.GetRandomOpenTile ();

		if (isCamping)
		{
			spawnTile = map.GetTileFromPosition (playerT.position);
		}

		Material tileMat = spawnTile.GetComponent <Renderer> ().material;
		Color initialColor = tileMat.color;
		Color flashColor = Color.red;
		float spawnTimer = 0f;

		while (spawnTimer < spawnDelay)
		{
			tileMat.color = Color.Lerp (initialColor, flashColor, Mathf.PingPong (spawnTimer * tileFlashSpeed, 1f));

			spawnTimer += Time.deltaTime;
			yield return null;
		}

		Enemy spawnedEnemy = Instantiate (enemy, spawnTile.position + Vector3.up, Quaternion.identity) as Enemy;
		spawnedEnemy.OnDeath += OnEnemyDeath;
	}

	void OnPlayerDeath ()
	{
		isDisabled = true;
	}

	void OnEnemyDeath ()
	{
		enemiesRemainingAlive--;

		if (enemiesRemainingAlive == 0f)
		{
			NextWave ();
		}
	}

	void ResetPlayerPosition ()
	{
		playerT.position = map.GetTileFromPosition (Vector3.zero).position + Vector3.up  * 3;
	}

	void NextWave ()
	{
		currentWaveNumber++;

		if (currentWaveNumber - 1 < waves.Length)
		{
			currentWave = waves [currentWaveNumber - 1];

			enemiesRemainingToSpawn = currentWave.enemyCount;
			enemiesRemainingAlive = enemiesRemainingToSpawn;

			if (OnNewWave != null)
			{
				OnNewWave (currentWaveNumber);
			}
			ResetPlayerPosition ();
		}
	}

	[System.Serializable]
	public class Wave {
		public int enemyCount;
		public float timeBetweenWaves;
	}
}

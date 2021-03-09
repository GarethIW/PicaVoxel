using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShmupEnemyManager : MonoBehaviour {

    public static ShmupEnemyManager Instance;
    public Object PlanePrefab;

    private const int MAX_ENEMIES = 20;

    public List<ShmupPlane> Enemies;

    public float StartDelay = 5f;
    public Vector2 SpawnTimeRange = new Vector2(1.5f,4f);

    private bool started = false;
    private float thisSpawnTime = 0f;
    private float timer = 0f;

    // Use this for initialization
    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        for (int i = 0; i < MAX_ENEMIES; i++)
        {
            ShmupPlane e = ((GameObject)Instantiate(PlanePrefab)).GetComponent<ShmupPlane>();
            e.transform.parent = transform;
            e.gameObject.SetActive(false);
            Enemies.Add(e);
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (!started)
        {
            if (timer >= StartDelay)
                started = true;
            return;
        }

        // Spawn a plane on the timer
        if (timer >= thisSpawnTime)
        {
            timer = 0f;
            thisSpawnTime = Random.Range(SpawnTimeRange.x, SpawnTimeRange.y);
            Spawn(new Vector3(15f,Random.Range(-3f,3f),-4f), new Vector3(-15f,0f,0f));
        }
    }

    public void Spawn(Vector3 position, Vector3 velocity)
    {
        ShmupPlane e = Enemies.FirstOrDefault(en => !en.gameObject.activeSelf);
        if (e)
            e.Init(position, velocity);
    }
}
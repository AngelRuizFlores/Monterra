using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureGenerator : MonoBehaviour
{
    [Serializable]
    public class Creatures
    {
        public string Name;
        [Range(0, 100)] public int probability = 100;
    }

    [Header("Spawn")]
    public int delayCreature = 5;
    public int max = 2;
    public List<Creatures> creatures = new List<Creatures>();

    [Header("Refs")]
    [SerializeField] private Transform player;

    [Header("Safe spawn")]
    [SerializeField] private float minDistanceToPlayer = 1.2f;  // evita spawn encima
    [SerializeField] private int spawnTries = 12;               // intentos para encontrar punto libre
    [SerializeField] private float checkRadius = 0.35f;         // radio para comprobar si hay algo ocupando
    [SerializeField] private LayerMask blockMask;               // capas que bloquean spawn (Player, Walls, Mon, etc.)

    private string creaturetospawn;
    private Collider2D homeZone;
    private float time;

    void Awake()
    {
        homeZone = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (max <= 0) return;

        time += Time.deltaTime;
        if (time < delayCreature) return;

        PickProbability();

        GameObject creature = PoolingManager.Instance.GetPooledObject(creaturetospawn);
        if (creature != null)
        {
            Vector3 spawnPos = FindSafeSpawnPosition();
            Quaternion spawnRot = transform.rotation;

            creature.SetActive(true);
            StartCoroutine(SpawnSafely(creature, spawnPos, spawnRot));

            max--;
        }

        time = 0f;
    }

    private IEnumerator SpawnSafely(GameObject creature, Vector3 pos, Quaternion rot)
    {
        // 1) Apaga SpriteRenderers (tú usas sprites, no meshes)
        var sprites = creature.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++) sprites[i].enabled = false;

        // 2) Apaga colliders (incluye triggers)
        var cols = creature.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        // 3) Pausa física
        var rb = creature.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 4) Coloca ya
        creature.transform.SetPositionAndRotation(pos, rot);
        if (rb != null) rb.position = pos;

        // 5) Sincroniza transforms
        Physics2D.SyncTransforms();

        // 6) Espera al siguiente tick de física
        yield return new WaitForFixedUpdate();

        // 7) Reactiva física + colliders + render
        if (rb != null) rb.simulated = true;

        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = true;

        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].enabled = true;

        // 8) Inicializa movimiento ya “limpio”
        var move = creature.GetComponent<RandomMovementBehavior>();
        if (move != null)
        {
            move.SetHomeZone(homeZone);
            move.SetPlayer(player);
            move.ResetState();
        }
    }

    private Vector3 FindSafeSpawnPosition()
    {
        Vector3 fallback = transform.position;

        for (int i = 0; i < spawnTries; i++)
        {
            Vector2 candidate = RandomPointInsideHome();
            if (player && Vector2.Distance(candidate, player.position) < minDistanceToPlayer)
                continue;

            // comprueba si está libre (Player / paredes / otros mons)
            if (Physics2D.OverlapCircle(candidate, checkRadius, blockMask) != null)
                continue;

            return candidate;
        }

        return fallback;
    }

    private Vector2 RandomPointInsideHome()
    {
        // sampling simple dentro de bounds + validación con OverlapPoint
        var b = homeZone.bounds;
        for (int k = 0; k < 20; k++)
        {
            Vector2 p = new Vector2(
                UnityEngine.Random.Range(b.min.x, b.max.x),
                UnityEngine.Random.Range(b.min.y, b.max.y)
            );

            if (!homeZone || homeZone.OverlapPoint(p))
                return p;
        }

        return transform.position;
    }

    private void PickProbability()
    {
        for (int i = 0; i < creatures.Count; i++)
        {
            int r = UnityEngine.Random.Range(0, 101);
            if (r < creatures[i].probability)
            {
                creaturetospawn = creatures[i].Name;
                return;
            }
        }

        // fallback por si ninguna entra
        creaturetospawn = creatures.Count > 0 ? creatures[0].Name : "";
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer);
    }
#endif
}

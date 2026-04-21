using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureGenerator : MonoBehaviour
{
    [Serializable]
    public class CreatureEntry
    {
        public string name;
        [Range(0, 100)] public int probability = 100;
    }

    [Header("Spawn")]
    [SerializeField] private int spawnDelay = 5;
    [SerializeField] private int maxSpawnCount = 2;
    [SerializeField] private List<CreatureEntry> creatures = new();

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Safe Spawn")]
    [SerializeField] private float minDistanceToPlayer = 1.2f;
    [SerializeField] private int spawnAttempts = 12;
    [SerializeField] private float checkRadius = 0.35f;
    [SerializeField] private LayerMask blockMask;

    private string creatureToSpawn;
    private Collider2D homeZone;
    private float timer;

    private void Awake()
    {
        homeZone = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (maxSpawnCount <= 0)
            return;

        timer += Time.deltaTime;
        if (timer < spawnDelay)
            return;

        PickCreatureByProbability();

        if (PoolingManager.Instance == null)
        {
            Debug.LogError("PoolingManager.Instance es null en CreatureGenerator.", this);
            return;
        }

        if (creatureToSpawn == null)
        {
            Debug.LogError("creatureToSpawn es null en CreatureGenerator.", this);
            return;
        }

        GameObject creature = PoolingManager.Instance.GetPooledObject(creatureToSpawn);

        if (creature != null)
        {
            Vector3 spawnPosition = FindSafeSpawnPosition();
            Quaternion spawnRotation = transform.rotation;

            creature.SetActive(true);
            StartCoroutine(SpawnSafely(creature, spawnPosition, spawnRotation));

            maxSpawnCount--;
        }

        timer = 0f;
    }

    private IEnumerator SpawnSafely(GameObject creature, Vector3 position, Quaternion rotation)
    {
        SpriteRenderer[] spriteRenderers = creature.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].enabled = false;

        Collider2D[] colliders = creature.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Rigidbody2D rb = creature.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        creature.transform.SetPositionAndRotation(position, rotation);

        if (rb != null)
            rb.position = position;

        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();

        if (rb != null)
            rb.simulated = true;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = true;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].enabled = true;
        }

        RandomMovementBehavior movement = creature.GetComponent<RandomMovementBehavior>();
        if (movement != null)
        {
            movement.SetHomeZone(homeZone);
            movement.SetPlayer(player);
            movement.ResetState();
        }
    }

    private Vector3 FindSafeSpawnPosition()
    {
        Vector3 fallbackPosition = transform.position;

        for (int i = 0; i < spawnAttempts; i++)
        {
            Vector2 candidate = RandomPointInsideHome();

            if (player != null && Vector2.Distance(candidate, player.position) < minDistanceToPlayer)
                continue;

            if (Physics2D.OverlapCircle(candidate, checkRadius, blockMask) != null)
                continue;

            return candidate;
        }

        return fallbackPosition;
    }

    private Vector2 RandomPointInsideHome()
    {
        Bounds bounds = homeZone.bounds;

        for (int i = 0; i < 20; i++)
        {
            Vector2 point = new Vector2(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y)
            );

            if (homeZone == null || homeZone.OverlapPoint(point))
                return point;
        }

        return transform.position;
    }

    private void PickCreatureByProbability()
    {
        for (int i = 0; i < creatures.Count; i++)
        {
            int roll = UnityEngine.Random.Range(0, 101);
            if (roll < creatures[i].probability)
            {
                creatureToSpawn = creatures[i].name;
                return;
            }
        }

        creatureToSpawn = creatures.Count > 0 ? creatures[0].name : string.Empty;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer);
    }
#endif
}
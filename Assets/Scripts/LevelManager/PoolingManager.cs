using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PooledItems
{
    public string Name;
    public GameObject objectToPool;
    public int amount;
}

public class PoolingManager : MonoBehaviour
{
    private static PoolingManager instance;

    public static PoolingManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PoolingManager>();
            }

            return instance;
        }
    }

    [Header("Pools")]
    [SerializeField] private List<PooledItems> pooledLists = new List<PooledItems>();

    private Dictionary<string, List<GameObject>> items;

    private void Awake()
    {
        items = new Dictionary<string, List<GameObject>>();

        for (int i = 0; i < pooledLists.Count; i++)
        {
            PooledItems pooledItem = pooledLists[i];

            items.Add(pooledItem.Name, new List<GameObject>());

            for (int j = 0; j < pooledItem.amount; j++)
            {
                GameObject pooledObject = Instantiate(pooledItem.objectToPool);
                pooledObject.SetActive(false);

                items[pooledItem.Name].Add(pooledObject);
            }
        }
    }

    public GameObject GetPooledObject(string name)
    {
        if (!items.ContainsKey(name))
        {
            return null;
        }

        List<GameObject> pooledObjects = items[name];

        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }

        return null;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class PooledItems //clase para identificar cada lista de objetos
{
    public string Name; //Nombre de la lista
    public GameObject objectToPool; //El objeto de la lista
    public int amount; //Cantidad de objetos en la lista
}

public class PoolingManager : MonoBehaviour
{
    private static PoolingManager _instance;
    public static PoolingManager Instance
    {
        get //crea la instancia
        {
            if(_instance == null)
            {
                _instance = FindFirstObjectByType<PoolingManager>();
            }
            return _instance;
        }
    }

    [SerializeField]
    private List<PooledItems> pooledLists = new List<PooledItems>();//lista de objectos

    [SerializeField]
    private Dictionary<string, List<GameObject>> _items;//diccionario que guarda cada objeto

    void Awake()
    {
        _items = new Dictionary<string, List<GameObject>>();

        for (int i = 0; i < pooledLists.Count; i++) //para cada lista de objetos
        {

            PooledItems l = pooledLists[i];
            _items.Add(l.Name, new List<GameObject>()); //creamos una entrada en
                                                       //en el dictionary
            for (int j = 0; j < l.amount; j++)        //y anyadimos las copias
            {
                GameObject tmp;
                tmp = Instantiate(l.objectToPool); //crea copias
                tmp.SetActive(false); //la desactivamos
                _items[l.Name].Add(tmp); //la anyadimos a la lista
            }
        }
    }

    public GameObject GetPooledObject(string Name)
    {//Busca un objeto por su nombre y lo retorna
        if (_items.ContainsKey(Name))
        {
            List<GameObject> tmp = _items[Name];
            for (int i = 0; i < tmp.Count; i++)
            {
                if (!tmp[i].activeInHierarchy)
                {
                    return tmp[i];
                }
            }
            return null;
        }
       return null;
    }
}
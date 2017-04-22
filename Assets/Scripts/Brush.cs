using UnityEngine;
using System.Collections.Generic;

namespace Autotiles
{
    [CreateAssetMenu]
    public class Brush : ScriptableObject
    {
        public List<AutotileRule> tiles;
        public float size = 1;
        public string group = "Default";
        public string[] interactGroups = new string[] { "Default" };

        public Brush()
        {
            tiles = new List<AutotileRule>();
            tiles.Add(new AutotileRule());
        }

        public PrefabData GetPrefab(Tilemap tilemap, int x, int y)
        {
            int index = 0;
            bool[] neighbors = new bool[8];

            for (int j = y - 1; j <= y + 1; j++)
            {
                for (int i = x - 1; i <= x + 1; i++)
                {
                    if (i == x && j == y) continue; //skip the middle tile

                    Brush tile = tilemap.GetTile(i, j);
                    neighbors[index] = tile != null && CanInteractWith(tile);

                    index++;
                }
            }

            return GetPrefab(neighbors);
        }

        public PrefabData GetPrefab(bool[] neighbors)
        {
            PrefabData prefab;

            bool found = FindPrefab(neighbors, out prefab);

            if (found == false && tiles.Count > 0)
            {
                if (tiles.Count > 0)
                {
                    Debug.LogWarning("There is no suitable prefab. Returning first one.");

                    prefab = new PrefabData();
                    prefab.Prefab = tiles[0].GetRandomPrefab();
                    prefab.Rotation = tiles[0].rotation;

                    return prefab;
                }
                else
                {
                    Debug.LogError("There is no prefabs in the brush.");
                }
            }

            return prefab;
        }

        //pass 8 neighbors statuses in next pattern:
        //5 6 7
        //3   4
        //0 1 2
        //UNDONE: binary search
        private bool FindPrefab(bool[] neighbors, out PrefabData prefab)
        {
            prefab = null;

            for (int i = 0; i < tiles.Count; i++)
            {
                for (int j = 0; j < tiles[i].rule.Length; j++)
                {
                    if (tiles[i].rule[j] == NeighborState.Undefined ||
                        (tiles[i].rule[j] == NeighborState.Filled && neighbors[j]) ||
                        (tiles[i].rule[j] == NeighborState.Empty && !neighbors[j]))
                    {
                        if (j == tiles[i].rule.Length - 1)
                        {
                            prefab = new PrefabData();
                            prefab.Prefab = tiles[i].GetRandomPrefab();
                            prefab.Rotation = tiles[i].rotation;

                            return true;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return false;
        }

        private bool CanInteractWith(Brush brush)
        {
            for (int i = 0; i < interactGroups.Length; i++)
            {
                if (interactGroups[i] == brush.group) return true;
            }

            return false;
        }

        public enum NeighborState
        {
            Undefined,
            Empty,
            Filled
        }

        [System.Serializable]
        public class AutotileRule
        {
            public List<RandomPrefab> prefabs;
            public NeighborState[] rule;
            public Quaternion rotation;

            public AutotileRule()
            {
                prefabs = new List<RandomPrefab>();
                prefabs.Add(new RandomPrefab());

                rule = new NeighborState[8];
            }

            public GameObject GetRandomPrefab()
            {
                float totalWeight = 0;
                int index = prefabs.Count - 1;

                for (int i = 0; i < prefabs.Count; i++)
                {
                    totalWeight += prefabs[i].weight;
                }

                float randVal = totalWeight * Random.value;

                for (int i = 0; i < prefabs.Count; i++)
                {
                    if (randVal < prefabs[i].weight)
                    {
                        index = i;
                        break;
                    }

                    randVal -= prefabs[i].weight;
                }

                return prefabs[index].prefab;
            }
        }

        [System.Serializable]
        public class RandomPrefab
        {
            public GameObject prefab;
            public float weight = 1f;
        }

        public class PrefabData
        {
            public GameObject Prefab { get; set; }
            public Quaternion Rotation { get; set; }
        }
    }
}
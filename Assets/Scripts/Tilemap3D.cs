using UnityEngine;

namespace Autotiles
{
    [SelectionBase]
    public class Tilemap3D : MonoBehaviour
    {
        private const int DefaultSize = 10;

        [SerializeField]
        private float tileSize = 1;

        [SerializeField]
        private int width = DefaultSize;
        [SerializeField]
        private int height = DefaultSize;

        [SerializeField]
        private Brush3D[] tiles = new Brush3D[DefaultSize * DefaultSize];
        [SerializeField]
        private GameObject[] instances = new GameObject[DefaultSize * DefaultSize];

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public float TileSize { get { return tileSize; } }

        public Brush3D GetTile(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return tiles[TileIndex(x, y)];
            }

            return null;
        }

        public GameObject GetTileInstance(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return instances[TileIndex(x, y)];
            }

            return null;
        }

        public void SetTile(int x, int y, Brush3D brush)
        {
            tiles[TileIndex(x, y)] = brush;

            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i >= 0 && i < Width && j >= 0 && j < Height)
                    {
                        UpdateInstance(i, j);
                    }
                }
            }
        }

        private int TileIndex(int x, int y)
        {
            return x + y * Width;
        }

        private Vector3 InstancePosition(int x, int y)
        {
            return new Vector3(x * tileSize + tileSize * 0.5f, 0, y * tileSize + tileSize * 0.5f);
        }

        private GameObject CreateInstance(Brush3D brush, int x, int y)
        {
            if (brush == null) return null;

            var prefab = brush.GetPrefab(this, x, y);
            if (prefab != null)
            {
                GameObject instance = Instantiate<GameObject>(prefab.Prefab);

                instance.transform.parent = transform;
                instance.transform.localPosition = InstancePosition(x, y);
                instance.transform.localRotation = instance.transform.rotation * prefab.Rotation;

                return instance;
            }
            return null;
        }

        private void UpdateInstance(int x, int y)
        {
            int index = TileIndex(x, y);
            var obj = instances[index];

            if (obj != null)
            {
                DestroyImmediate(obj);
            }

            if (tiles[index] != null)
            {
                instances[index] = CreateInstance(tiles[index], x, y);
            }
        }

        public void Clear()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int index = TileIndex(x, y);
                    var obj = instances[index];

                    if (obj != null)
                    {
                        DestroyImmediate(instances[index]);
                    }

                    tiles[index] = null;
                    instances[index] = null;
                }
            }
        }

        public void Rebuild()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    UpdateInstance(x, y);
                }
            }
        }

        public void ChangeTileSize(float size)
        {
            tileSize = size;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    GameObject inst = instances[TileIndex(x, y)];
                    if (inst != null)
                    {
                        inst.transform.localPosition = InstancePosition(x, y);
                    }
                }
            }
        }

        public void Resize(int width, int height)
        {
            //Destroy cutted instances
            for (int x = width; x < this.width; x++)
            {
                for (int y = 0; y < this.height; y++)
                {
                    var obj = instances[TileIndex(x, y)];

                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }
            }
            for (int y = height; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    var obj = instances[TileIndex(x, y)];

                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }
            }

            //Create new arrays
            var newTiles = new Brush3D[width * height];
            var newInstances = new GameObject[width * height];

            for (int x = 0; x < Mathf.Min(this.width, width); x++)
            {
                for (int y = 0; y < Mathf.Min(this.height, height); y++)
                {
                    int index = TileIndex(x, y);
                    int newIndex = x + y * width;

                    newTiles[newIndex] = tiles[index];
                    newInstances[newIndex] = instances[index];
                }
            }

            tiles = newTiles;
            instances = newInstances;

            this.width = width;
            this.height = height;

            //Update tiles at border
            for (int x = 0; x < width; x++)
            {
                UpdateInstance(x, height - 1);
            }
            for (int y = 0; y < height - 1; y++)
            {
                UpdateInstance(width - 1, y);
            }
        }
    }
}

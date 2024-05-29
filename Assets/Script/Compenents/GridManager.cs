using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Components
{
    public class GridManager : SerializedMonoBehaviour
    {
        [BoxGroup(Order = 999)]
        [TableMatrix(SquareCells = true), OdinSerialize]
        private Tile[,] _grid;

        [SerializeField] private List<GameObject> _tilePrefabs;

        private int _gridSizeX;
        private int _gridSizeY;

        private Tile DrawTile(Rect rect, Tile tile)
        {
            UnityEditor.EditorGUI.DrawRect(rect, Color.blue);
            return tile;
        }

        [Button]
        private void CreateGrid(int sizeX, int sizeY)
        {
            _gridSizeX = sizeX;
            _gridSizeY = sizeY;

            if (_grid != null)
            {
                foreach (Tile o in _grid)
                {
                    DestroyImmediate(o.gameObject);
                }
            }

            _grid = new Tile[_gridSizeX, _gridSizeY];

            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    Vector2Int coord = new Vector2Int(x, _gridSizeY - y - 1);
                    Vector3 pos = new Vector3(coord.x, coord.y, 0f);

                    
                    List<GameObject> possibleTiles = new List<GameObject>(_tilePrefabs);

                    
                    if (y > 1)
                    {
                        Tile upperTile1 = _grid[x, y - 1];
                        Tile upperTile2 = _grid[x, y - 2];
                        if (upperTile1 != null && upperTile2 != null && upperTile1.ID == upperTile2.ID)
                        {
                            possibleTiles.RemoveAll(tilePrefab => tilePrefab.GetComponent<Tile>().ID == upperTile1.ID);
                        }
                    }

                    
                    if (x > 1)
                    {
                        Tile leftTile1 = _grid[x - 1, y];
                        Tile leftTile2 = _grid[x - 2, y];
                        if (leftTile1 != null && leftTile2 != null && leftTile1.ID == leftTile2.ID)
                        {
                            possibleTiles.RemoveAll(tilePrefab => tilePrefab.GetComponent<Tile>().ID == leftTile1.ID);
                        }
                    }

                    
                    int randomIndex = Random.Range(0, possibleTiles.Count);
                    GameObject tilePrefabRandom = possibleTiles[randomIndex];
                    GameObject tileNew = Instantiate(tilePrefabRandom, pos, Quaternion.identity);
                    Tile tile = tileNew.GetComponent<Tile>();
                    tile.Construct(coord);
                    _grid[x, y] = tile;
                }
            }
        }
    }
}

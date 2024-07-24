﻿using System.Collections.Generic;
using System.Linq;
using Extensions.System;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Components
{ 
    public partial  class GridManager 
    {
        
#if UNITY_EDITOR
      private Tile DrawTile(Rect rect,Tile tile)
      {
          if (tile == false) return tile;

          Texture2D preview = AssetPreview.GetAssetPreview(tile.gameObject);
          rect = rect.Padding(3);
          EditorGUI.DrawPreviewTexture(rect, preview);
          
            return tile;
        }
      [Button]
      private void TestGridDir(Vector2 input ){Debug.LogWarning((GridF.GetGridDir(input)));}
      //
      // [Button]
      // private void TestGameOver()
      // {
      //     bool isGameOver = IsGameOver(out Tile hintTile, out GridDir hintDir);
      //     Debug.LogWarning(($"isGameOver:{isGameOver}, hintTile{hintTile}, hintDir{hintDir}",hintTile));
      // }
      
        private void OnDrawGizmos()
        {
            if (_lastMatches == null)
            {
                return;                
            }

            if (_lastMatches.Count == 0)
            {
                return;
            }
            Gizmos.color = Color.blue;

            foreach (Tile tile in _lastMatches.SelectMany( e => e))
            {
                if(! tile ) continue;
                Gizmos.DrawWireCube(tile.transform.position,Vector3.one);
            }
        }

        [Button]
        // Button oluşturduk
        private void CalculateBounds()
        {
            _gridBounds = new Bounds();
            // Yeni bir bounds nesnesi oluşturuyoruz
            
            foreach (Tile tile in _grid)
                // gridi dönüyoruz 
            {
                Bounds spriteBounds = tile.GetComponent<SpriteRenderer>().bounds;
                _gridBounds.Encapsulate(spriteBounds);
                // Tile SpriteRenderere bileşenin boundslarını alıyor (sınır)
                // Bu sınırları Encapsulate ile _gridbounds a ekliyoruz
            }

            foreach (GameObject border in _gridBorders)
            {
                Bounds spriteBounds = border.GetComponentInChildren<SpriteRenderer>().bounds;
                _gridBounds.Encapsulate(spriteBounds);
                // Bu işlemde borderlar için
            }
        }
        
        
        [Button]
        private void CreateGrid(int sizeX, int sizeY)
        {
            _gridSizeX = sizeX; // yukarıda tanımladığımız değişkenler burada ki parametlerden alacaklar.
            _gridSizeY = sizeY;
            
            if (_grid != null)
            {
                foreach (Tile o in _grid)
                {
                    DestroyImmediate(o.gameObject);
                }
                // Eğer grid boş değilse bütün grid dön onları yok et
            }

            _grid = new Tile[_gridSizeX, _gridSizeY];
            // Burada x ve y olmak üzere tile yarattık 
            
            for (int x = 0; x < _gridSizeX; x++)
            for (int y = 0; y < _gridSizeY; y++)
                // Burada nested loop ile gridin her hücersini dolaşıyoruz
            {
                List<int> spawnableIds = new(_prefabIds);  // Her hücre için uygun renkleri belirtiyor
                Vector2Int coords = new(x, _gridSizeY - y - 1); // Burada ters çevirme işlemi yapıyoruz.
                Vector3 pos = new(coords.x, coords.y, 0f); // Vector3 pos değişkenine yeni x.y.z yi atıyoruz.

                _grid.GetSpawnableColors(coords, spawnableIds);
                int randomId = spawnableIds.Random();
                // Rast gele bir renk seçiyor
                
                GameObject tilePrefabRandom = _tilePrefabs[randomId];
                GameObject tileNew = PrefabUtility.InstantiatePrefab(tilePrefabRandom,transform) as GameObject; // Random Prefab yaratmak
                tileNew.transform.position = pos;
                // Seçilen tipe uygun Prefab Instatiate ediyor
                
                Tile tile = tileNew.GetComponent<Tile>();
                tile.Construct(coords);
                
                // Yeni oluşan tile ı doğru poz yerleştiriyor
                 // Tile nesnesine koordinatlarını atıyor.
                

                _grid[coords.x, coords.y] = tile; // Ters y kord. Tile atarken dikkat!!
                // Tileları  grid dizisinde saklıyor.
            }
            
            CalculateBounds();
            GenerateTileBG();
            GenerateBorders();
        }

        [Button]
        private void GenerateTileBG()
        {
            _tileBGs.DoToAll(DestroyImmediate);
            _tileBGs = new List<GameObject>();

            foreach (Tile tile  in _grid)
            {
                Vector3 tileWorldPos = tile.transform.position;

                GameObject tileBG = PrefabUtility.InstantiatePrefab
                // Burada GameObject tipinden tileBg değişken ürettik.prefabUtility.Insta.. ile prefab örneği attık.
                (
                 _tileBGPrefab,
                 _bGTrans
                ) as GameObject;
                
                tileBG.transform.position = tileWorldPos;
                _tileBGs.Add(tileBG);

            }
        }

        [Button]
        private void GenerateBorders()
        {
            _gridBorders.DoToAll(DestroyImmediate);
            //DestroyImmediate: bir objenin anında hiyerarşiden ve sahneden silinmesini sağlayan bir fonksiyondur. 
            _gridBorders = new List<GameObject>();

            Tile botLeftCorner = _grid[0, 0];
            InstantiateBorder(botLeftCorner.transform.position, _borderBotLeft);
            Tile topRightCorner = _grid[_grid.GetLength(0) - 1 , _grid.GetLength(1) - 1];
            InstantiateBorder(topRightCorner.transform.position, _borderTopRight);
            Tile botRightCorner = _grid[_grid.GetLength(0) - 1, 0];
            InstantiateBorder(botRightCorner.transform.position, _borderBotRight);
            Tile topLeftCorner = _grid[0,_grid.GetLength(1) - 1];
            InstantiateBorder(topLeftCorner.transform.position, _borderTopLeft);

            for (int x = 0; x < _grid.GetLength(0);x++)
            {
                Tile tileBot = _grid[x, 0];
                Tile tileTop = _grid[x, _grid.GetLength(1) - 1];
                
                InstantiateBorder(tileBot.transform.position, _borderBot);
                InstantiateBorder(tileTop.transform.position,_borderTop);
            }

            for (int y = 0; y < _grid.GetLength(1); y++)
            {
                Tile tileLeft = _grid[0, y];
                Tile tileRight = _grid[_grid.GetLength(0) - 1 ,y];

                InstantiateBorder(tileLeft.transform.position, _borderLeft);
                InstantiateBorder(tileRight.transform.position, _borderRight);
            }
        }

        private void InstantiateBorder(Vector3 tileWPos, GameObject borderPrefab)
        {
            GameObject newBorder = PrefabUtility.InstantiatePrefab(
                borderPrefab,
                _borderTrans
                
                ) as GameObject;
            newBorder.transform.position = tileWPos;
            _gridBorders.Add(newBorder);
        }
#endif
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Components.UI;
using DG.Tweening;
using Events;
using Extensions.DoTween;
using Extensions.System;
using Extensions.Unity;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Sequence = DG.Tweening.Sequence;


namespace Components
{
    public partial class GridManager : SerializedMonoBehaviour, ITweenContainerBind
    {
        [Inject] private GridEvents GridEvents { get; set; }
        [Inject] private InputEvents InputEvents { get; set; }

        [BoxGroup(Order = 999)]
#if UNITY_EDITOR

        [TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawTile))]
#endif
        [OdinSerialize]
        private Tile[,] _grid; // Tileden coklu array oluşuturuyoruz _gird adında.

        [SerializeField] private List<GameObject> _tilePrefabs; // Listede GameObject tipinde tutuyoruz 
        [SerializeField] private int _gridSizeX; // 2 tane X ve y değişken tanımladık.
        [SerializeField] private int _gridSizeY;
        [SerializeField] private List<int> _prefabIds; //Prefab Idleri tutacak int liste
        [SerializeField] private Bounds _gridBounds;
        [SerializeField] private Transform _transform;
        [SerializeField] private List<GameObject> _tileBGs = new(); //GameObjelerden tile arka plan
        [SerializeField] private List<GameObject> _gridBorders = new(); // ''  grid'in Çerçevesi
        [SerializeField] private GameObject _tileBGPrefab; // arka Plan prefab        
        [SerializeField] private Transform _bGTrans; // transfrom
        [SerializeField] private GameObject _borderTopLeft;
        [SerializeField] private GameObject _borderTopRight;
        [SerializeField] private GameObject _borderBotLeft;
        [SerializeField] private GameObject _borderBotRight;
        [SerializeField] private GameObject _borderLeft;
        [SerializeField] private GameObject _borderRight;
        [SerializeField] private GameObject _borderTop;
        [SerializeField] private GameObject _borderBot;
        [SerializeField] private Transform _borderTrans;
        private Tile _selectedTile; // Seçilen Tile
        private Vector3 _mouseDownPos; // Mouse aşağı hareketi
        private Vector3 _mouseUpPos; // Mouse Yukarı Hareketi
        private List<MonoPool> _tilePoolsByPrefabID; // Havuzdaki Prefab Idlerin tutulduğu yer
        private MonoPool _tilePool0;
        private MonoPool _tilePool1;
        private MonoPool _tilePool2;
        private MonoPool _tilePool3;
        private Tile[,] _tilesToMove; // Tile hareketi için yaratılan gridteki tile
        [OdinSerialize] private List<List<Tile>> _lastMatches;
        private Tile _hintTile; // Tile hareket ipucu
        private GridDir _hintDir; // İpucu yönü 
        private Sequence _hintTween;
        private Coroutine _destroyRoutine;
        public ITweenContainer TweenContainer { get; set; }
        private Coroutine _hintRoutine;
        
        [SerializeField] private int _scoreMulti;
        
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private PlayerScoreTMP _playerScoreTMP;
        [SerializeField] private TMP_Text gameOverScoreText;
        private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _matchParticlePrefab;
        
        
        private Dictionary<int, MonoPool> _powerupPoolsByPrefabID; 
        private const int horizontalPowerup = 2;   //HorizPowerUp 2 seferde gelsin
        private const int verticalBombPowerup = 4;  // Vertical 4 seferde
        private const int bombPowerup = 6;          // bomb 6 seferde.
        private bool _horizontalPowerupPresent = false;
        private bool _verticalPowerupPresent = false;
        private bool _bombPowerupPresent = false;
        [SerializeField] private List<GameObject> _tilePowerupPrefabs;  //PowerUpTileları burada tutuyoruz
        [SerializeField] private List<int> _powerupPrefabIDs;  //PowerUpTile idlerini burada tutuyoruz
        
        private void Awake()
        {
            _tilePoolsByPrefabID = new List<MonoPool>();
            // Burada MonoPool  türünde yeni bir liste oluşturduk
            
            _powerupPoolsByPrefabID = new Dictionary<int, MonoPool>();
            //power
            

            for (int prefabId = 0; prefabId < _prefabIds.Count; prefabId++)
                // Bu, _prefabIds listesindeki her bir eleman için bir döngü başlatır.
                // Her döngü, bir prefab ID'sini temsil eder
            {
                MonoPool tilePool = new
                (
                    new MonoPoolData(
                        _tilePrefabs[prefabId],
                        10,
                        _transform
                    )
                    //her prefab ID'si için yeni bir MonoPool nesnesi oluşturur.
                    // Dataya verdiğimiz parametreler
                    // _tilePrefabs[] kullanılacak Prefab
                    // 10 pool başlangıc boyut
                    // _transform: Havuzdaki nesnelerin parent transform'u
                );

                _tilePoolsByPrefabID.Add(tilePool);
                // Oluşturulan MonoPoola, _tilePoolsByPrefabIDleri eklenr.
            }
            
            // Awake PowerUp 
            for (int i = 0; i <_powerupPrefabIDs.Count; i++)
                // For ile powerupPrefabIDs her PowerUpTile için calışıyor
            {
                
                int powerupId = _tilePrefabs.Count + i;
                //Bu, normal Tile prefablarının sayısına i'yi ekleyerek benzersiz bir ID oluşturuyor.
                // Yeni bir MonoPool nesnesi oluşturuluyor

                
                MonoPool powerupPool = new MonoPool(
                    new MonoPoolData(
                        _tilePowerupPrefabs[i],
                        5,
                        _transform
                    )
                );
                _powerupPoolsByPrefabID.Add(powerupId, powerupPool);
            }
            // bu kodun amacı PowerUpTilelarını Pool oluşturuyor ve Dict'ekliyor
            
           
            
            TweenContainer = TweenContain.Install(this);
            // Bu satır animasyon için kullanılıyor
        }

        private void Start()
        {
            for (int x = 0; x < _grid.GetLength(0); x++)
            for (int y = 0; y < _grid.GetLength(1); y++)
                // Nested _grid 2boyutlu dize üzerinde İterasyon yapılıyor
            {
                Tile tile = _grid[x, y];
                //Mevcut grid konumundaki Tile nesnesini alır.
                SpawnTile(tile.ID, _grid.CoordsToWorld(_transform, tile.Coords), tile.Coords);
                // Yeni bir tile oluşturur. tile.ID'yi, dünya koordinatlarını ve grid koordinatlarını kullanır.

                tile.gameObject.Destroy();
                // Orijnal tile nesnesini yok eder
            }

            IsGameOver(out _hintTile, out _hintDir);
            // Oyunun bitip bitmediğini kontrol eder
            GridEvents.GridLoaded?.Invoke(_gridBounds);
            GridEvents.InputStart?.Invoke();
            // Grid yüklendiğinde ve input başladığında olayları tetikler.
            gameOverPanel = GameObject.Find("GameOverCanvas");
            // GameOverCanvas" adlı GameObject'i bulur.


            
            if (gameOverPanel != null)
            {
                _canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
                gameOverScoreText = gameOverPanel.GetComponentInChildren<TMP_Text>();

                if (_canvasGroup == null)
                {
                    Debug.LogError("Canvas Group null");
                    return;
                }
                // GameOver panelini ve ilgili bileşenlerini bulur. Hata durumunda log kaydeder.
            }

            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
           // GameOver panelini başlangıçta görünmez ve etkileşimsiz yapar.
            
            gameOverPanel.SetActive(false);
            _playerScoreTMP = FindObjectOfType<PlayerScoreTMP>();
            // Oyuncu skorunu gösteren TextMeshPro bileşenini bulur.

        }

        private void OnEnable()
        {
            RegisterEvents();
        }


        private void OnDisable()
        {
            UnRegisterEvents();
            TweenContainer.Clear();
        }

        private bool CanMove(Vector2Int tileMoveCoord) => _grid.IsInsideGrid(tileMoveCoord);

        private bool HasAnyMatches(out List<List<Tile>> matches)
        {
            matches = new List<List<Tile>>();

            foreach (Tile tile in _grid)
            {
                
                List<Tile> matchesAll = _grid.GetMatchesXAll(tile);
                matchesAll.AddRange(_grid.GetMatchesYAll(tile));

                if (matchesAll.Count > 0)
                {
                    matches.Add(matchesAll);
                }
            }

            matches = matches.OrderByDescending(e => e.Count).ToList();
            // OrderByDescending :  IEnumerable<T> koleksiyonlarını sıralamak için kullanılır
            // ve koleksiyondaki öğeleri belirtilen sıralama ölçütüne göre azalan sırayla sıralar

            for (int i = 0; i < matches.Count; i++)
            {
                List<Tile> match = matches[i];

                matches[i] = match.Where(e => e.ToBeDestroyed == false).DoToAll(e => e.ToBeDestroyed = true).ToList();
                // Bu komut match öğelerinden False olanları alır ve True yapar Listeye Ekler.Bir koşula göre çalışır.
            }

            matches = matches.Where(e => e.Count > 2).ToList();
            return matches.Count > 0;
            // Bu method Oyundaki tileleri kontrol ediyor eğer 3 veya daha fazla yatay
            // Dikey bir arada ise eşleşme olarak sayar.Eşleşen taşları listede tutar
            // Bu eşleşmeleri kontrol eder ve Gridten cıkarır.Eşleşen varsa True yoksa False döndürür.

        }

        private bool IsGameOver(out Tile hintTile, out GridDir hintDir)
        {
            hintDir = GridDir.Null;
            hintTile = null;

            List<Tile> matches = new();

            foreach (Tile fromTile in _grid)
            {
                hintTile = fromTile;

                Vector2Int thisCoord = fromTile.Coords;

                Vector2Int leftCoord = thisCoord + Vector2Int.left;
                Vector2Int topCoord = thisCoord + Vector2Int.up;
                Vector2Int rightCoord = thisCoord + Vector2Int.right;
                Vector2Int botCoord = thisCoord + Vector2Int.down;

                if (_grid.IsInsideGrid(leftCoord))
                {
                    Tile toTile = _grid.Get(leftCoord);

                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));

                    _grid.Swap(toTile, fromTile);

                    if (matches.Count > 0)
                    {
                        hintDir = GridDir.Left;
                        return false;
                    }
                }

                if (_grid.IsInsideGrid(topCoord))
                {
                    Tile toTile = _grid.Get(topCoord);
                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));

                    _grid.Swap(toTile, fromTile);

                    if (matches.Count > 0)
                    {
                        hintDir = GridDir.Up;
                        return false;
                    }
                }

                if (_grid.IsInsideGrid(rightCoord))
                {
                    Tile toTile = _grid.Get(rightCoord);
                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));

                    _grid.Swap(toTile, fromTile);

                    if (matches.Count > 0)
                    {
                        hintDir = GridDir.Right;
                        return false;
                    }
                }

                if (_grid.IsInsideGrid(botCoord))
                {
                    Tile toTile = _grid.Get(botCoord);
                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));

                    _grid.Swap(toTile, fromTile);

                    if (matches.Count > 0)
                    {
                        hintDir = GridDir.Down;
                        return false;
                    }
                }
            }

            return matches.Count == 0;
            //Method Grid Üzerinde her tile için Döngü yapıyor
            // Her tile için 4 yönü kontrol ediyoruz
            // Eğer eşleşme varsa, bu hareketi ipucu olarak kaydediyor ve oyunun bitmediğini belirtiyor (false döndürüyor).
            // Eşleşme yoksa, Tile haline getiriyor ve diğer yönleri kontrol etmeye devam ediyor.

        }

        private void SpawnAndAllocateTiles()
        {
            // _tilesToMove = new Tile[_gridSizeX,_gridSizeY];
            //
            // for(int y = 0; y < _gridSizeY; y ++)
            // {
            //     int spawnStartY = 0;
            //     
            //     for(int x = 0; x < _gridSizeX; x ++)
            //     {
            //         Vector2Int thisCoord = new(x, y);
            //         Tile thisTile = _grid.Get(thisCoord);
            //
            //         if(thisTile) continue;
            //
            //         int spawnPoint = _gridSizeY;
            //
            //         for(int y1 = y; y1 <= spawnPoint; y1 ++)
            //         {
            //             if(y1 == spawnPoint)
            //             {
            //                 if(spawnStartY == 0)
            //                 {
            //                     spawnStartY = thisCoord.y;
            //                 }
            //             
            //                 MonoPool randomPool = _tilePoolsByPrefabID.Random();
            //                 Tile newTile = SpawnTile
            //                 (
            //                     randomPool, 
            //                     _grid.CoordsToWorld(_transform, new Vector2Int(x, spawnPoint)),
            //                     thisCoord
            //                 );
            //             
            //                 _tilesToMove[thisCoord.x, thisCoord.y] = newTile;
            //                 break;
            //             }
            //
            //             Vector2Int emptyCoords = new(x, y1);
            //
            //             Tile mostTopTile = _grid.Get(emptyCoords);
            //
            //             if(mostTopTile)
            //             {
            //                 _grid.Set(null, mostTopTile.Coords);
            //                 _grid.Set(mostTopTile, thisCoord);
            //             
            //                 _tilesToMove[thisCoord.x, thisCoord.y] = mostTopTile;
            //
            //                 break;
            //             }
            //         }
            //     }
            // }
            //
            // StartCoroutine(RainDownRoutine());
            {
                _tilesToMove = new Tile[_gridSizeX, _gridSizeY];
                // Hareket edecek Tile için bir dizi oluşturduk

                // Nested Loop Gridin tüm bölümlerini taramak için kullanılıyor
                for (int y = 0; y < _gridSizeY; y++)
                {
                    int spawnStartY = 0;

                    for (int x = 0; x < _gridSizeX; x++)
                    {
                        Vector2Int thisCoord = new(x, y);
                        Tile thisTile = _grid.Get(thisCoord);

                        if (thisTile) continue;
                        // Burası Mevcut Gridi Kontrol ediyor Grid doluysa bir sonra ki gridegeçiyor
                        
                        int spawnPoint = _gridSizeY;

                        for (int y1 = y; y1 <= spawnPoint; y1++)
                            // Boş gridlerin üstünde ki grid kontrol etmemizi sağlıyor
                        {
                            if (y1 == spawnPoint)
                            {
                                if (spawnStartY == 0)
                                {
                                    spawnStartY = thisCoord.y;
                                }

                                // MonoPool randomPool = _tilePoolsByPrefabID.Random();

                                
                                // PowerUp
                                Tile newTile = SpawnRegularOrPowerupTile(new Vector2Int(x, spawnPoint), thisCoord);
                                //Method Cağırılıyor ya düz tile ya da powerTile oluştuyor    
                                
                                
                                // Tile newTile = SpawnTile

                                // randomPool,
                                // _grid.CoordsToWorld(_transform, new Vector2Int(x, spawnPoint)),
                                // thisCoord
                                // );

                                _tilesToMove[thisCoord.x, thisCoord.y] = newTile;
                                break;
                            }


                            // bu kısım sütunun En üstüne ulaşıldığında yeni parça oluşturuyor
                            Vector2Int emptyCoords = new(x, y1);

                            Tile mostTopTile = _grid.Get(emptyCoords);

                            if (mostTopTile)
                            {
                                _grid.Set(null, mostTopTile.Coords);
                                _grid.Set(mostTopTile, thisCoord);

                                _tilesToMove[thisCoord.x, thisCoord.y] = mostTopTile;

                                break;
                            }
                            //Bu kısım, yukarıdaki dolu bir hücre bulunduğunda, o parçayı boş hücreye taşıyor
                        }
                    }
                }

                StartCoroutine(RainDownRoutine());
                //Son olarak, bu satır parçaların düşme animasyonunu başlatıyor.

            }
        }



        private Tile SpawnTile(MonoPool randomPool, Vector3 spawnWorldPos, Vector2Int spawnCoords)
        {
            // 3 parametre Alıyor method
            //önceden oluşturulmuş nesneleri içeren bir havuz (object pool).
            //spawnWorldPos: Yeni Tile dünya koordinatlarındaki konumu.
            // spawnCoords: Yeni Tile ızgara koordinatları.
               
            Tile newTile = randomPool.Request<Tile>();
            newTile.Teleport(spawnWorldPos);
            _grid.Set(newTile, spawnCoords);
            return newTile;
            
            // Burada Yeni bir NewTile yaratıyoruz. Havuzdan bir Tile istiyor
            // Yeni belirlenen Tile teleport İle WorldPos göre ışınlıyor
            // NewTile'ı gride set ediyoruz.
        }
        private Tile SpawnTile(int id, Vector3 worldPos,Vector2Int coords)=> SpawnTile(_tilePoolsByPrefabID[id], worldPos, coords);
        
        private IEnumerator RainDownRoutine()
        {
            int longestDistY = 0;
            Tween longestTween = null;
            
            for(int y = 0; y < _gridSizeY; y ++) // TODO: Should start from first tile that we are moving
            {
                bool shouldWait = false;
                
                for(int x = 0; x < _gridSizeX; x ++)
                {
                    Tile thisTile = _tilesToMove[x, y];

                    if(thisTile == false) continue;
                    
                    // Grid Boyunca Yukarıdan Aşağı doğru iterasyon yapılıyo
                    // Her sütun için Boşluklara düşmesi gerekn tile belirtiliyor
                    
                    Tween thisTween = thisTile.DoMove(_grid.CoordsToWorld(_transform, thisTile.Coords));
                    // Her karo için Anim başlatılıyor
                    shouldWait = true;

                    if(longestDistY < y)
                    {
                        longestDistY = y;
                        longestTween = thisTween;
                    }
                    // En uzun mesafe kat eden Tile'ı anim takip ediyor
                }
                

                if(shouldWait)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                // Her satın için bekleme Süresi  Tile kademeli düşmesi için
            }

            if(longestTween != null)
            {
                longestTween.onComplete += delegate
                {
                    if(HasAnyMatches(out _lastMatches))
                    {
                        StartDestroyRoutine();
                    }
                    else
                    {
                        IsGameOver(out _hintTile, out _hintDir);
                        GridEvents.InputStart?.Invoke();
                    }
                };
                // En uzun anim tamamladığın da Eşleşme varmı yok mu bakıyor
                // Eşleşme varsa yok ediyor StartDestroyRoutine İlE
                // Yeni eşleşme yoksa  oyunun bitip bitmediğini Kontrol ediyor
                // Bir sonra ki ipucu için hamle belitiyor hintTile ve hintDir den
                // Tekrar Input aktif hale geliyor
            }
            else
            {
                Debug.LogWarning("This should not have happened!");
                GridEvents.InputStart?.Invoke();
                
                // anim hiç yoksa Debug cıkıyor ve tekrar Input AKTİF oluyor
            }
        }

        private void StartDestroyRoutine()
        {
            if(_destroyRoutine != null)
            {
                StopCoroutine(_destroyRoutine);
            }
            
            _destroyRoutine = StartCoroutine(DestroyRoutine());
        }
        private IEnumerator DestroyRoutine()
        {
            foreach (List<Tile> matches in _lastMatches)
            {
                IncScoreMulti();
                matches.DoToAll(DespawnTile);

                foreach (Tile tile in matches)
                {
                    Vector3 particlePosition = _grid.CoordsToWorld(_transform, tile.Coords);
                    Instantiate(_matchParticlePrefab, particlePosition, Quaternion.identity);

                }
                GridEvents.MatchGroupDespawn?.Invoke(matches.Count * _scoreMulti);

                yield return new WaitForSeconds(0.1f);
                
            }
            SpawnAndAllocateTiles();
            TestGameOver();
            
            // foreach(List<Tile> matches in _lastMatches)
            // {
            //     IncScoreMulti();
            //     matches.DoToAll(DespawnTile);
            //     
            //     GridEvents.MatchGroupDespawn?.Invoke(matches.Count * _scoreMulti);
            //
            //     yield return new WaitForSeconds(0.1f);
            // }
        }

        private void TestGameOver()
        {
            // bool isGameOver = IsGameOver(out Tile hintTile, out GridDir hintDir);
            //
            // Debug.LogWarning($"isGameOver{isGameOver} hintTile {hintTile} hintDir{hintDir}",hintTile);
            // if (isGameOver)
            // {
            //     ShowGamePanel();
            // }
            bool isGameOver = IsGameOver(out Tile hintTile, out GridDir hintDir);

            Debug.LogWarning($"isGameOver: {isGameOver}, hintTile {hintTile}, hintDir {hintDir}", hintTile);
            if (isGameOver)
            {
                ShowGameOverPanel();
            }
        }

        private void ShowGameOverPanel()
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            Time.timeScale = 0;
            
            Debug.Log("Show gameover panel");
            gameOverPanel.SetActive(true);
            gameOverScoreText.text = $"Score:{_playerScoreTMP.GetCurrentScore()}";
        }

        private void HideGameOverPanel()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            Time.timeScale = 1;
            Debug.Log("Hide gameover panel");
            gameOverPanel.SetActive(false);
        }

        public void OnNewGameButtonClicked()
        {
            Debug.Log("New game button clicked , Restarting game");
            HideGameOverPanel();
            RestartGame();
        }

        public void RestartGame()
        {
            ClearGrid();
            ResetScoreMulti();
            StartGame();
        }

        public void ClearGrid()
        {
            foreach (Tile tile in _grid)
            {
                if (tile != null)
                {
                    DespawnTile(tile);
                }
            }
        }

        private void ResetScore()
        {
            _scoreMulti = 0;
            _playerScoreTMP.SetScore(0);
        }

        private void StartGame()
        {
            SpawnAndAllocateTiles();
        }


        private void DespawnTile(Tile tile)
        {
            _grid.Set(null, tile.Coords);
            // burada belirtilen Coordsta tile refi griden kaldırıyor.
            //Power
            if (_powerupPrefabIDs.Contains(tile.ID))
                // bu satır kaldırlan Tile ın powermı değil mi bkaıyor
            {
                if (tile.ID == _powerupPrefabIDs[0])
                {
                    _horizontalPowerupPresent = false;
                }
                else if (tile.ID == _powerupPrefabIDs[1])
                {
                    _verticalPowerupPresent = false;
                }
                else if (tile.ID == _powerupPrefabIDs[2])
                {
                    _bombPowerupPresent = false;
                }
                _powerupPoolsByPrefabID[tile.ID].DeSpawn(tile);
                return; 
                // Bu kısım, kaldırılan powerın türüne göre ilgili  false olarak ayarlıyor.
                // Yani o türdeki powerın artık oyun gridin de olmadığını işaretliyor.
            }

            _tilePoolsByPrefabID[tile.ID].DeSpawn(tile);
            // Bu satırlar, güçlendirme karosunu uygun havuza iade ediyor ve fonksiyondan çıkıyor.

            
            
            // _grid.Set(null, e.Coords);
            // _tilePoolsByPrefabID[e.ID].DeSpawn(e);
        }
        private void DoTileMoveAnim(Tile fromTile, Tile toTile, TweenCallback onComplete = null)
        {
            Vector3 fromTileWorldPos = _grid.CoordsToWorld(_transform, fromTile.Coords);
            fromTile.DoMove(fromTileWorldPos);
            Vector3 toTileWorldPos = _grid.CoordsToWorld(_transform, toTile.Coords);
            toTile.DoMove(toTileWorldPos, onComplete);
        }
        private void StartHintRoutine()
        {
            if(_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
            }

            _hintRoutine = StartCoroutine(HintRoutineUpdate());
        }
        private void StopHintRoutine()
        {
            if(_hintTile)
            {
                _hintTile.Teleport(_grid.CoordsToWorld(_transform, _hintTile.Coords));
            }
            
            if(_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }
        }
        
        private IEnumerator HintRoutineUpdate()
        {
            while(true)
            {
                yield return new WaitForSeconds(3f);
                TryShowHint();
            }
        }
        private void TryShowHint()
        {
            if(_hintTile)
            {
                Vector2Int gridMoveDir = _hintDir.ToVector();

                Vector3 moveCoords = _grid.CoordsToWorld(_transform, _hintTile.Coords + gridMoveDir);
                
                _hintTween = _hintTile.DoHint(moveCoords);
            }
        }
        private void ResetScoreMulti() {_scoreMulti = 0;}

        private void IncScoreMulti()
        {
            _scoreMulti ++;
        }
        private void RegisterEvents()
        {
            InputEvents.MouseDownGrid += OnMouseDownGrid;
            InputEvents.MouseUpGrid += OnMouseUpGrid;
            GridEvents.InputStart += OnInputStart;
            GridEvents.InputStop += OnInputStop;
        }
        private void OnInputStop()
        {
            StopHintRoutine();
        }

        private void OnInputStart()
        {
            StartHintRoutine();
            ResetScoreMulti();
        }
        private void OnMouseDownGrid(Tile clickedTile, Vector3 dirVector)
        {
            _selectedTile = clickedTile;
            _mouseDownPos = dirVector;

            if(_hintTween.IsActive())
            {
                _hintTween.Complete();
            }
        }
        
        //PowerUp
        private Tile SpawnRegularOrPowerupTile(Vector2Int spawnPoint, Vector2Int targetCoord)
        {   
            
            // spawnPoint: Tile oluşturulacağı başlangıç noktası (Grid koordinatları)
            // targetCoord: Tile ulaşması gereken hedef nokta (Grid koordinatları)
            
            
            //powerUp oluşturumalı mı diye bakılıyor.
            if (ShouldSpawnPowerup(out int powerupId))
            {
                // Eğer PowerUp oluşturulmalıysa, belirtilen powerUpID'sine sahip havuzdan bir Tile oluşturuluyor
                return SpawnTile(_powerupPoolsByPrefabID[powerupId], _grid.CoordsToWorld(_transform, spawnPoint), targetCoord);
            }
            else
            {
                // Eğer powerup oluşturulmamalıysa, rastgele bir normal tile havuzu seçiliyor
                MonoPool randomPool = _tilePoolsByPrefabID.Random();
                // Seçilen havuzdan bir tile oluşturuluyor
                return SpawnTile(randomPool, _grid.CoordsToWorld(_transform, spawnPoint), targetCoord);
            }
        }

        //PowerUp
        private bool ShouldSpawnPowerup(out int powerupId)
        {
            powerupId = -1;
           // powerupId'yi -1 olarak başlatıyoruz (geçersiz bir ID)
           
            // HorizontalPowerUp
            if (_scoreMulti == horizontalPowerup + 1 && !_horizontalPowerupPresent)
            // Eğer ScoreMulti büyükse HorizontalPowerUptan ve HPowerUp yoksa
            {
                
                powerupId = _powerupPrefabIDs[0];
                _horizontalPowerupPresent = true;
                return true;
                // HorizPowerUp Id ATANIR  h.powerUp True;
                
            }
            if (_scoreMulti == verticalBombPowerup + 1 && !_verticalPowerupPresent)
             // Eğer ScoreMulti büyükse veritcalPowerUptan ve verticalPowerUp yoksa
            {
                powerupId = _powerupPrefabIDs[1];
                _verticalPowerupPresent = true; 
                return true;
                // VerticalPowerUp Id ATANIR  V.powerUp True;
            }
            
            if (_scoreMulti == bombPowerup + 1 && !_bombPowerupPresent) 
                // Eğer ScoreMulti büyükse bombPowerUptan ve bombPowerUp yoksa
            {
                Debug.LogWarning($"ScoreMulti: {_scoreMulti}");

                powerupId = _powerupPrefabIDs[2];
                _bombPowerupPresent = true;
                return true;
                // Eğer skor çarpanı bombapowerUp bir fazlaysa ve henüz bombUp yoksa
                //  Bir uyarı log'u yazdırılır
                // BombPower ID'si atanır, bombpower varlığı işaretlenir ve true döndürülür.
            } 

            return false;
        }
        
        
        
        private void OnMouseUpGrid(Vector3 mouseUpPos)
        {
            _mouseUpPos = mouseUpPos;
            

            Vector3 dirVector = mouseUpPos - _mouseDownPos;
            // Mouse Hareketini Yönünü hepsalıyoruz  
            
            if(_selectedTile)
                // Eğer Tile seçildiyse
            {
                //PowerUp
                if (IsPowerupTile(_selectedTile))
                // Seçilen Tile PowerUpTile ise 
                {
                    
                    ActivatePowerup(_selectedTile);
                    return;
                    // burada Fonksiyon cağırıyoruz ve sonlanıyor.
                }

                if (!HasAnyMatches(out _lastMatches))
                    // Eşleşme var mı yok mu kontrol ediyoruz
                {
                    ResetScoreMulti();
                    // yoksa bu metod geliyor
                }
                
                
                Vector2Int tileMoveCoord = _selectedTile.Coords + GridF.GetGridDirVector(dirVector);
                // Hareket Edeceğimiz Tile ile kendi seçilen Tile Coords Hesaplıyoruz
                if(! CanMove(tileMoveCoord)) return;
                // Hareket geçirli mi diye bakıyoruz değilse sonlanıyor
                
                Tile toTile = _grid.Get(tileMoveCoord);
                _grid.Swap(_selectedTile, toTile);
                // Geçerli ise Tile ile hedef Tile yer değiştiriyor

                if(! HasAnyMatches(out _lastMatches))
                {
                    GridEvents.InputStop?.Invoke();
                    // Eşleşme yoksa Stoplıyoruz 
                    
                    DoTileMoveAnim(_selectedTile, toTile,
                        delegate
                        {
                            _grid.Swap(toTile, _selectedTile);
                            
                            DoTileMoveAnim(_selectedTile, toTile,
                                delegate
                                {
                                    GridEvents.InputStart?.Invoke();
                                });
                        });
                    // Burada Animasyon İşlemleri yapılıyor 
                    // Anim bitince tile eski hale geliyor
                    // Tekara Anim ile tile eski hale gelir
                    // InputStart İle işlemi başlatıyoruz
                }
                else
                {
                    GridEvents.InputStop?.Invoke();

                    DoTileMoveAnim
                    (
                        _selectedTile,
                        toTile,
                        StartDestroyRoutine
                    );
                }
                // Eğer eşleşme varsa Giriş işlemlerini Durur
                // Tile hareket ettiren Anim başlar
                // Anim bittiğin de Eşelşme tileları yok eder.
            }
            
        }
        
        //power
        private bool IsPowerupTile(Tile tile)
        { // Method Tile Tipinde tile param alıyor
          // //Amac Tile bir PowerUpTile mı ona bakılıyor.
          
            return _powerupPrefabIDs.Contains(tile.ID);
            // verilen ID _powerUpPrefabIDste var mı yok mu ona bakılıyor
            // varsa True Yani PowerUpTile değilse false düz tile
            
        }
        
        
        //power
        private void ActivatePowerup(Tile powerupTile)
        {
            // Yok edilecek Tileları tutmak için liste oluşturduk
            List<Tile> tilesToDestroy = new List<Tile>();

            // yatay powerUp Control
            if (powerupTile.ID ==_powerupPrefabIDs[0]) // 0 denme sebebi x ve y x = 0
                                                       // y = 1
            {
                for (int x = 0; x < _gridSizeX; x++)
                {
                    tilesToDestroy.Add(_grid[x, powerupTile.Coords.y]);
                }
                _horizontalPowerupPresent = false;
                //  Horizontal PowerUp Destroy all tile 
                
            }
            //Vertical PowerUp Controll
            else if (powerupTile.ID == _powerupPrefabIDs[1])
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    tilesToDestroy.Add(_grid[powerupTile.Coords.x, y]);
                }

                _verticalPowerupPresent = false;
                // Vertical PowerUp destroy all tile
            }
            
            
            
            //BOMB PowerUp Controll
            else if(powerupTile.ID == _powerupPrefabIDs[2])
            {
                Vector2Int[] adjacentDirection = new Vector2Int[]
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                    
                    // Bomb PowerUp 8 tiles that we look at from 4 sides will be destroyed
                };
                
                foreach (Vector2Int dir in adjacentDirection)
                {
                    Vector2Int adjacentCoord = powerupTile.Coords + dir;
                    if (_grid.IsInsideGrid(adjacentCoord))
                    { // Gridin içinde mi diye bakıyorum.
                        tilesToDestroy.Add(_grid[adjacentCoord.x, adjacentCoord.y]);
                    }
                }
                _bombPowerupPresent = false;
            }
            
            foreach (Tile tile in tilesToDestroy)
            {
                DespawnTile(tile);
            }
            DespawnTile(powerupTile);
            // Yok edilecek bütün Tileları yok et

            GridEvents.MatchGroupDespawn?.Invoke(tilesToDestroy.Count * _scoreMulti);
            // Yok edilen tile sayısı ile skor carpanı skoru hesapla olayı tetikle
            
            if (powerupTile.ID ==_powerupPrefabIDs [0])_horizontalPowerupPresent = false;
            else if (powerupTile.ID ==_powerupPrefabIDs [1]) _verticalPowerupPresent = false;
            else if (powerupTile.ID == _powerupPrefabIDs[2]) _bombPowerupPresent = false;
            // Kod tekrarı Yukarıda burası zaten işleme alınıyor.!!!
            
            //ScoreMulti sıfırla
            _scoreMulti = 1;
            
            // Skoru tekrar hesapla 
            int score = tilesToDestroy.Count * _scoreMulti;
            GridEvents.MatchGroupDespawn?.Invoke(score);
            // Burasıda kod tekrarı
            
            SpawnAndAllocateTiles();
            //Yeni Tile oluştur ve yerleştir

        }
            
            
        private void UnRegisterEvents()
        {
            InputEvents.MouseDownGrid -= OnMouseDownGrid;
            InputEvents.MouseUpGrid -= OnMouseUpGrid;
            GridEvents.InputStart -= OnInputStart;
            GridEvents.InputStop -= OnInputStop;
        }
    }
}



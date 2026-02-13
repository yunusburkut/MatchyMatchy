using System;
using System.Collections.Generic;
using Game.ScriptableObjects;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class GridManager : MonoBehaviour
{
    public enum TileType : byte
    {
        Blue = 0,
        Green = 1,
        Red = 2,
        Yellow = 3
    }

    private static readonly (int dx, int dy)[] Dir8 =
    {
        (-1, 0), // left
        (1, 0), // right
        (0, -1), // down
        (0, 1), // up
        (1, 1), // right up
        (-1, -1), // left down
        (-1, 1), // left up
        (1, -1) // right down
    };
    
    
    
    private static readonly (int dx, int dy)[] Dir4 =
    {
        (-1, 0), // left
        (1, 0), // right
        (0, -1), // down
        (0, 1), // up
    };
    
    private static readonly (int dx, int dy)[] Dir16 =
    {
        (-1, 0),  // left
        (1, 0),   // right
        (0, -1),  // down
        (0, 1),   // up

        (1, 1),   // right up
        (-1, -1), // left down
        (-1, 1),  // left up
        (1, -1),  // right down

        (-2, 0),  // left (2)
        (2, 0),   // right (2)
        (0, -2),  // down (2)
        (0, 2),   // up (2)

        (2, 2),   // right up (2)
        (-2, -2), // left down (2)
        (-2, 2),  // left up (2)
        (2, -2)   // right down (2)
    };
    
    private static readonly (int dx, int dy)[] Dir24 =
    {
        // 1-step (4)
        (-1, 0),  // left
        (1, 0),   // right
        (0, -1),  // down
        (0, 1),   // up

        // 1-step diagonals (4)
        (1, 1),   // right up
        (-1, -1), // left down
        (-1, 1),  // left up
        (1, -1),  // right down

        // 2-step straight (4)
        (-2, 0),  // left (2)
        (2, 0),   // right (2)
        (0, -2),  // down (2)
        (0, 2),   // up (2)

        // 2-step diagonals (4)
        (2, 2),   // right up (2)
        (-2, -2), // left down (2)
        (-2, 2),  // left up (2)
        (2, -2),  // right down (2)

        // Knight / L-moves (8): (±2,±1) and (±1,±2)
        (2, 1),
        (2, -1),
        (-2, 1),
        (-2, -1),
        (1, 2),
        (1, -2),
        (-1, 2),
        (-1, -2),
    };
    
    [Header("View")] [SerializeField] private GridView gridPrefab;
    [SerializeField] private GameObject canvasParent;

    [Header("Config")] [SerializeField] private GridSO gridSo;


    private int _width;
    private int _height;

    private TileType[] _cells;
    private GridView[] _views;
    private Vector2[] _homeAnchoredPos;
    
    private int _stamp = 1;
    private int[] _visitedStamp;   // length = _cells.Length
    private int[] _queue;          // length = _cells.Length
    private void Start()
    {
        _width = gridSo.gridX;
        _height = gridSo.gridY;

        _cells = new TileType[_width * _height];
        _views = new GridView[_width * _height];
        _homeAnchoredPos = new Vector2[_width * _height];

        CreateCenteredGridView();
        InitializeModel();
        FindColor();
        FindBlocks();
    }
    
    private void CreateCenteredGridView()
    {
        float spacing = gridSo.tileSpacing;

        float totalWidth = (_width - 1) * spacing;
        float totalHeight = (_height - 1) * spacing;

        float startX = -totalWidth * 0.5f;
        float startY = -totalHeight * 0.5f;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int index = Idx(x, y);

                var view = Instantiate(gridPrefab, canvasParent.transform);
                _views[index] = view;

                view.Initialize(x, y);

                view.PointerDown += OnGridCellPointerDown;
                view.PointerUp += OnGridCellPointerUp;


                float px = startX + x * spacing;
                float py = startY + y * spacing;

                var rt = view.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(px, py);
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                    
                    _homeAnchoredPos[index] = rt.anchoredPosition;
                }

            }
        }
    }

    private void InitializeModel()
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            var index = Random.Range(0, 4);
            _cells[i] = (TileType)index;
            _views[i].SetImage(gridSo.Sprites[index]);
        }
    }

    private Sequence _seq;
    private bool _isMoving = false;

    private void OnGridCellPointerDown(int x, int y)
    {
        _seq?.Kill();
        _seq = DOTween.Sequence();

        _isMoving = true;

        int clickedIndex = Idx(x, y);
        var clickedView = _views[clickedIndex];

        ForEachNeighbor24(x, y, (nx, ny, level) =>
        {
            int index = Idx(nx, ny);
            var view = _views[index];
            
            
            PushNeighbor(view, clickedView, pushOut: true, index: index, level);
        });
    }
    
    private void OnGridCellPointerUp(int x, int y)
    {
        _seq?.Kill();
        _seq = DOTween.Sequence();

        _isMoving = false;

        int clickedIndex = Idx(x, y);
        var clickedView = _views[clickedIndex];

        ForEachNeighbor24(x, y, (nx, ny, level) =>
        {
            int index = Idx(nx, ny);
            var view = _views[index];
            PushNeighbor(view, clickedView, pushOut: false, index: index, level);
        });
    }
    
    private void PushNeighbor(GridView view, GridView clickedView, bool pushOut, int index, float neighborLevel)
    {
        var rt = view.GetComponent<RectTransform>();
        var centerRt = clickedView.GetComponent<RectTransform>();
        if (rt == null || centerRt == null) return;

        Vector2 centerPos = centerRt.anchoredPosition;
        Vector2 pos = rt.anchoredPosition;

        Vector2 dir = pos - centerPos;
        float lenSq = dir.sqrMagnitude;
        if (lenSq < 1e-8f) return;
        
        float pushDistance = 10f * (1f / neighborLevel); 

        Vector2 target;
        if (pushOut)
        {
            float invLen = 1f / Mathf.Sqrt(lenSq);
            Vector2 outDir = dir * invLen;
            target = pos + outDir * pushDistance;
        }
        else
        {
            target = _homeAnchoredPos[index];
        }

        _seq.Join(rt.DOAnchorPos(target, 0.1f).SetEase(Ease.OutQuad));
    }
    public void ForEachNeighbor4(int x, int y, Action<int, int> visitor)
    {
        for (int i = 0; i < Dir4.Length; i++)
        {
            var (dx, dy) = Dir4[i];
            int nx = x + dx;
            int ny = y + dy;

            if (!InBounds(nx, ny)) continue;
            visitor(nx, ny);
        }
    }
    
    public void ForEachNeighbor8(int x, int y, Action<int, int> visitor)
    {
        for (int i = 0; i < Dir8.Length; i++)
        {
            var (dx, dy) = Dir8[i];
            int nx = x + dx;
            int ny = y + dy;

            if (!InBounds(nx, ny)) continue;
            visitor(nx, ny);
        }
    }
    
    public void ForEachNeighbor16(int x, int y, Action<int, int> visitor)
    {
        for (int i = 0; i < Dir16.Length; i++)
        {
            var (dx, dy) = Dir16[i];
            int nx = x + dx;
            int ny = y + dy;

            if (!InBounds(nx, ny)) continue;
            visitor(nx, ny);
        }
    }
    
    public void ForEachNeighbor24(int x, int y, Action<int, int, int> visitor)
    {
        for (int i = 0; i < Dir24.Length; i++)
        {
            var (dx, dy) = Dir24[i];
            int nx = x + dx;
            int ny = y + dy;

            if (!InBounds(nx, ny)) continue;

            int level = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == 1 ? 1 : 2;
            visitor(nx, ny, level);
        }
    }
    
    private int Idx(int x, int y) => x + y * _width;

    public bool InBounds(int x, int y) => (uint)x < (uint)_width && (uint)y < (uint)_height;

    public TileType GetCell(int x, int y)
    {
        if (!InBounds(x, y)) return TileType.Green;
        return _cells[Idx(x, y)];
    }

    public void SetCell(int x, int y, TileType value)
    {
        if (!InBounds(x, y)) return;
        _cells[Idx(x, y)] = value;
    }

    public bool TryGetNeighbor(int x, int y, int dx, int dy, out int nx, out int ny)
    {
        nx = x + dx;
        ny = y + dy;
        return InBounds(nx, ny);
    }
    
    private void FindColor()
    {
        int red = 0;
        int green = 0;
        int blue = 0;
        int yellow = 0;
        
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (GetCell(x, y) == TileType.Blue)
                {
                    blue++;
                }

                if (GetCell(x, y) == TileType.Green)
                {
                    green++;
                }
                
                if (GetCell(x, y) == TileType.Red)
                {
                    red++;
                }
                
                if (GetCell(x, y) == TileType.Yellow)
                {
                    yellow++;
                }
            }
        }
        
        Debug.Log($"Red: {red} Green: {green} Blue: {blue} Yellow: {yellow}");
    }
    
    private void EnsureBuffers()
    {
        int n = _cells.Length;
        if (_visitedStamp == null || _visitedStamp.Length != n)
            _visitedStamp = new int[n];
        if (_queue == null || _queue.Length != n)
            _queue = new int[n];
    }
    
    private void FindBlocks()
    {
        EnsureBuffers();
    
        _stamp++;
        if (_stamp == int.MaxValue)
        {
            System.Array.Clear(_visitedStamp, 0, _visitedStamp.Length);
            _stamp = 1;
        }
    
        int blockCount = 0;
        int largestBlockSize = 0;
    
        int w = _width;
        int h = _height;
        var cells = _cells;
        var visited = _visitedStamp;
        var queue = _queue;
        int stamp = _stamp;
    
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                int startIndex = row + x;
                if (visited[startIndex] == stamp) continue;
    
                var type = cells[startIndex];
                int currentSize = 0;
    
                int head = 0, tail = 0;
                visited[startIndex] = stamp;
                queue[tail++] = startIndex;
    
                while (head < tail)
                {
                    int idx = queue[head++];
                    currentSize++;
    
                    int cx = idx - (idx / w) * w; 
                    int cy = idx / w;
    
                    if (cx > 0)
                    {
                        int n = idx - 1;
                        if (visited[n] != stamp && cells[n] == type)
                        {
                            visited[n] = stamp;
                            queue[tail++] = n;
                        }
                    }
                    
                    if (cx + 1 < w)
                    {
                        int n = idx + 1;
                        if (visited[n] != stamp && cells[n] == type)
                        {
                            visited[n] = stamp;
                            queue[tail++] = n;
                        }
                    }
                    
                    if (cy > 0)
                    {
                        int n = idx - w;
                        if (visited[n] != stamp && cells[n] == type)
                        {
                            visited[n] = stamp;
                            queue[tail++] = n;
                        }
                    }
                    
                    if (cy + 1 < h)
                    {
                        int n = idx + w;
                        if (visited[n] != stamp && cells[n] == type)
                        {
                            visited[n] = stamp;
                            queue[tail++] = n;
                        }
                    }
                }
    
                if (currentSize > 2) blockCount++;
                if (currentSize > largestBlockSize) largestBlockSize = currentSize;
            }
        }
    }
}
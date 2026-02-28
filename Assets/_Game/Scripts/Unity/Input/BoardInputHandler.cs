using UnityEngine;

namespace Game.Unity
{
    using Game.Core;

    /// Converts touch/mouse input into swap requests.
    /// Attach to the board camera or a UI overlay.
    public class BoardInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Match3Controller _match3;
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private int _boardWidth  = 9;
        [SerializeField] private int _boardHeight = 9;

        private Vector2Int? _dragStart;
        private bool _inputLocked;

        private void Update()
        {
            if (_inputLocked) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var cell = ScreenToCell(Input.mousePosition);
                if (cell.HasValue) _dragStart = cell.Value;
            }
            else if (Input.GetMouseButtonUp(0) && _dragStart.HasValue)
            {
                var cell = ScreenToCell(Input.mousePosition);
                if (cell.HasValue && cell.Value != _dragStart.Value)
                    TrySwap(_dragStart.Value, cell.Value);
                _dragStart = null;
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;
            var touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _dragStart = ScreenToCell(touch.position);
                    break;
                case TouchPhase.Ended:
                    if (_dragStart.HasValue)
                    {
                        var end = ScreenToCell(touch.position);
                        if (end.HasValue && end.Value != _dragStart.Value)
                            TrySwap(_dragStart.Value, end.Value);
                        _dragStart = null;
                    }
                    break;
            }
        }

        private void TrySwap(Vector2Int a, Vector2Int b)
        {
            // Only swap adjacents
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) return;
            _match3.OnSwapInput(a, b);
        }

        private Vector2Int? ScreenToCell(Vector3 screenPos)
        {
            if (_camera == null) return null;
            var world = _camera.ScreenToWorldPoint(screenPos);
            float offsetX = (_boardWidth  - 1) * _tileSize * 0.5f;
            float offsetY = (_boardHeight - 1) * _tileSize * 0.5f;
            int x = Mathf.RoundToInt((world.x + offsetX) / _tileSize);
            int y = Mathf.RoundToInt((world.y + offsetY) / _tileSize);
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight) return null;
            return new Vector2Int(x, y);
        }

        public void SetInputLocked(bool locked) => _inputLocked = locked;
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity
{
    using Game.Core;

    /// Visual representation of a single tile.
    /// Uses DOTween if available; graceful fallback otherwise.
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color[] _colorMap; // indexed by TileColor enum

        private static readonly Color[] DefaultColors = {
            Color.red, Color.blue, Color.green, Color.yellow,
            new Color(0.5f, 0f, 0.5f), new Color(1f, 0.5f, 0f)
        };

        public void ShowColor(TileColor color)
        {
            if (_sprite == null) return;
            int idx = (int)color;
            var colors = (_colorMap != null && _colorMap.Length > idx) ? _colorMap : DefaultColors;
            _sprite.color = idx < colors.Length ? colors[idx] : Color.white;
            transform.localScale = Vector3.one;
        }

        public void ShowSpecial(TileKind kind)
        {
            if (_sprite == null) return;
            // Tint based on special type as placeholder
            _sprite.color = kind switch
            {
                TileKind.Rocket => Color.cyan,
                TileKind.Bomb   => Color.magenta,
                TileKind.Disco  => Color.white,
                _               => _sprite.color
            };
            // DOTween punch scale placeholder
#if DOTWEEN
            transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
#else
            StartCoroutine(PunchScale());
#endif
        }

        public void PlayPop()
        {
#if DOTWEEN
            transform.DOScale(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
#else
            StartCoroutine(ScaleDown());
#endif
        }

        public void MoveTo(Vector3 worldPos)
        {
#if DOTWEEN
            transform.DOMove(worldPos, 0.15f);
#else
            transform.position = worldPos;
#endif
        }

        private System.Collections.IEnumerator ScaleDown()
        {
            float t = 0f;
            var startScale = transform.localScale;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.2f);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator PunchScale()
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float s = 1f + 0.3f * Mathf.Sin(t / 0.3f * Mathf.PI);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}

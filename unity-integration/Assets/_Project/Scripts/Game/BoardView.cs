using System.Collections;
using System.Collections.Generic;
using DiceDungeon.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDungeon.Game
{
    /// <summary>
    /// 24칸 사각 트랙 렌더링 + 플레이어 말 이동.
    /// 7×7 그리드의 둘레 24칸: 0=좌하단 코너에서 시계 반대 방향(브루마블 방향).
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private readonly List<Image> _tileImages = new List<Image>();
        private readonly List<Text> _tileLabels = new List<Text>();
        private RectTransform _token;
        private RectTransform _area;

        private const int Side = 7; // 7×7 둘레 = 24칸

        public void Build(RectTransform area)
        {
            _area = area;
            for (int i = 0; i < 24; i++)
            {
                var cell = UiFactory.Rect(area, $"Tile{i}", Vector2.zero, Vector2.zero);
                cell.anchorMin = cell.anchorMax = CellAnchor(i);
                cell.sizeDelta = new Vector2(110, 110);
                var img = cell.gameObject.AddComponent<Image>();
                _tileImages.Add(img);
                _tileLabels.Add(UiFactory.Label(cell, "Icon", "", 44, Color.white, Vector2.zero, Vector2.one));
            }

            _token = UiFactory.Rect(area, "PlayerToken", Vector2.zero, Vector2.zero);
            _token.anchorMin = _token.anchorMax = CellAnchor(0);
            _token.sizeDelta = new Vector2(56, 56);
            var tokenImg = _token.gameObject.AddComponent<Image>();
            tokenImg.color = new Color(1f, 0.85f, 0.2f);
            _token.SetAsLastSibling();
        }

        public void ShowFloor(IReadOnlyList<Tile> tiles)
        {
            for (int i = 0; i < tiles.Count && i < 24; i++)
                Paint(i, tiles[i]);
            _token.anchorMin = _token.anchorMax = CellAnchor(0);
            _token.anchoredPosition = Vector2.zero;
        }

        public void Paint(int index, Tile tile)
        {
            _tileImages[index].color = TileColor(tile);
            _tileLabels[index].text = TileIcon(tile);
        }

        /// <summary>칸 단위 점프 이동 (05-백로그 Week 1: 0.15초/칸).</summary>
        public IEnumerator MoveToken(int from, int steps)
        {
            for (int s = 1; s <= steps; s++)
            {
                int idx = (from + s) % 24;
                Vector2 start = _token.anchorMin;
                Vector2 end = CellAnchor(idx);
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime / 0.15f;
                    Vector2 p = Vector2.Lerp(start, end, Mathf.Clamp01(t));
                    _token.anchorMin = _token.anchorMax = p;
                    yield return null;
                }
            }
        }

        /// <summary>둘레 인덱스 → 정규화 앵커 좌표.</summary>
        private static Vector2 CellAnchor(int i)
        {
            int x, y;
            if (i < 6) { x = i; y = 0; }              // 하단: 좌→우
            else if (i < 12) { x = 6; y = i - 6; }    // 우측: 하→상
            else if (i < 18) { x = 18 - i; y = 6; }   // 상단: 우→좌
            else { x = 0; y = 24 - i; }               // 좌측: 상→하
            return new Vector2((x + 0.5f) / Side, (y + 0.5f) / Side);
        }

        private static Color TileColor(Tile tile)
        {
            if (tile.Captured) return new Color(0.25f, 0.45f, 0.85f); // 점령: 파란 깃발
            switch (tile.Type)
            {
                case TileType.Start: return new Color(0.9f, 0.75f, 0.2f);
                case TileType.Battle: return new Color(0.55f, 0.2f, 0.2f);
                case TileType.Elite: return new Color(0.75f, 0.1f, 0.1f);
                case TileType.Treasure: return new Color(0.8f, 0.6f, 0.15f);
                case TileType.Shop: return new Color(0.3f, 0.6f, 0.4f);
                case TileType.Rest: return new Color(0.9f, 0.5f, 0.2f);
                case TileType.Trap: return new Color(0.45f, 0.3f, 0.5f);
                case TileType.Event: return new Color(0.35f, 0.4f, 0.6f);
                default: return Color.gray;
            }
        }

        private static string TileIcon(Tile tile)
        {
            if (tile.Captured) return "⚑";
            switch (tile.Type)
            {
                case TileType.Start: return "◈";
                case TileType.Battle: return "☠";
                case TileType.Elite: return "♛";
                case TileType.Treasure: return "▣";
                case TileType.Shop: return "♎";
                case TileType.Rest: return "♨";
                case TileType.Trap: return "▲";
                case TileType.Event: return "?";
                default: return "";
            }
        }
    }
}

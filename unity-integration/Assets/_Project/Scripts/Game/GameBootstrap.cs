using DiceDungeon.Core.Characters;
using DiceDungeon.Core.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDungeon.Game
{
    /// <summary>
    /// 씬 전체를 코드로 구성한다 — 빈 씬에 이 컴포넌트 하나만 붙이고 Play (SETUP.md).
    /// 프리팹·인스펙터 배선 없음: Phase 1 스캐폴드의 진입점.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private GameManager _game;

        private void Start()
        {
            var canvas = UiFactory.CreateCanvas();
            var root = canvas.transform;
            UiFactory.Panel(root, "Background", Vector2.zero, Vector2.one, new Color(0.09f, 0.08f, 0.12f));

            _game = gameObject.AddComponent<GameManager>();
            BuildRunPanel(root);
            var battleView = gameObject.AddComponent<BattleView>();
            battleView.Build(root); // RunPanel 위에 겹치는 오버레이
            _game.BattleUi = battleView;
            BuildTownPanel(root);
            BuildChoicePanel(root);
            BuildResultPanel(root);

            _game.RunPanel.SetActive(false);
            _game.ChoicePanel.SetActive(false);
            _game.ResultPanel.SetActive(false);
            _game.TownPanel.SetActive(true);
            RefreshTown();
        }

        private void BuildRunPanel(Transform root)
        {
            var panel = UiFactory.Rect(root, "RunPanel", Vector2.zero, Vector2.one);
            _game.RunPanel = panel.gameObject;

            // 상단 HUD (01-게임기획서 7: 층·골드·소울스톤)
            var hud = UiFactory.Panel(panel, "Hud", new Vector2(0, 0.94f), Vector2.one, new Color(0, 0, 0, 0.5f));
            _game.FloorText = UiFactory.Label(hud.transform, "Floor", "1층", 40, Color.white, new Vector2(0.02f, 0), new Vector2(0.25f, 1));
            _game.GoldText = UiFactory.Label(hud.transform, "Gold", "0G", 40, new Color(1f, 0.85f, 0.3f), new Vector2(0.3f, 0), new Vector2(0.6f, 1));
            _game.SoulText = UiFactory.Label(hud.transform, "Soul", "◆0", 40, new Color(0.7f, 0.5f, 1f), new Vector2(0.65f, 0), new Vector2(0.98f, 1));

            // 보드 (화면 중앙 60%)
            var boardArea = UiFactory.Rect(panel, "BoardArea", new Vector2(0.03f, 0.33f), new Vector2(0.97f, 0.93f));
            var board = boardArea.gameObject.AddComponent<BoardView>();
            board.Build(boardArea);
            _game.Board = board;

            // 로그 + HP
            _game.LogText = UiFactory.Label(panel, "Log", "", 30, new Color(0.8f, 0.8f, 0.8f),
                new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.32f), TextAnchor.UpperLeft);
            var (bg, fill) = UiFactory.Bar(panel, "HpBar", new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.2f),
                new Color(0.2f, 0.2f, 0.2f), new Color(0.85f, 0.25f, 0.25f));
            _game.HpFill = fill;
            _game.HpText = UiFactory.Label(bg.transform, "HpText", "", 26, Color.white, Vector2.zero, Vector2.one);

            // 주사위 (엄지 존)
            _game.DiceText = UiFactory.Label(panel, "Dice", "🎲 -", 44, Color.white,
                new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.15f));
            _game.RollButton = UiFactory.ActionButton(panel, "Roll", "주사위 굴리기", 44,
                new Vector2(0.15f, 0.015f), new Vector2(0.85f, 0.08f),
                new Color(0.85f, 0.55f, 0.15f), () => _game.OnRollPressed());
        }

        private void BuildTownPanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "TownPanel", Vector2.zero, Vector2.one, new Color(0.12f, 0.1f, 0.16f));
            _game.TownPanel = panel.gameObject;
            UiFactory.Label(panel.transform, "Title", "다이스 던전 — 마을", 56, Color.white, new Vector2(0, 0.9f), new Vector2(1, 0.98f));
            _game.TownInfoText = UiFactory.Label(panel.transform, "Info", "", 32, new Color(0.85f, 0.85f, 0.9f),
                new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.89f));

            // 캐릭터 선택 (해금 포함)
            for (int i = 0; i < CharacterClass.All.Length; i++)
            {
                var c = CharacterClass.All[i];
                float x0 = 0.03f + i * 0.24f;
                UiFactory.ActionButton(panel.transform, $"Char{i}", c.Name, 34,
                    new Vector2(x0, 0.66f), new Vector2(x0 + 0.22f, 0.75f),
                    new Color(0.25f, 0.3f, 0.45f), () => { _game.SelectOrUnlock(c.Id); });
            }

            // 훈련소 (02-시스템설계 5)
            string[] trainNames = { "체력 훈련", "공격 훈련", "방어 훈련" };
            for (int i = 0; i < 3; i++)
            {
                int stat = i;
                float x0 = 0.05f + i * 0.31f;
                UiFactory.ActionButton(panel.transform, $"Train{i}", trainNames[i], 30,
                    new Vector2(x0, 0.52f), new Vector2(x0 + 0.28f, 0.62f),
                    new Color(0.3f, 0.45f, 0.3f), () => _game.Train(stat));
            }

            UiFactory.ActionButton(panel.transform, "Relic", "유물 강화 (가장 싼 것)", 34,
                new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.48f),
                new Color(0.45f, 0.3f, 0.5f), () => _game.BuyCheapestRelic());

            UiFactory.ActionButton(panel.transform, "Start", "던전 입장", 48,
                new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.2f),
                new Color(0.8f, 0.35f, 0.2f), () => _game.StartRun());

            _game.RefreshTownUi = RefreshTown;
        }

        private void RefreshTown()
        {
            var p = _game.Profile;
            int relicTotal = 0;
            for (int i = 0; i < p.RelicLevels.Length; i++) relicTotal += p.RelicLevels[i];
            _game.TownInfoText.text =
                $"◆ 소울스톤 {p.Soulstones}   |   최고 {p.BestFloor}층   |   {p.TotalRuns}런\n" +
                $"선택: {CharacterClass.Get(_game.Selected).Name}   훈련 {p.TrainHpLevel}/{p.TrainAtkLevel}/{p.TrainDefLevel}" +
                $" (비용 {PlayerProfile.TrainCost(p.TrainHpLevel)}/{PlayerProfile.TrainCost(p.TrainAtkLevel)}/{PlayerProfile.TrainCost(p.TrainDefLevel)})" +
                $"   유물 {relicTotal}Lv";
        }

        private void BuildChoicePanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "ChoicePanel", new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f),
                new Color(0.05f, 0.05f, 0.1f, 0.97f));
            _game.ChoicePanel = panel.gameObject;
            UiFactory.Label(panel.transform, "Title", "층 클리어!", 48, Color.white, new Vector2(0, 0.65f), new Vector2(1, 0.95f));
            UiFactory.ActionButton(panel.transform, "Descend", "더 깊이 (보상↑)", 36,
                new Vector2(0.06f, 0.1f), new Vector2(0.48f, 0.5f),
                new Color(0.7f, 0.3f, 0.2f), () => _game.OnDescend());
            UiFactory.ActionButton(panel.transform, "Retreat", "귀환 (100% 회수)", 36,
                new Vector2(0.52f, 0.1f), new Vector2(0.94f, 0.5f),
                new Color(0.25f, 0.4f, 0.55f), () => _game.OnRetreat());
        }

        private void BuildResultPanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "ResultPanel", Vector2.zero, Vector2.one, new Color(0.05f, 0.04f, 0.08f, 0.98f));
            _game.ResultPanel = panel.gameObject;
            _game.ResultText = UiFactory.Label(panel.transform, "Result", "", 44, Color.white,
                new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.85f));
            UiFactory.ActionButton(panel.transform, "Town", "마을로", 44,
                new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.28f),
                new Color(0.3f, 0.5f, 0.35f), () => _game.BackToTown());
        }
    }
}

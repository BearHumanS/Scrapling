using System.Collections;
using System.Collections.Generic;
using DiceDungeon.Core.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDungeon.Game
{
    /// <summary>
    /// 수동 전투 오버레이 (05-백로그 Week 3).
    /// Core의 InteractiveBattle이 판정하고, 여기는 입력 대기와 이벤트 재생만 한다.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        private GameObject _panel;
        private Text _title, _log, _playerLabel, _shieldLabel;
        private Image _playerFill;
        private readonly List<GameObject> _monsterSlots = new List<GameObject>();
        private readonly List<Image> _monsterFills = new List<Image>();
        private readonly List<Text> _monsterLabels = new List<Text>();
        private readonly List<Text> _monsterStatus = new List<Text>();
        private Button _attackBtn, _defendBtn, _potionBtn;
        private readonly List<Button> _skillBtns = new List<Button>();
        private readonly List<Text> _skillLabels = new List<Text>();

        private PlayerActionType? _pending;
        private int _pendingSkill;

        private const int MaxMonsters = 2;

        public void Build(Transform root)
        {
            var panel = UiFactory.Panel(root, "BattlePanel", new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f),
                new Color(0.06f, 0.05f, 0.1f, 0.98f));
            _panel = panel.gameObject;

            _title = UiFactory.Label(panel.transform, "Title", "전투!", 46, Color.white,
                new Vector2(0, 0.92f), new Vector2(1, 0.99f));

            // 적 슬롯 (우측 상단 영역, 01-게임기획서 7: 아군 좌 / 적 우)
            for (int i = 0; i < MaxMonsters; i++)
            {
                float y0 = 0.78f - i * 0.14f;
                var slot = UiFactory.Panel(panel.transform, $"Monster{i}",
                    new Vector2(0.4f, y0), new Vector2(0.96f, y0 + 0.12f), new Color(0.15f, 0.1f, 0.12f));
                _monsterLabels.Add(UiFactory.Label(slot.transform, "Name", "", 30, Color.white,
                    new Vector2(0.03f, 0.5f), new Vector2(0.97f, 1f), TextAnchor.MiddleLeft));
                var (bg, fill) = UiFactory.Bar(slot.transform, "Hp",
                    new Vector2(0.03f, 0.15f), new Vector2(0.7f, 0.42f),
                    new Color(0.2f, 0.2f, 0.2f), new Color(0.8f, 0.25f, 0.25f));
                _monsterFills.Add(fill);
                _monsterStatus.Add(UiFactory.Label(slot.transform, "Status", "", 26, Color.white,
                    new Vector2(0.72f, 0.1f), new Vector2(0.97f, 0.45f), TextAnchor.MiddleRight));
                _monsterSlots.Add(slot.gameObject);
            }

            // 플레이어 (좌측)
            var me = UiFactory.Panel(panel.transform, "PlayerSide",
                new Vector2(0.04f, 0.5f), new Vector2(0.36f, 0.9f), new Color(0.1f, 0.12f, 0.18f));
            _playerLabel = UiFactory.Label(me.transform, "Name", "", 30, Color.white,
                new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.95f));
            var (pbg, pfill) = UiFactory.Bar(me.transform, "Hp",
                new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.52f),
                new Color(0.2f, 0.2f, 0.2f), new Color(0.3f, 0.75f, 0.35f));
            _playerFill = pfill;
            _shieldLabel = UiFactory.Label(me.transform, "Shield", "", 26, new Color(0.6f, 0.8f, 1f),
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.35f));

            _log = UiFactory.Label(panel.transform, "Log", "", 28, new Color(0.9f, 0.85f, 0.7f),
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.48f));

            // 행동 버튼 (엄지 존): 기본 3 + 스킬 3
            _attackBtn = UiFactory.ActionButton(panel.transform, "Attack", "공격", 36,
                new Vector2(0.04f, 0.24f), new Vector2(0.34f, 0.33f),
                new Color(0.7f, 0.3f, 0.2f), () => _pending = PlayerActionType.Attack);
            _defendBtn = UiFactory.ActionButton(panel.transform, "Defend", "방어", 36,
                new Vector2(0.36f, 0.24f), new Vector2(0.64f, 0.33f),
                new Color(0.3f, 0.4f, 0.6f), () => _pending = PlayerActionType.Defend);
            _potionBtn = UiFactory.ActionButton(panel.transform, "Potion", "포션", 36,
                new Vector2(0.66f, 0.24f), new Vector2(0.96f, 0.33f),
                new Color(0.35f, 0.55f, 0.3f), () => _pending = PlayerActionType.Potion);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                float x0 = 0.04f + i * 0.32f;
                var btn = UiFactory.ActionButton(panel.transform, $"Skill{i}", "-", 28,
                    new Vector2(x0, 0.12f), new Vector2(x0 + 0.3f, 0.22f),
                    new Color(0.45f, 0.3f, 0.55f),
                    () => { _pending = PlayerActionType.Skill; _pendingSkill = idx; });
                _skillBtns.Add(btn);
                _skillLabels.Add(btn.GetComponentInChildren<Text>());
            }

            _panel.SetActive(false);
        }

        public IEnumerator RunBattle(InteractiveBattle battle, bool isBoss, string intro)
        {
            _panel.SetActive(true);
            _title.text = isBoss ? "⚔ 보스전 ⚔" : "전투!";
            _log.text = intro;
            Refresh(battle);
            yield return new WaitForSeconds(0.5f);

            while (!battle.Over)
            {
                SetButtons(battle, enabled: true);
                _pending = null;
                yield return new WaitUntil(() => _pending.HasValue);
                SetButtons(battle, enabled: false);

                var events = battle.DoRound(_pending.Value, _pendingSkill);
                foreach (var ev in events)
                {
                    _log.text = Describe(ev);
                    Refresh(battle);
                    yield return new WaitForSeconds(0.35f);
                }
            }

            _log.text = battle.PlayerWon ? "승리!" : "쓰러졌다...";
            yield return new WaitForSeconds(0.9f);
            _panel.SetActive(false);
        }

        private void SetButtons(InteractiveBattle battle, bool enabled)
        {
            _attackBtn.interactable = enabled;
            _defendBtn.interactable = enabled;
            _potionBtn.interactable = enabled && battle.Potions > 0;
            for (int i = 0; i < _skillBtns.Count; i++)
            {
                bool has = i < battle.Skills.Count;
                _skillBtns[i].gameObject.SetActive(has);
                if (!has) continue;
                var s = battle.Skills[i];
                _skillLabels[i].text = s.Ready ? s.Name : $"{s.Name} ({s.CurrentCooldown})";
                _skillBtns[i].interactable = enabled && s.Ready;
            }
        }

        private void Refresh(InteractiveBattle battle)
        {
            _playerLabel.text = $"{battle.Player.Name}\n{battle.Player.Hp}/{battle.Player.MaxHp}";
            UiFactory.SetRatio(_playerFill, (float)battle.Player.HpRatio);
            _shieldLabel.text = battle.Shield > 0 ? $"실드 {battle.Shield}" : $"포션 {battle.Potions}";

            for (int i = 0; i < MaxMonsters; i++)
            {
                bool has = i < battle.Monsters.Count;
                _monsterSlots[i].SetActive(has);
                if (!has) continue;
                var m = battle.Monsters[i];
                _monsterLabels[i].text = m.IsAlive ? $"{m.Name}  {m.Hp}/{m.MaxHp}" : $"{m.Name}  ✝";
                UiFactory.SetRatio(_monsterFills[i], (float)m.HpRatio);
                _monsterStatus[i].text = StatusChips(m);
            }
        }

        private static string StatusChips(Unit u)
        {
            string chips = "";
            if (u.Statuses.Has(StatusType.Burn)) chips += "🔥";
            if (u.Statuses.Has(StatusType.Poison)) chips += "☠";
            if (u.Statuses.Has(StatusType.Freeze)) chips += "❄";
            if (u.Statuses.Has(StatusType.Stun)) chips += "💫";
            return chips;
        }

        private static string Describe(BattleEvent ev)
        {
            switch (ev.Type)
            {
                case BattleEventType.PlayerHit: return $"{ev.Target?.Name}에게 {ev.Amount} 피해" + (ev.Crit ? " (크리티컬!)" : "");
                case BattleEventType.MonsterHit: return $"{ev.Actor?.Name}의 공격! -{ev.Amount}";
                case BattleEventType.Miss: return "회피했다!";
                case BattleEventType.ShieldAbsorb: return $"실드가 {ev.Amount} 흡수";
                case BattleEventType.Heal: return $"회복 +{ev.Amount}";
                case BattleEventType.StatusApplied: return $"{ev.Target?.Name} 상태이상!";
                case BattleEventType.StatusTick: return $"{ev.Actor?.Name} 지속 피해 {ev.Amount}";
                case BattleEventType.Stunned: return $"{ev.Actor?.Name}은(는) 기절해 움직이지 못한다";
                case BattleEventType.MonsterDied: return $"{ev.Target?.Name} 처치!";
                case BattleEventType.Revived: return "빛이 감싸며 부활했다!";
                case BattleEventType.Victory: return "승리!";
                case BattleEventType.PlayerDied:
                case BattleEventType.Defeat: return "쓰러졌다...";
                default: return "";
            }
        }
    }
}

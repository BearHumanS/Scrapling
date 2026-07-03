using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Characters;
using DiceDungeon.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDungeon.Game
{
    /// <summary>
    /// 심화 시스템 UI (07-심화시스템 7): 레벨업 스탯 배분 / 전직 2택1 / 스킬트리.
    /// 판정은 전부 CharacterSheet(Core)가 하고, 여기는 버튼과 대기만 담당한다.
    /// </summary>
    public sealed class ProgressionPanels : MonoBehaviour
    {
        private GameObject _levelPanel, _jobPanel, _skillPanel;
        private Text _levelInfo, _skillInfo;
        private readonly List<Text> _statLabels = new List<Text>();
        private Button _jobA, _jobB;
        private Text _jobALabel, _jobBLabel;
        private RectTransform _skillList;

        private bool _levelDone, _skillClosed;
        private JobPath _jobChoice;

        private static readonly (StatId id, string name)[] StatNames =
        {
            (StatId.Str, "힘"), (StatId.Agi, "민첩"), (StatId.Vit, "체력"),
            (StatId.Int, "지능"), (StatId.Dex, "재주"), (StatId.Luk, "운"),
        };

        private System.Func<CharacterSheet> _sheet;

        public void Build(Transform root, System.Func<CharacterSheet> sheetGetter)
        {
            _sheet = sheetGetter;
            BuildLevelPanel(root);
            BuildJobPanel(root);
            BuildSkillPanel(root);
        }

        // ---------- 레벨업: 스탯 포인트 배분 ----------

        private void BuildLevelPanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "LevelUpPanel", new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.78f),
                new Color(0.07f, 0.06f, 0.12f, 0.98f));
            _levelPanel = panel.gameObject;
            _levelInfo = UiFactory.Label(panel.transform, "Info", "", 38, new Color(1f, 0.85f, 0.4f),
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));

            for (int i = 0; i < StatNames.Length; i++)
            {
                var (id, name) = StatNames[i];
                int row = i / 2, col = i % 2;
                float x0 = 0.06f + col * 0.47f, y0 = 0.62f - row * 0.19f;
                var btn = UiFactory.ActionButton(panel.transform, $"Stat{i}", "", 30,
                    new Vector2(x0, y0), new Vector2(x0 + 0.41f, y0 + 0.15f),
                    new Color(0.25f, 0.3f, 0.45f), () =>
                    {
                        var s = _sheet();
                        s.SpendStatPoint(id);
                        RefreshLevelPanel();
                        if (s.UnspentStatPoints <= 0) _levelDone = true;
                    });
                _statLabels.Add(btn.GetComponentInChildren<Text>());
            }
            _levelPanel.SetActive(false);
        }

        private void RefreshLevelPanel()
        {
            var s = _sheet();
            _levelInfo.text = $"레벨 업!  Lv{s.Level}   남은 포인트 {s.UnspentStatPoints}";
            for (int i = 0; i < StatNames.Length; i++)
            {
                var (id, name) = StatNames[i];
                _statLabels[i].text = $"{name} {s.Stats.Get(id)}  (+1)";
            }
        }

        /// <summary>스탯 포인트를 전부 배분할 때까지 대기.</summary>
        public IEnumerator RunLevelUp()
        {
            var s = _sheet();
            if (s.UnspentStatPoints <= 0) yield break;
            _levelDone = false;
            RefreshLevelPanel();
            _levelPanel.SetActive(true);
            yield return new WaitUntil(() => _levelDone);
            yield return new WaitForSeconds(0.25f);
            _levelPanel.SetActive(false);
        }

        // ---------- 전직: 2택1 ----------

        private void BuildJobPanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "JobPanel", new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.72f),
                new Color(0.1f, 0.05f, 0.14f, 0.98f));
            _jobPanel = panel.gameObject;
            UiFactory.Label(panel.transform, "Title", "전직의 순간 — 길을 선택하라", 38, new Color(1f, 0.85f, 0.4f),
                new Vector2(0, 0.8f), new Vector2(1, 0.97f));
            _jobA = UiFactory.ActionButton(panel.transform, "PathA", "", 30,
                new Vector2(0.05f, 0.08f), new Vector2(0.48f, 0.75f),
                new Color(0.5f, 0.25f, 0.2f), () => _jobChoice = _pathA);
            _jobB = UiFactory.ActionButton(panel.transform, "PathB", "", 30,
                new Vector2(0.52f, 0.08f), new Vector2(0.95f, 0.75f),
                new Color(0.2f, 0.3f, 0.5f), () => _jobChoice = _pathB);
            _jobALabel = _jobA.GetComponentInChildren<Text>();
            _jobBLabel = _jobB.GetComponentInChildren<Text>();
            _jobPanel.SetActive(false);
        }

        private JobPath _pathA, _pathB;

        public IEnumerator RunJobChange()
        {
            var s = _sheet();
            if (!s.CanJobChange) yield break;
            (_pathA, _pathB) = JobCatalog.PathsFor(s.Job);
            _jobALabel.text = PathSummary(_pathA);
            _jobBLabel.text = PathSummary(_pathB);
            _jobChoice = null;
            _jobPanel.SetActive(true);
            yield return new WaitUntil(() => _jobChoice != null);
            s.JobChange(_jobChoice);
            _jobPanel.SetActive(false);
        }

        private static string PathSummary(JobPath p)
        {
            var skills = string.Join("\n", p.Tree.Select(t => "· " + t.Name));
            return $"{p.Name}\n\n{skills}";
        }

        // ---------- 스킬트리 ----------

        private void BuildSkillPanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "SkillPanel", new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f),
                new Color(0.06f, 0.07f, 0.12f, 0.98f));
            _skillPanel = panel.gameObject;
            _skillInfo = UiFactory.Label(panel.transform, "Info", "", 34, Color.white,
                new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f));
            _skillList = UiFactory.Rect(panel.transform, "List", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.91f));
            UiFactory.ActionButton(panel.transform, "Close", "닫기", 34,
                new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.1f),
                new Color(0.35f, 0.35f, 0.4f), () => _skillClosed = true);
            _skillPanel.SetActive(false);
        }

        public IEnumerator RunSkillTree()
        {
            _skillClosed = false;
            RefreshSkillPanel();
            _skillPanel.SetActive(true);
            yield return new WaitUntil(() => _skillClosed);
            _skillPanel.SetActive(false);
        }

        private void RefreshSkillPanel()
        {
            var s = _sheet();
            string mode = s.EquippedIds.Count > 0 ? $"수동 {s.EquippedIds.Count}/{JobCatalog.SkillSlots}" : "자동(계수순)";
            _skillInfo.text = $"스킬트리 — 포인트 {s.Book.Points}  |  장착: {mode}";

            foreach (Transform child in _skillList) Destroy(child.gameObject);

            var pool = s.SkillPool.ToList();
            for (int i = 0; i < pool.Count; i++)
            {
                var def = pool[i];
                int lv = s.Book.LevelOf(def.Id);
                float rowH = 1f / Mathf.Max(10, pool.Count);
                float y1 = 1f - i * rowH, y0 = y1 - rowH * 0.92f;

                var row = UiFactory.Panel(_skillList, $"Row{i}", new Vector2(0, y0), new Vector2(1, y1),
                    new Color(0.12f, 0.13f, 0.2f));
                string desc = def.IsPassive ? "패시브" : $"쿨{def.Cooldown}" + (def.Aoe ? "·광역" : "");
                string prereq = def.PrereqId.Length > 0 ? $"  [선행: {NameOf(pool, def.PrereqId)} Lv{def.PrereqLevel}]" : "";
                bool equipped = s.EquippedIds.Contains(def.Id);
                UiFactory.Label(row.transform, "Name",
                    $"{(equipped ? "⭐" : "")}{def.Name}  Lv{lv}/{def.MaxLevel}  ({desc}){prereq}",
                    26, lv > 0 ? Color.white : new Color(0.65f, 0.65f, 0.7f),
                    new Vector2(0.02f, 0), new Vector2(0.62f, 1), TextAnchor.MiddleLeft);

                var learnBtn = UiFactory.ActionButton(row.transform, "Learn", "배우기", 24,
                    new Vector2(0.64f, 0.1f), new Vector2(0.8f, 0.9f),
                    new Color(0.3f, 0.5f, 0.3f), () =>
                    {
                        s.Book.Learn(def);
                        RefreshSkillPanel();
                        OnSkillLearned?.Invoke();
                    });
                learnBtn.interactable = s.Book.CanLearn(def);

                // 수동 장착 토글 (감사 B1: 저계수 정체성 스킬도 빌드로 선택 가능하게)
                if (!def.IsPassive)
                {
                    var equipBtn = UiFactory.ActionButton(row.transform, "Equip",
                        equipped ? "해제" : "장착", 24,
                        new Vector2(0.82f, 0.1f), new Vector2(0.98f, 0.9f),
                        equipped ? new Color(0.55f, 0.4f, 0.2f) : new Color(0.3f, 0.4f, 0.55f), () =>
                        {
                            s.ToggleEquip(def.Id);
                            RefreshSkillPanel();
                            OnSkillLearned?.Invoke();
                        });
                    equipBtn.interactable = lv > 0;
                }
            }
        }

        public System.Action OnSkillLearned;

        private static string NameOf(List<SkillDef> pool, string id)
            => pool.FirstOrDefault(d => d.Id == id)?.Name ?? id;
    }
}

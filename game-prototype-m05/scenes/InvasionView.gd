extends Control
class_name InvasionView
## 침공 화면(코드 구성). 영웅이 AI 보드 장애물(트랩→수비→코어)을 순차 돌파.

signal finished(success: bool)

var _hero: HeroState
var _obstacles: Array = []   # [{kind, ...}]
var _idx: int = 0
var _log: Array = []

var title_label: Label
var hero_label: Label
var progress_label: Label
var log_label: Label
var advance_btn: Button
var retreat_btn: Button

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build()

func _build() -> void:
	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 6)
	add_child(root)

	title_label = Label.new()
	title_label.text = "⚔ 침공 — AI 보드 침투"
	root.add_child(title_label)
	hero_label = Label.new()
	root.add_child(hero_label)
	progress_label = Label.new()
	root.add_child(progress_label)

	var btns := HBoxContainer.new()
	root.add_child(btns)
	advance_btn = Button.new()
	advance_btn.text = "▶ 진격"
	advance_btn.pressed.connect(_on_advance)
	btns.add_child(advance_btn)
	retreat_btn = Button.new()
	retreat_btn.text = "후퇴"
	retreat_btn.pressed.connect(_on_retreat)
	btns.add_child(retreat_btn)

	log_label = Label.new()
	log_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(log_label)

func start(hero: HeroState, obstacles: Array) -> void:
	_hero = hero
	_obstacles = obstacles
	_idx = 0
	_log.clear()
	advance_btn.disabled = false
	retreat_btn.disabled = false
	_clog("침공 개시! 장애물 %d개 (트랩·수비·코어)" % obstacles.size())
	_refresh()

func _clog(t: String) -> void:
	_log.append(t)
	if _log.size() > 9:
		_log = _log.slice(_log.size() - 9)

func _refresh() -> void:
	hero_label.text = "영웅 HP %d/%d | ATK %d | DEF %d" % [
		max(_hero.hp, 0), _hero.max_hp, _hero.atk, _hero.def]
	var nxt := "-"
	if _idx < _obstacles.size():
		nxt = _describe(_obstacles[_idx])
	progress_label.text = "진행 %d/%d | 다음: %s" % [_idx, _obstacles.size(), nxt]
	log_label.text = "\n".join(_log)

func _describe(ob: Dictionary) -> String:
	match ob.get("kind", ""):
		"trap":
			return "트랩(피해 %d)" % int(ob.get("dmg", 0))
		"defender":
			return "수비병(HP %d, ATK %d)" % [int(ob.get("hp", 0)), int(ob.get("atk", 0))]
		"core":
			return "코어(HP %d, 수호 ATK %d)" % [int(ob.get("hp", 0)), int(ob.get("atk", 0))]
	return "?"

func _on_advance() -> void:
	if _idx >= _obstacles.size():
		return
	var ob: Dictionary = _obstacles[_idx]
	match ob.get("kind", ""):
		"trap":
			var d: int = int(ob.get("dmg", 0))
			_hero.hp -= d
			_clog("트랩 발동! -%d HP" % d)
		"defender":
			_fight(ob, "수비병")
		"core":
			_fight(ob, "코어")
	if _hero.hp <= 0:
		_hero.hp = 0
		_clog("영웅 쓰러짐... 침공 실패.")
		_refresh()
		_end(false)
		return
	_idx += 1
	if _idx >= _obstacles.size():
		_clog("코어 파괴! 침공 성공! 🎉")
		_refresh()
		_end(true)
		return
	_refresh()

func _fight(ob: Dictionary, label: String) -> void:
	var ehp: int = int(ob.get("hp", 0))
	var eatk: int = int(ob.get("atk", 0))
	var rounds: int = 0
	while ehp > 0 and _hero.hp > 0 and rounds < 50:
		rounds += 1
		ehp -= Balance.damage(_hero.atk, 1.0, 5, false)
		if ehp <= 0:
			break
		_hero.hp -= Balance.damage(eatk, 1.0, _hero.def, false)
	if _hero.hp > 0 and ehp <= 0:
		_clog("%s 격파! (남은 HP %d)" % [label, _hero.hp])
	elif _hero.hp <= 0:
		_clog("%s와 교전 중 쓰러짐..." % label)

func _on_retreat() -> void:
	_clog("후퇴 선택. 침공 취소.")
	_end(false)

func _end(success: bool) -> void:
	advance_btn.disabled = true
	retreat_btn.disabled = true
	finished.emit(success)

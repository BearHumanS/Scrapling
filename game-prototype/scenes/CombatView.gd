extends Control
class_name CombatView
## 전투 화면(코드로 UI 구성). rpg-combat.md + ux-screens.md §3.

signal finished(victory: bool)

var _player: PlayerState
var _enemy: EnemyData
var _enemy_hp: int = 0
var _defending: bool = false
var _log: Array = []

var enemy_label: Label
var player_label: Label
var log_label: Label
var atk_btn: Button
var skill_btn: Button
var def_btn: Button

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build()

func _build() -> void:
	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 8)
	add_child(root)

	var title := Label.new()
	title.text = "⚔ 전투!"
	root.add_child(title)
	enemy_label = Label.new()
	root.add_child(enemy_label)
	player_label = Label.new()
	root.add_child(player_label)

	var btns := HBoxContainer.new()
	root.add_child(btns)
	atk_btn = Button.new()
	atk_btn.text = "공격"
	atk_btn.pressed.connect(_on_attack)
	btns.add_child(atk_btn)
	skill_btn = Button.new()
	skill_btn.text = "강타 (EN2)"
	skill_btn.pressed.connect(_on_skill)
	btns.add_child(skill_btn)
	def_btn = Button.new()
	def_btn.text = "방어 (+EN)"
	def_btn.pressed.connect(_on_defend)
	btns.add_child(def_btn)

	log_label = Label.new()
	log_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(log_label)

func start(player: PlayerState, enemy: EnemyData) -> void:
	_player = player
	_enemy = enemy
	_enemy_hp = enemy.max_hp
	_player.en = _player.max_en
	_defending = false
	_log.clear()
	_set_enabled(true)
	_clog("%s (HP %d) 등장!" % [enemy.display_name, enemy.max_hp])
	_refresh()

func _clog(t: String) -> void:
	_log.append(t)
	if _log.size() > 8:
		_log = _log.slice(_log.size() - 8)

func _refresh() -> void:
	enemy_label.text = "%s   HP %d/%d" % [_enemy.display_name, max(_enemy_hp, 0), _enemy.max_hp]
	player_label.text = "%s   HP %d/%d   EN %d/%d" % [
		_player.name, max(_player.hp, 0), _player.max_hp, _player.en, _player.max_en]
	log_label.text = "\n".join(_log)

func _player_hit(coeff: float) -> void:
	var dmg: int = Balance.damage(_player.atk, coeff, _enemy.def, false)
	if RNG.chance(0.1):
		dmg = int(round(dmg * 1.5))
		_clog("치명타!")
	_enemy_hp -= dmg
	_clog("%s 공격 → %d 피해" % [_player.name, dmg])

func _on_attack() -> void:
	_defending = false
	_player_hit(1.0)
	_player.en = min(_player.max_en, _player.en + 1)
	_after_player_action()

func _on_skill() -> void:
	if _player.en < 2:
		_clog("EN 부족!")
		_refresh()
		return
	_defending = false
	_player.en -= 2
	_player_hit(1.5)
	_after_player_action()

func _on_defend() -> void:
	_defending = true
	_player.en = min(_player.max_en, _player.en + 2)
	_clog("%s 방어 태세 (다음 피해 감소)" % _player.name)
	_after_player_action()

func _after_player_action() -> void:
	if _enemy_hp <= 0:
		_clog("%s 처치! 승리!" % _enemy.display_name)
		_refresh()
		_end(true)
		return
	var edmg: int = Balance.damage(_enemy.atk, 1.0, _player.def, _defending)
	_player.hp -= edmg
	_clog("%s 반격 → %d 피해" % [_enemy.display_name, edmg])
	if _player.hp <= 0:
		_player.hp = 0
		_clog("패배...")
		_refresh()
		_end(false)
		return
	_refresh()

func _end(victory: bool) -> void:
	_set_enabled(false)
	finished.emit(victory)

func _set_enabled(v: bool) -> void:
	atk_btn.disabled = not v
	skill_btn.disabled = not v
	def_btn.disabled = not v

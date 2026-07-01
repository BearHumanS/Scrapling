extends Control
class_name BoardView
## 내 보드 파밍 화면(코드 구성).

signal roll_pressed()
signal invade_pressed()

var info_label: Label
var hero_label: Label
var ai_label: Label
var hint_label: Label
var board_label: RichTextLabel
var dice_label: Label
var log_label: Label
var roll_btn: Button
var invade_btn: Button

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build()
	EventBus.log_message.connect(_on_log)

func _build() -> void:
	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 6)
	add_child(root)

	var title := Label.new()
	title.text = "🎲 다이스마블 M0.5 — 내 보드 파밍"
	root.add_child(title)

	info_label = Label.new()
	root.add_child(info_label)
	hero_label = Label.new()
	root.add_child(hero_label)
	ai_label = Label.new()
	root.add_child(ai_label)
	hint_label = Label.new()
	root.add_child(hint_label)

	board_label = RichTextLabel.new()
	board_label.bbcode_enabled = true
	board_label.fit_content = true
	board_label.custom_minimum_size = Vector2(0, 110)
	root.add_child(board_label)

	dice_label = Label.new()
	dice_label.text = "🎲 -"
	root.add_child(dice_label)

	var btns := HBoxContainer.new()
	root.add_child(btns)
	roll_btn = Button.new()
	roll_btn.text = "🎲 주사위 굴리기 (파밍)"
	roll_btn.pressed.connect(func(): roll_pressed.emit())
	btns.add_child(roll_btn)
	invade_btn = Button.new()
	invade_btn.text = "⚔ AI 보드 침공!"
	invade_btn.pressed.connect(func(): invade_pressed.emit())
	btns.add_child(invade_btn)

	var log_title := Label.new()
	log_title.text = "─ 로그 ─"
	root.add_child(log_title)
	log_label = Label.new()
	log_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(log_label)

func _on_log(_t: String) -> void:
	log_label.text = "\n".join(GameState.log_lines)

func set_buttons(roll: bool, invade: bool) -> void:
	roll_btn.disabled = not roll
	invade_btn.disabled = not invade

func refresh() -> void:
	info_label.text = "턴 %d | 라운드 %d | 티어 %d | 누적파밍 %d" % [
		GameState.turn_count, GameState.round_count, GameState.tier, GameState.total_harvest]
	var h: HeroState = GameState.hero
	hero_label.text = "영웅  HP %d/%d | ATK %d | DEF %d | 골드 %d" % [
		h.hp, h.max_hp, h.atk, h.def, h.gold]
	ai_label.text = "AI 보드  라운드 %d | 티어 %d | 코어HP %d | 트랩 %d | 수비 %d(atk %d)" % [
		GameState.ai_round, GameState.ai_tier, GameState.ai_core_hp(),
		GameState.ai_trap_count(), GameState.ai_defender_hp(), GameState.ai_defender_atk()]
	if GameState.invasion_available:
		hint_label.text = "💡 지금 침공: 코어HP %d vs 내 ATK %d — 빠를수록 AI 얇음(초반 취약 노려라)" % [
			GameState.ai_core_hp(), h.atk]
	else:
		hint_label.text = "💡 라운드 3부터 침공 가능(완주=라운드). 파밍으로 전투력을 키우세요."
	board_label.text = _board_string()
	log_label.text = "\n".join(GameState.log_lines)

func _board_string() -> String:
	var parts: Array = []
	for i in GameState.board.size():
		var t: FarmTile = GameState.board[i]
		var here := ""
		if GameState.pos == i:
			here = "◀"
		if t.type == FarmTile.Type.START:
			parts.append("[출발%s]" % here)
		else:
			parts.append("[%s d%d s%d%s]" % [t.name, t.dev, int(t.store), here])
	return " ".join(parts)

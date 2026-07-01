extends Control
class_name BoardView
## 파밍 화면 + AI 보드 미리보기(두 보드 동시 표시). 코드 구성 UI.

signal roll_pressed()
signal train_pressed()
signal sabotage_pressed()
signal invade_pressed()

var info_label: Label
var hero_label: Label
var hint_label: Label
var my_board_label: RichTextLabel
var ai_title_label: Label
var ai_board_label: RichTextLabel
var dice_label: Label
var log_label: Label
var roll_btn: Button
var train_btn: Button
var sabotage_btn: Button
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
	title.text = "🎲 다이스마블 M0.5 — 파밍 & 침공"
	root.add_child(title)

	info_label = Label.new()
	root.add_child(info_label)
	hero_label = Label.new()
	root.add_child(hero_label)
	hint_label = Label.new()
	hint_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(hint_label)

	var my_title := Label.new()
	my_title.text = "── 내 보드 (파밍) ──"
	root.add_child(my_title)
	my_board_label = RichTextLabel.new()
	my_board_label.bbcode_enabled = true
	my_board_label.fit_content = true
	my_board_label.custom_minimum_size = Vector2(0, 90)
	root.add_child(my_board_label)

	ai_title_label = Label.new()
	ai_title_label.text = "── AI 보드 (침공 경로 미리보기) ──"
	root.add_child(ai_title_label)
	ai_board_label = RichTextLabel.new()
	ai_board_label.bbcode_enabled = true
	ai_board_label.fit_content = true
	ai_board_label.custom_minimum_size = Vector2(0, 40)
	root.add_child(ai_board_label)

	dice_label = Label.new()
	dice_label.text = "🎲 -"
	root.add_child(dice_label)

	var btns := HBoxContainer.new()
	root.add_child(btns)
	roll_btn = Button.new()
	roll_btn.text = "🎲 주사위(파밍)"
	roll_btn.pressed.connect(func(): roll_pressed.emit())
	btns.add_child(roll_btn)
	train_btn = Button.new()
	train_btn.text = "🛡 훈련(%dG)" % GameState.TRAIN_COST
	train_btn.pressed.connect(func(): train_pressed.emit())
	btns.add_child(train_btn)
	sabotage_btn = Button.new()
	sabotage_btn.text = "💣 사보타주(%dG)" % GameState.SABO_COST
	sabotage_btn.pressed.connect(func(): sabotage_pressed.emit())
	btns.add_child(sabotage_btn)
	invade_btn = Button.new()
	invade_btn.text = "⚔ AI 침공!"
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

func set_buttons(roll: bool, train: bool, sabo: bool, invade: bool) -> void:
	roll_btn.disabled = not roll
	train_btn.disabled = not train
	sabotage_btn.disabled = not sabo
	invade_btn.disabled = not invade

func refresh() -> void:
	info_label.text = "턴 %d | 라운드 %d | 티어 %d | 누적파밍 %d" % [
		GameState.turn_count, GameState.round_count, GameState.tier, GameState.total_harvest]
	var h: HeroState = GameState.hero
	hero_label.text = "영웅  HP %d/%d | ATK %d | DEF %d | 💰골드 %d" % [
		h.hp, h.max_hp, h.atk, h.def, h.gold]
	if GameState.invasion_available:
		hint_label.text = "💡 침공 가능! 코어HP %d vs 내 ATK %d — 훈련(자기강화)/사보타주(코어 -%d) 중 골드 배분이 관건" % [
			GameState.ai_core_hp(), h.atk, GameState.SABO_DMG]
	else:
		hint_label.text = "💡 라운드 3부터 침공 가능. 골드로 훈련(강해지기) 또는 사보타주(AI 코어 약화) 선택. 속도는 순수 운."
	my_board_label.text = _my_board_string()
	ai_board_label.text = _ai_preview_string()
	log_label.text = "\n".join(GameState.log_lines)

func _my_board_string() -> String:
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

func _ai_preview_string() -> String:
	var parts: Array = ["[입구]"]
	for i in GameState.ai_trap_count():
		parts.append("[트랩 %d]" % GameState.ai_trap_dmg())
	parts.append("[수비 HP%d/ATK%d]" % [GameState.ai_defender_hp(), GameState.ai_defender_atk()])
	var core_txt := "[코어 HP%d]" % GameState.ai_core_hp()
	if GameState.ai_core_damage > 0:
		core_txt += "(사보 -%d)" % GameState.ai_core_damage
	parts.append(core_txt)
	return " → ".join(parts)

extends Control
class_name BoardView
## 보드 화면(코드로 UI 구성). ux-screens.md §2의 축소판.

signal roll_pressed()
signal buy_pressed()
signal skip_pressed()

var info_label: Label
var players_label: Label
var board_label: RichTextLabel
var dice_label: Label
var log_label: Label
var roll_btn: Button
var buy_btn: Button
var skip_btn: Button

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build()
	EventBus.log_message.connect(_on_log)

func _build() -> void:
	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 8)
	add_child(root)

	var title := Label.new()
	title.text = "🎲 다이스마블 프로토타입 (M0)"
	root.add_child(title)

	info_label = Label.new()
	root.add_child(info_label)

	players_label = Label.new()
	root.add_child(players_label)

	board_label = RichTextLabel.new()
	board_label.bbcode_enabled = true
	board_label.fit_content = true
	board_label.custom_minimum_size = Vector2(0, 100)
	root.add_child(board_label)

	dice_label = Label.new()
	dice_label.text = "🎲 -"
	root.add_child(dice_label)

	var btns := HBoxContainer.new()
	root.add_child(btns)
	roll_btn = Button.new()
	roll_btn.text = "🎲 주사위 굴리기"
	roll_btn.pressed.connect(func(): roll_pressed.emit())
	btns.add_child(roll_btn)
	buy_btn = Button.new()
	buy_btn.text = "구매"
	buy_btn.pressed.connect(func(): buy_pressed.emit())
	btns.add_child(buy_btn)
	skip_btn = Button.new()
	skip_btn.text = "구매 안 함"
	skip_btn.pressed.connect(func(): skip_pressed.emit())
	btns.add_child(skip_btn)

	var log_title := Label.new()
	log_title.text = "─ 로그 ─"
	root.add_child(log_title)
	log_label = Label.new()
	log_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(log_label)

func _on_log(_t: String) -> void:
	refresh_log()

func refresh_log() -> void:
	log_label.text = "\n".join(GameState.log_lines)

func set_buttons(roll: bool, buy: bool, skip: bool) -> void:
	roll_btn.disabled = not roll
	buy_btn.visible = buy
	skip_btn.visible = skip

func refresh() -> void:
	var p: PlayerState = GameState.current_player()
	info_label.text = "차례: %s   (목표 %d바퀴)" % [p.name, GameState.target_laps]
	var lines: Array = []
	for pl in GameState.players:
		lines.append("%s | 골드 %d | 위치 %d | 바퀴 %d | HP %d/%d | 자산 %d" % [
			pl.name, pl.gold, pl.position, pl.laps, pl.hp, pl.max_hp, GameState.net_worth(pl)])
	players_label.text = "\n".join(lines)
	board_label.text = _board_string()
	refresh_log()

func _board_string() -> String:
	var parts: Array = []
	for i in GameState.board.size():
		var t: TileData = GameState.board[i]
		var mark := ""
		for pi in GameState.players.size():
			if GameState.players[pi].position == i:
				mark += "P%d" % (pi + 1)
		var own := ""
		if t.type == TileData.Type.LAND and t.owner_index >= 0:
			own = "*%d" % (t.owner_index + 1)
		var here := ""
		if mark != "":
			here = "<%s>" % mark
		parts.append("[%d:%s%s%s]" % [i, _short(t), own, here])
	return " ".join(parts)

func _short(t: TileData) -> String:
	match t.type:
		TileData.Type.START:
			return "출발"
		TileData.Type.LAND:
			return t.display_name
		TileData.Type.BATTLE:
			return "전투"
		TileData.Type.TREASURE:
			return "보물"
		TileData.Type.EMPTY:
			return "쉼터"
	return "?"

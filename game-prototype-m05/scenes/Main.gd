extends Control
## M0.5 오케스트레이터: 파밍 턴 진행 + 침공 전환.

const BoardViewScript = preload("res://scenes/BoardView.gd")
const InvasionViewScript = preload("res://scenes/InvasionView.gd")

var board_view: BoardView
var invasion_view: InvasionView

func _ready() -> void:
	board_view = BoardViewScript.new()
	add_child(board_view)
	invasion_view = InvasionViewScript.new()
	add_child(invasion_view)
	invasion_view.visible = false

	board_view.roll_pressed.connect(_on_roll)
	board_view.train_pressed.connect(_on_train)
	board_view.sabotage_pressed.connect(_on_sabotage)
	board_view.invade_pressed.connect(_on_invade)
	invasion_view.finished.connect(_on_invasion_finished)

	GameState.add_log("내 보드를 파밍하며 전투력을 키우고, 라운드 3부터 AI 보드를 침공하세요.")
	GameState.add_log("빨리 도는 건 순수 주사위 운 — 빠르면 축적이 얇아 AI에게 역으로 털릴 수 있음.")
	_refresh_board()

func _refresh_board() -> void:
	var active: bool = not GameState.won
	board_view.set_buttons(
		active,
		active and GameState.can_train(),
		active and GameState.can_sabotage(),
		active and GameState.invasion_available)
	board_view.refresh()

func _on_train() -> void:
	if GameState.train():
		GameState.add_log("훈련 완료: ATK +%d, HP +%d (골드 -%d)" % [
			GameState.TRAIN_ATK, GameState.TRAIN_HP, GameState.TRAIN_COST])
	_refresh_board()

func _on_sabotage() -> void:
	if GameState.sabotage():
		GameState.add_log("AI 보드에 방해 트랩 설치! 코어 -%d (누적 -%d)" % [
			GameState.SABO_DMG, GameState.ai_core_damage])
	_refresh_board()

func _on_roll() -> void:
	if GameState.won:
		return
	# 1) 모든 칸 축적
	GameState.accumulate_tick()
	# 2) 주사위 이동(순수 운)
	var die: int = RNG.roll_die()
	board_view.dice_label.text = "🎲 %d" % die
	var lapped: bool = GameState.move_hero(die)
	if lapped:
		GameState.add_log("완주! 라운드 %d 도달 (티어 %d)" % [GameState.round_count, GameState.tier])
	# 3) 착지 칸 처리
	var t: FarmTile = GameState.board[GameState.pos]
	if t.type == FarmTile.Type.FARM:
		var got: int = GameState.harvest_tile(t)
		GameState.add_log("주사위 %d → '%s' 수거 +%d (개발 %d)" % [die, t.name, got, t.dev])
	else:
		GameState.add_log("주사위 %d → 출발점 통과" % die)
	# 4) AI 방어 성장
	GameState.ai_tick()
	_refresh_board()

func _on_invade() -> void:
	if not GameState.invasion_available or GameState.won:
		return
	var obs: Array = []
	for i in GameState.ai_trap_count():
		obs.append({"kind": "trap", "dmg": GameState.ai_trap_dmg()})
	obs.append({"kind": "defender", "hp": GameState.ai_defender_hp(), "atk": GameState.ai_defender_atk()})
	obs.append({"kind": "core", "hp": GameState.ai_core_hp(), "atk": GameState.ai_defender_atk()})
	GameState.add_log("침공 개시 (AI 티어 %d)" % GameState.ai_tier)
	board_view.visible = false
	invasion_view.visible = true
	invasion_view.start(GameState.hero, obs)

func _on_invasion_finished(success: bool) -> void:
	invasion_view.visible = false
	board_view.visible = true
	if success:
		GameState.won = true
		GameState.add_log("🎉 침공 성공! AI 코어 격파 — 슬라이스 클리어!")
	else:
		var h: HeroState = GameState.hero
		h.hp = max(1, h.max_hp / 2)   # 후퇴, 절반 회복
		GameState.add_log("침공 실패/후퇴. 재정비 후 다시 파밍(AI는 계속 강해짐).")
	_refresh_board()

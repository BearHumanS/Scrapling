extends Control
## M0 오케스트레이터: 턴 흐름을 구동하고 보드/전투 화면을 전환한다.
## 흐름: 주사위 → 이동 → 칸 처리 → (구매/전투) → 턴 종료 → 다음.

const BoardViewScript = preload("res://scenes/BoardView.gd")
const CombatViewScript = preload("res://scenes/CombatView.gd")

var board_view: BoardView
var combat_view: CombatView
var tsm := TurnStateMachine.new()

var _awaiting_buy: bool = false
var _last_die: int = 0

func _ready() -> void:
	board_view = BoardViewScript.new()
	add_child(board_view)
	combat_view = CombatViewScript.new()
	add_child(combat_view)
	combat_view.visible = false

	board_view.roll_pressed.connect(_on_roll)
	board_view.buy_pressed.connect(_on_buy)
	board_view.skip_pressed.connect(_on_skip)
	combat_view.finished.connect(_on_combat_finished)

	GameState.add_log("게임 시작! %d바퀴 후 자산이 많은 쪽이 승리." % GameState.target_laps)
	_start_turn()

func _start_turn() -> void:
	if GameState.game_over:
		return
	tsm.to(TurnStateMachine.Phase.IDLE)
	_awaiting_buy = false
	board_view.set_buttons(true, false, false)
	board_view.refresh()
	var p: PlayerState = GameState.current_player()
	if p.is_ai:
		board_view.set_buttons(false, false, false)
		await get_tree().create_timer(0.6).timeout
		_on_roll()

func _on_roll() -> void:
	tsm.to(TurnStateMachine.Phase.ROLL)
	var p: PlayerState = GameState.current_player()
	_last_die = RNG.roll_die()
	board_view.dice_label.text = "🎲 %s → %d" % [p.name, _last_die]
	GameState.add_log("%s 주사위: %d" % [p.name, _last_die])
	board_view.set_buttons(false, false, false)
	await get_tree().create_timer(0.3).timeout
	_do_move(p)

func _do_move(p: PlayerState) -> void:
	tsm.to(TurnStateMachine.Phase.MOVE)
	var size: int = GameState.board.size()
	var new_pos: int = p.position + _last_die
	if new_pos >= size:
		new_pos = new_pos % size
		p.laps += 1
		p.gold += 100
		p.hp = min(p.max_hp, p.hp + 20)
		GameState.add_log("%s 출발점 통과! +100골드, +20HP (%d바퀴)" % [p.name, p.laps])
	p.position = new_pos
	board_view.refresh()
	await get_tree().create_timer(0.3).timeout
	_resolve_tile(p)

func _resolve_tile(p: PlayerState) -> void:
	tsm.to(TurnStateMachine.Phase.RESOLVE)
	var t: TileData = GameState.board[p.position]
	match t.type:
		TileData.Type.START:
			GameState.add_log("%s 출발점에 도착." % p.name)
			_end_turn()
		TileData.Type.EMPTY:
			GameState.add_log("%s 쉼터에서 휴식." % p.name)
			_end_turn()
		TileData.Type.TREASURE:
			var g: int = RNG.range_i(30, 90)
			p.gold += g
			GameState.add_log("%s 보물 발견! +%d골드" % [p.name, g])
			_end_turn()
		TileData.Type.BATTLE:
			_start_combat(p)
		TileData.Type.LAND:
			_resolve_land(p, t)
		_:
			_end_turn()

func _resolve_land(p: PlayerState, t: TileData) -> void:
	if t.owner_index == -1:
		if p.is_ai:
			if AIController.should_buy(p, t):
				_buy_tile(p, t)
			else:
				GameState.add_log("%s 구매 보류 (%s)" % [p.name, t.display_name])
			_end_turn()
		else:
			if p.gold >= t.price:
				GameState.add_log("빈 땅: %s (가격 %d). 구매하시겠어요?" % [t.display_name, t.price])
				_awaiting_buy = true
				tsm.to(TurnStateMachine.Phase.ACTION)
				board_view.set_buttons(false, true, true)
			else:
				GameState.add_log("골드 부족으로 %s 구매 불가." % t.display_name)
				_end_turn()
	elif t.owner_index == GameState.current:
		GameState.add_log("%s 자신의 땅(%s)." % [p.name, t.display_name])
		_end_turn()
	else:
		var toll: int = int(round(t.price * 0.2))
		var pay: int = min(toll, p.gold)
		p.gold -= pay
		GameState.players[t.owner_index].gold += pay
		GameState.add_log("%s 통행료 %d 지불 → %s" % [p.name, pay, GameState.players[t.owner_index].name])
		_end_turn()

func _buy_tile(p: PlayerState, t: TileData) -> void:
	p.gold -= t.price
	t.owner_index = GameState.current
	p.owned_tiles.append(p.position)
	GameState.add_log("%s 구매: %s (-%d골드)" % [p.name, t.display_name, t.price])

func _on_buy() -> void:
	if not _awaiting_buy:
		return
	_awaiting_buy = false
	var p: PlayerState = GameState.current_player()
	var t: TileData = GameState.board[p.position]
	_buy_tile(p, t)
	board_view.set_buttons(false, false, false)
	_end_turn()

func _on_skip() -> void:
	if not _awaiting_buy:
		return
	_awaiting_buy = false
	GameState.add_log("%s 구매 보류." % GameState.current_player().name)
	board_view.set_buttons(false, false, false)
	_end_turn()

func _start_combat(p: PlayerState) -> void:
	tsm.to(TurnStateMachine.Phase.ACTION)
	var enemy := EnemyData.new()  # 슬라임 기본값
	if p.is_ai:
		# AI는 자동 전투로 빠르게 해결
		var php: int = p.hp
		var ehp: int = enemy.max_hp
		while ehp > 0 and php > 0:
			ehp -= Balance.damage(p.atk, 1.0, enemy.def, false)
			if ehp <= 0:
				break
			php -= Balance.damage(enemy.atk, 1.0, p.def, false)
		if php > 0:
			p.hp = php
			p.gold += enemy.gold_reward
			GameState.add_log("%s 전투 승리! +%d골드" % [p.name, enemy.gold_reward])
		else:
			p.hp = p.max_hp
			var loss: int = int(round(p.gold * 0.2))
			p.gold -= loss
			GameState.add_log("%s 전투 패배... 부활, -%d골드" % [p.name, loss])
		_end_turn()
	else:
		board_view.visible = false
		combat_view.visible = true
		combat_view.start(p, enemy)

func _on_combat_finished(victory: bool) -> void:
	combat_view.visible = false
	board_view.visible = true
	var p: PlayerState = GameState.current_player()
	if victory:
		p.gold += 40
		GameState.add_log("%s 전투 승리! +40골드" % p.name)
	else:
		p.hp = p.max_hp
		var loss: int = int(round(p.gold * 0.2))
		p.gold -= loss
		GameState.add_log("%s 전투 패배... 부활, -%d골드" % [p.name, loss])
	_end_turn()

func _end_turn() -> void:
	tsm.to(TurnStateMachine.Phase.END)
	board_view.refresh()
	for pl in GameState.players:
		if pl.laps >= GameState.target_laps:
			_game_over()
			return
	await get_tree().create_timer(0.4).timeout
	GameState.advance_turn()
	_start_turn()

func _game_over() -> void:
	tsm.to(TurnStateMachine.Phase.GAME_OVER)
	GameState.game_over = true
	var best: PlayerState = GameState.players[0]
	for pl in GameState.players:
		if GameState.net_worth(pl) > GameState.net_worth(best):
			best = pl
	GameState.add_log("게임 종료! 승자: %s (자산 %d)" % [best.name, GameState.net_worth(best)])
	board_view.set_buttons(false, false, false)
	board_view.refresh()

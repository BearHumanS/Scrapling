extends Node
## 게임 상태 + 코어 규칙(메모리). 데이터 주도 보드/플레이어 생성.

var board: Array = []          # Array[TileData]
var players: Array = []        # Array[PlayerState]
var current: int = 0
var target_laps: int = 3
var game_over: bool = false
var log_lines: Array = []

func _ready() -> void:
	reset_game()

func reset_game() -> void:
	game_over = false
	current = 0
	log_lines.clear()
	_build_board()
	_build_players()

func _build_board() -> void:
	board.clear()
	board.append(_tile(TileData.Type.START, "출발", 0))
	board.append(_tile(TileData.Type.LAND, "새싹 농장", 100))
	board.append(_tile(TileData.Type.BATTLE, "몬스터 출현", 0))
	board.append(_tile(TileData.Type.LAND, "숲속 오두막", 120))
	board.append(_tile(TileData.Type.TREASURE, "보물상자", 0))
	board.append(_tile(TileData.Type.LAND, "강변 마을", 140))
	board.append(_tile(TileData.Type.EMPTY, "쉼터", 0))
	board.append(_tile(TileData.Type.LAND, "언덕 요새", 160))
	board.append(_tile(TileData.Type.BATTLE, "몬스터 출현", 0))
	board.append(_tile(TileData.Type.LAND, "광산", 180))
	board.append(_tile(TileData.Type.TREASURE, "보물상자", 0))
	board.append(_tile(TileData.Type.LAND, "고원 상단", 200))
	board.append(_tile(TileData.Type.EMPTY, "쉼터", 0))
	board.append(_tile(TileData.Type.LAND, "성문 거리", 220))
	board.append(_tile(TileData.Type.BATTLE, "몬스터 출현", 0))
	board.append(_tile(TileData.Type.LAND, "왕성", 260))

func _tile(t: int, n: String, price: int) -> TileData:
	var td := TileData.new()
	td.type = t
	td.display_name = n
	td.price = price
	td.owner_index = -1
	return td

func _build_players() -> void:
	players.clear()
	var p1 := PlayerState.new()
	p1.name = "플레이어"
	p1.is_ai = false
	players.append(p1)
	var p2 := PlayerState.new()
	p2.name = "AI 라이벌"
	p2.is_ai = true
	players.append(p2)

func current_player() -> PlayerState:
	return players[current]

func advance_turn() -> void:
	current = (current + 1) % players.size()

func net_worth(p: PlayerState) -> int:
	var v: int = p.gold
	for i in p.owned_tiles:
		v += board[i].price
	return v

func add_log(t: String) -> void:
	log_lines.append(t)
	if log_lines.size() > 8:
		log_lines = log_lines.slice(log_lines.size() - 8)
	EventBus.log_message.emit(t)

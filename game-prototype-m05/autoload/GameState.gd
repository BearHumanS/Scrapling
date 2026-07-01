extends Node
## M0.5 상태 + 규칙. 내 보드 파밍(축적+상한) / 라운드=완주 / 3라운드 티어업 /
## AI 보드는 라운드에 따라 방어(트랩·수비·코어)가 성장.

var board: Array = []          # Array[FarmTile] — 내 보드
var hero: HeroState
var pos: int = 0
var round_count: int = 0
var tier: int = 1
var turn_count: int = 0
var total_harvest: int = 0

# AI 보드(추상): 라운드에 따라 방어 성장
var ai_round: int = 0
var ai_tier: int = 1
var ai_core_damage: int = 0   # 사보타주 누적 피해(내가 상대 보드에 심은 방해)

# 골드 사용 비용/효과
const TRAIN_COST := 30
const TRAIN_ATK := 3
const TRAIN_HP := 15
const SABO_COST := 40
const SABO_DMG := 60

var invasion_available: bool = false
var won: bool = false
var log_lines: Array = []

func _ready() -> void:
	reset_game()

func reset_game() -> void:
	pos = 0
	round_count = 0
	tier = 1
	turn_count = 0
	total_harvest = 0
	ai_round = 0
	ai_tier = 1
	ai_core_damage = 0
	invasion_available = false
	won = false
	log_lines.clear()
	hero = HeroState.new()
	_build_board()

func _build_board() -> void:
	board.clear()
	var start := FarmTile.new()
	start.type = FarmTile.Type.START
	start.name = "출발"
	board.append(start)
	var names := ["농장", "광산", "숲", "강가", "목장", "대장간", "시장", "제단",
		"채석장", "과수원", "사냥터", "약초밭", "마구간", "창고", "전망대"]
	for n in names:
		var t := FarmTile.new()
		t.type = FarmTile.Type.FARM
		t.name = n
		board.append(t)

# 매 턴: 모든 파밍 칸에 자원 축적(상한까지)
func accumulate_tick() -> void:
	for t in board:
		if t.type == FarmTile.Type.FARM:
			var g: float = Balance.gen(tier, t.dev)
			var cap_v: float = Balance.cap(tier)
			t.store = min(cap_v, t.store + g)

# 착지 칸 수거 + 개발도 증가
func harvest_tile(t: FarmTile) -> int:
	var got: int = int(t.store)
	total_harvest += got
	hero.gold += got
	t.store = 0.0
	t.dev += 1
	return got

# 골드 → 자기 강화(훈련)
func can_train() -> bool:
	return hero.gold >= TRAIN_COST

func train() -> bool:
	if not can_train():
		return false
	hero.gold -= TRAIN_COST
	hero.atk += TRAIN_ATK
	hero.max_hp += TRAIN_HP
	hero.hp += TRAIN_HP
	return true

# 골드 → 상대 보드 방해(사보타주: 코어에 트랩 설치)
func can_sabotage() -> bool:
	return hero.gold >= SABO_COST

func sabotage() -> bool:
	if not can_sabotage():
		return false
	hero.gold -= SABO_COST
	ai_core_damage += SABO_DMG
	return true

# 이동(순수 주사위). 완주 시 라운드/티어 갱신. 반환: 완주 여부
func move_hero(die: int) -> bool:
	var size: int = board.size()
	var lapped: bool = false
	if pos + die >= size:
		lapped = true
		round_count += 1
		tier = 1 + round_count / 3
		if round_count >= 3:
			invasion_available = true
	pos = (pos + die) % size
	return lapped

# AI 방어 성장(플레이어 턴 5회마다 1라운드)
func ai_tick() -> void:
	turn_count += 1
	if turn_count % 5 == 0:
		ai_round += 1
		ai_tier = 1 + ai_round / 3

func ai_core_hp() -> int:
	return max(20, 80 * ai_tier + 15 * ai_round - ai_core_damage)

func ai_defender_hp() -> int:
	return 40 * ai_tier

func ai_defender_atk() -> int:
	return 8 * ai_tier

func ai_trap_dmg() -> int:
	return 15 * ai_tier

func ai_trap_count() -> int:
	return min(ai_round, 3)

func add_log(t: String) -> void:
	log_lines.append(t)
	if log_lines.size() > 8:
		log_lines = log_lines.slice(log_lines.size() - 8)
	EventBus.log_message.emit(t)

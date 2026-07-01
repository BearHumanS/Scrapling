extends RefCounted
class_name TurnStateMachine
## 턴 진행 상태 추적(경량). architecture.md §3의 상태 기계.
## M0에서는 Main.gd가 흐름을 구동하고, 여기서 현재 페이즈만 보관한다.

enum Phase { IDLE, ROLL, MOVE, RESOLVE, ACTION, END, GAME_OVER }

var phase: int = Phase.IDLE

func to(p: int) -> void:
	phase = p

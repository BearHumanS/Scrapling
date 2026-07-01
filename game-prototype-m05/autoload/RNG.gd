extends Node
## 시드 기반 난수(결정적). 완주 속도는 순수 이 주사위 운.

var rng := RandomNumberGenerator.new()

func _ready() -> void:
	rng.seed = 20260701

func roll_die() -> int:
	return rng.randi_range(1, 6)

func range_i(a: int, b: int) -> int:
	return rng.randi_range(a, b)

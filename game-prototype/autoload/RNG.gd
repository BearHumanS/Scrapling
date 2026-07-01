extends Node
## 시드 기반 난수. 결정적 프로토(리플레이/디버그 일관성) — architecture.md 원칙.

var rng := RandomNumberGenerator.new()

func _ready() -> void:
	rng.seed = 20260630  # 고정 시드

func roll_die() -> int:
	return rng.randi_range(1, 6)

func range_i(a: int, b: int) -> int:
	return rng.randi_range(a, b)

func chance(p: float) -> bool:
	return rng.randf() < p

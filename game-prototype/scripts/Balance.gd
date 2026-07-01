extends RefCounted
class_name Balance
## 전투 피해 공식(비율 감산) — balance-design.md §4.

static func damage(atk: int, coeff: float, target_def: int, defending: bool) -> int:
	var raw: float = float(atk) * coeff
	var mit: float = raw * (100.0 / (100.0 + float(target_def)))
	if defending:
		mit *= 0.5
	return int(max(1.0, round(mit)))

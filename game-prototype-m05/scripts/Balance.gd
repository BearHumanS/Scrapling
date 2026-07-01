extends RefCounted
class_name Balance
## 파밍 축적/전투 수식. round-tempo-system.md + balance-design.md.

const GEN_BASE := 5.0
const K := 0.5
const CAP_BASE := 20.0

static func gen(tier: int, dev: int) -> float:
	return GEN_BASE * float(tier) * (1.0 + K * float(dev))

static func cap(tier: int) -> float:
	return CAP_BASE * float(tier)

static func damage(atk: int, coeff: float, target_def: int, defending: bool) -> int:
	var raw: float = float(atk) * coeff
	var mit: float = raw * (100.0 / (100.0 + float(target_def)))
	if defending:
		mit *= 0.5
	return int(max(1.0, round(mit)))

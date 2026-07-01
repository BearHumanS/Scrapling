extends RefCounted
class_name AIController
## 더미 상대 휴리스틱(최소). board-system.md §6.

static func should_buy(player: PlayerState, tile: TileData) -> bool:
	# 여유 있게 살 수 있으면 구매(가격의 2배 이상 보유)
	return player.gold >= tile.price * 2

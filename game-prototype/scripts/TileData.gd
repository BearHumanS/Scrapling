extends Resource
class_name TileData
## 보드 한 칸의 데이터(데이터 주도 설계). board-system.md 참조.

enum Type { START, LAND, BATTLE, TREASURE, EMPTY }

@export var type: int = Type.EMPTY
@export var display_name: String = ""
@export var price: int = 0        # LAND 전용

var owner_index: int = -1         # 런타임 소유자(-1 = 미소유)

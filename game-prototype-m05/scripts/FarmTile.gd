extends RefCounted
class_name FarmTile
## 내 보드의 파밍 칸. 매 턴 store 축적(cap까지), 착지 시 수거+dev 증가.

enum Type { START, FARM }

var type: int = Type.FARM
var name: String = ""
var store: float = 0.0   # 축적된 자원
var dev: int = 0         # 개발도(착지 누적)

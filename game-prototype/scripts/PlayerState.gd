extends RefCounted
class_name PlayerState
## 플레이어 런타임 상태(메모리). 전사 클래스 기본값 — content-tables.md.

var name: String = ""
var is_ai: bool = false
var gold: int = 200
var position: int = 0
var laps: int = 0
var max_hp: int = 120
var hp: int = 120
var atk: int = 14
var def: int = 10
var max_en: int = 5
var en: int = 5
var owned_tiles: Array = []   # 보유 칸 인덱스

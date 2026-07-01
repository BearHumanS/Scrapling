extends Node
## 전역 시그널 허브. 로직과 UI를 느슨하게 연결한다.

signal log_message(text: String)
signal state_changed()

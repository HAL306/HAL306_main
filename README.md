# HAL306_main

コーディング規約

命名規則
クラス / 構造体	パスカルケース	PlayerController,ItemData
メソッド（関数）	パスカルケース	動詞から始める　TakeDamage(), Initialize()
プロパティ	      パスカルケース	public float MaxHp { get; }
変数            キャメルケース  currentHp, MoveSpeed
定数            CONSTANT_CASE	全大文字＋スネークケース（MAX_LEVEL）

bool変数にはis, can, has などのプレフィックス（接頭辞）をつける　
isDead （死んでいるか）
canJump （ジャンプできるか）
hasKey （鍵を持っているか）

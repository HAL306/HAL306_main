using UnityEngine;
using System;

public abstract class BossAttackBase : MonoBehaviour
{
    // 攻撃終了時にボス本体へ通知するためのコールバック
    protected Action onAttackComplete;

    protected virtual void Awake()
    {
        // 最初は自身のUpdateが呼ばれないように無効化しておく
        this.enabled = false;
    }

    /// <summary>
    /// 移行判定関数（virtualなので、子クラスで override して条件を追加できる）
    /// BossControllerで呼び出される
    /// trueのときこの行動に移行する
    /// </summary>
    public virtual bool CanExecute() { return false; }

    /// <summary>
    /// ボス本体から呼ばれる攻撃開始処理
    /// 派生クラスで呼ぶ必要はない
    /// </summary>
    public void BeginAttack(Action onComplete)
    {
        onAttackComplete = onComplete;

        // コンポーネントを有効化し、UnityのUpdate/FixedUpdateを自動で回し始める
        this.enabled = true;

        OnBegin(); // 派生クラスでの初期化用
    }

    /// <summary>
    /// 攻撃が完了した時に派生クラスから呼ぶ処理
    /// </summary>
    protected void EndAttack()
    {
        // 自身のUpdateを止める
        this.enabled = false;

        OnEnd(); // 派生クラスでの終了処理用

        // ボス本体に通知
        onAttackComplete?.Invoke();
    }

    // 派生クラスでオーバーライドして使う
    protected virtual void OnBegin() { }
    protected virtual void OnEnd() { }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BattleUnit : MonoBehaviour
{
    // 変更:モンスターはBattleSystemから受け取る
    [SerializeField] bool isPlayerUnit;
    [SerializeField] BattleHud hud;

    public Pokemon Pokemon { get; set; }
    public bool IsPlayerUnit { get => isPlayerUnit; }
    public BattleHud Hud { get => hud; }

    Vector3 originalPos;
    Color originalColor;
    Image image;

    // バトルで使うモンスターを保持
    // モンスターの画像を反映する

    private void Awake()
    {
        image = GetComponent<Image>();
        originalPos = transform.localPosition;
        originalColor = image.color;
    }

    public void Setup(Pokemon pokemon)
    {
        // _baseからレベルに応じたモンスターを生成する
        // BattleSystemで使うからプロパティに入れる
        Pokemon = pokemon;

        if (isPlayerUnit)
        {
            image.sprite = Pokemon.Base.BackSprite;
        }
        else
        {
            image.sprite = Pokemon.Base.FrontSprite;
        }
        hud.SetData(pokemon);
        hud.gameObject.SetActive(true);
        image.color = originalColor;
        PlayerEnterAnimation();
    }

    public void Clear()
    {
        hud.gameObject.SetActive(false);
    }

    // 登場Anim
    public void PlayerEnterAnimation()
    {
        if (isPlayerUnit)
        {
            // 左端に配置
            transform.localPosition = new Vector3(-850, originalPos.y);
        }
        else
        {
            // 右端に配置
            transform.localPosition = new Vector3(850, originalPos.y);
        }
        // 戦闘時の位置までアニメーション
        transform.DOLocalMoveX(originalPos.x, 1f);
    }
    // 攻撃Anim
    public void PlayerAttackAnimation()
    {
        // シーケンス
        // 右に動いた後, 元の位置に戻る
        Sequence sequence = DOTween.Sequence();
        if (isPlayerUnit)
        {
            sequence.Append(transform.DOLocalMoveX(originalPos.x + 50f, 0.25f)); // 後ろに追加
        }
        else
        {
            sequence.Append(transform.DOLocalMoveX(originalPos.x - 50f, 0.25f)); // 後ろに追加
        }
        sequence.Append(transform.DOLocalMoveX(originalPos.x, 0.2f)); // 後ろに追加
    }
    // ダメージAnim
    public void PlayerHitAnimation()
    {
        // 色を一度GLAYにしてから戻す
        Sequence sequence = DOTween.Sequence();
        sequence.Append(image.DOColor(Color.gray, 0.1f));
        sequence.Append(image.DOColor(originalColor, 0.1f));
    }
    // 戦闘不能Anim
    public void PlayerFaintAnimation()
    {
        // 下にさがりながら,薄くなる
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveY(originalPos.y - 150f, 0.5f));
        sequence.Join(image.DOFade(0, 0.5f));
    }

}

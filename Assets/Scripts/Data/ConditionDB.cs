using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionDB
{
    // キー,Value
    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.Poison,
            new Condition()
            {
                Id = ConditionID.Poison,
                Name = "どく",
                StartMessage = "は毒になった",
                OnAfterTurn = (Pokemon pokemon) =>
                {
                    // 毒ダメージを与える
                    pokemon.UpdateHP(pokemon.MaxHP/8);
                    // メッセージを表示する
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}は毒のダメージをうける");
                }
            }
        },
        {
            ConditionID.Burn,
            new Condition()
            {
                Id = ConditionID.Burn,
                Name = "やけど",
                StartMessage = "は火傷をおった",
                OnAfterTurn = (Pokemon pokemon) =>
                {
                    // 毒ダメージを与える
                    pokemon.UpdateHP(pokemon.MaxHP/16);
                    // メッセージを表示する
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}は火傷のダメージをうける");
                }
            }
        },
        {
            ConditionID.Paralysis,
            new Condition()
            {
                Id = ConditionID.Paralysis,
                Name = "マヒ",
                StartMessage = "はマヒになった.",
                OnBeforeMove = (Pokemon pokemon) =>
                {
                    // ture:技が出せる, false:マヒで動けない

                    // ・一定確率で
                    // ・技が出せずに自分のターンが終わる

                    // 1,2,3,4が出る中で1が出たら（25%の確率で）
                    if (Random.Range(1,5) == 1)
                    {
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}はしびれて動けない.");
                        return false;
                    }
                    return true;
                }
            }
        },
        {
            ConditionID.Freeze,
            new Condition()
            {
                Id = ConditionID.Freeze,
                Name = "こおり",
                StartMessage = "はこおった.",
                OnBeforeMove = (Pokemon pokemon) =>
                {
                    // ture:技が出せる, false:氷で動けない

                    // ・一定確率で
                    // ・氷が溶けて技が出せる
                    // 1,2,3,4が出る中で1が出たら（25%の確率で）
                    if (Random.Range(1,5) == 1)
                    {
                        // 氷状態を解除
                        pokemon.CureStatus();
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}のこおりは溶けた.");
                        return true;
                    }
                    // 技が出せずに自分のターンが終わる
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}はこおって動けない.");
                    return false;
                }
            }
        },
        {
            ConditionID.Sleep,
            new Condition()
            {
                Id = ConditionID.Sleep,
                Name = "すいみん",
                StartMessage = "は眠った.",
                OnStart = (Pokemon pokemon) =>
                {
                    // 技を受けた時に、何ターン眠るか決める
                    pokemon.StatusTime = Random.Range(1,4); // 1,2,3ターンのどれか
                },
                OnBeforeMove = (Pokemon pokemon) =>
                {
                    // ture:技が出せる, false:眠って技が出せない

                    if (pokemon.StatusTime <= 0)
                    {
                        pokemon.CureStatus();
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}は目を覚ました.");
                        return true;
                    }
                    pokemon.StatusTime--;
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}は眠っている.");
                    return false;
                }
            }
        },

        {
            ConditionID.Confusion,
            new Condition()
            {
                Id = ConditionID.Confusion,
                Name = "こんらん",
                StartMessage = "は混乱した.",
                OnStart = (Pokemon pokemon) =>
                {
                    pokemon.VolatileStatusTime = Random.Range(1,5); // 1,2,3,4ターンのどれか
                },
                OnBeforeMove = (Pokemon pokemon) =>
                {
                    // ture:技が出せる, false:眠って技が出せない

                    // ・もし、カウントが0なら、混乱が解除され行動可能
                    if (pokemon.VolatileStatusTime <= 0)
                    {
                        pokemon.CureVolatileStatus();
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}は混乱がとけた.");
                        return true;
                    }
                    pokemon.VolatileStatusTime--;
                    // ・そうでないなら、一定確率(50%)で通常行動
                    if (Random.Range(1,3) == 1)
                    {
                        return true;
                    }

                    // ・その他の確率で自分を攻撃
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}は混乱している.");
                    pokemon.UpdateHP(pokemon.MaxHP/8);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name}は自分を攻撃した.");
                    return false;
                }
            }
        },


    };
}

// バトル終了時に回復しない

// 毒の実装
// ＊毒になるとどうなる？
// ・技の発動後に
// ・必ず
// ・ダメージを受ける


// まひの実装
// ＊マヒになるとどうなる？
// ・技の発動前に
// ・一定確率で
// ・技が出せずに自分のターンが終わる

// こおりの実装
// ＊こおりになるとどうなる？
// ・技の発動前に
// ・一定確率で
// ・氷が溶けて技が出せる
// ・その他の確率で
// ・技が出せずに自分のターンが終わる

// 睡眠の実装
// ＊睡眠技を受けた時
// ・何ターン寝るか決める
// -------
// ＊モンスターが技を使う時
// ・眠りのターンカウントを減らす
// ・眠りのターンカウントが0になったら、行動可能
// ・0じゃなかったら、行動不可能

// ------------
// 目標:混乱の実装
//・バトル終了時に回復する
// ＊混乱技を受けた時
// ・何ターン混乱するか決める
// -------
// ＊モンスターが技を使う時
// ・もし、カウントが0なら、混乱が解除され行動可能
// ・そうでないなら、一定確率で自分を攻撃
// ・その他の確率で通常行動



public enum ConditionID
{
    None,
    Poison,    // 毒
    Burn,      // 火傷
    Sleep,     // 睡眠
    Paralysis, // 麻痺
    Freeze,    // こおり
    Confusion,
}

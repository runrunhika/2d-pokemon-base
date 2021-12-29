using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public enum BattleState
{
    Start,
    ActionSelection, // 行動選択状態
    MoveSelection,   // 技選択状態
    RunningTurn,     // 技の実行
    Busy,            // 処理中
    PartyScreen,     // ポケモン選択状態
    BattleOver,      // バトル終了状態
    AboutToUse,      // トレーナーバトルでモンスターを倒したときに、こちらが入れ替えるかどうか
}

public enum BattleAction
{
    Move,
    SwitchPokemon,
    Item,
    Run,
}

// トレーナーバトル
// モンスターを倒したときに、モンスターを入れ替える
// キャンセルしたときの処理

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;

    // [SerializeField] GameController gameController;
    public UnityAction OnBattleOver;


    BattleState state;
    BattleState? preState; // ?はnullを含む
    int currentAction; // 0:Fight, 1:Run
    int currentMove; // 0:左上, 1:右上, 2:左下, 3:右下　の技
    int currentMember;
    bool aboutToUse; // モンスターを入れ替えるか？

    // これらの変数をどこから取得するの？
    PokemonParty playerParty;
    PokemonParty trainerParty;
    Pokemon wildPokemon;

    PlayerController player;
    TrainerController trainer;

    bool isTrainerBattle;

    // 野生ポケモンとのバトル
    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon)
    {
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;
        StartCoroutine(SetupBattle());
    }

    // トレーナーバトル
    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty)
    {
        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        playerImage.sprite = player.Sprite;
        trainerImage.sprite = trainer.Sprite;
        StartCoroutine(SetupBattle());
    }


    IEnumerator SetupBattle()
    {
        //・UIを隠す
        playerUnit.Clear();
        enemyUnit.Clear();


        // 野生とトレーナーの区別が必要
        if (!isTrainerBattle)
        {
            // モンスターの生成と描画
            playerUnit.Setup(playerParty.GetHealthyPokemon()); // Playerの戦闘可能ポケモンをセット
            enemyUnit.Setup(wildPokemon);  // 野生ポケモンをセット
            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
            yield return dialogBox.TypeDialog($"やせいの {enemyUnit.Pokemon.Base.Name} があらわれた.");
        }
        else
        {
            // トレーナーバトル
            //・モンスターを隠す
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);
            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            yield return dialogBox.TypeDialog($"{trainer.Name} がバトルを仕掛けてきた!");

            //・トレーナーがモンスターを出す
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            Pokemon enemyPokemon = trainerParty.GetHealthyPokemon();
            enemyUnit.Setup(enemyPokemon);
            yield return dialogBox.TypeDialog($"{trainer.Name} が {enemyPokemon.Base.Name}を出してきた!");
            //・Playerがモンスターを出す
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            Pokemon playerPokemon = playerParty.GetHealthyPokemon();
            playerUnit.Setup(playerPokemon);
            yield return dialogBox.TypeDialog($"いけ！{playerPokemon.Base.Name}!");
            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
        }

        partyScreen.Init();
        ActionSelection();
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogBox.EnableActionSelector(true);
        StartCoroutine(dialogBox.TypeDialog("どうする？"));
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableDialogText(false);
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableMoveSelector(true);
    }

    void OpenPartyAction()
    {
        state = BattleState.PartyScreen;
        // パーティスクリーンを表示
        // ポケモンデータを反映
        partyScreen.gameObject.SetActive(true);
        partyScreen.SetPartyData(playerParty.Pokemons);
    }

    IEnumerator AboutToUse(Pokemon newPokemon)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name}が{newPokemon.Base.Name}を出そうとしている.モンスターを入れ替えますか？");
        // Playerにこちらがモンスターを入れ替えるかきく
        dialogBox.EnableChoiceBox(true);
        state = BattleState.AboutToUse;

    }

    // faintedUnit:やられたモンスター
    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        // やられたモンスターが
        // pkayerUnitなら
        if (faintedUnit.IsPlayerUnit)
        {
            Pokemon nextPokemon = playerParty.GetHealthyPokemon();
            if (nextPokemon == null)
            {
                // ・いないならバトル終了
                BattleOver();
            }
            else
            {
                // ・他にモンスターがいるなら選択画面
                OpenPartyAction();
            }
        }
        else
        {
            if (isTrainerBattle)
            {
                // 次のモンスターがいる場合
                Pokemon nextPokemon = trainerParty.GetHealthyPokemon();
                if (nextPokemon == null)
                {
                    BattleOver();
                }
                else
                {
                    StartCoroutine(AboutToUse(nextPokemon));
                }
            }
            else
            {
                // 野生なら終了
                BattleOver();
            }
        }
    }

    void BattleOver()
    {
        state = BattleState.BattleOver;
        // 手持ちのポケモンのステータスを全てリセットする
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());
        OnBattleOver();
        isTrainerBattle = false;
    }


    IEnumerator RunTurns(BattleAction battleAction)
    {
        state = BattleState.RunningTurn;

        if (battleAction == BattleAction.Move)
        {
            // それぞれの技の決定
            playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentMove];
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            // 先攻の決定:何もなければPlayerが先攻
            bool playerGoesFirst = true; // true:Playerが先攻, false:Enemyが先攻
            BattleUnit firstUnit = playerUnit;
            BattleUnit secondUnit = enemyUnit;

            // それぞれの技の優先度を取得
            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            // 比較
            if (playerMovePriority < enemyMovePriority)
            {
                // 敵が先攻
                playerGoesFirst = false;
            }
            else if(playerMovePriority == enemyMovePriority)
            {
                // モンスターのSpeedが速い方が先攻
                if (playerUnit.Pokemon.Speed < enemyUnit.Pokemon.Speed)
                {
                    playerGoesFirst = false;
                }
            }

            if (playerGoesFirst == false)
            {
                firstUnit = enemyUnit;
                secondUnit = playerUnit;
            }

            Pokemon secondPokemon = secondUnit.Pokemon;

            // 先攻の処理
            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver)
            {
                yield break;
            }

            if (secondPokemon.HP > 0)
            {
                // 後攻の処理
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver)
                {
                    yield break;
                }
            }
        }
        else
        {
            if (battleAction == BattleAction.SwitchPokemon)
            {
                // モンスターの入れ替え
                // 入れ替えモンスターの決定
                Pokemon selectedMember = playerParty.Pokemons[currentMember];

                // 入れ替え処理
                yield return SwitchPokemon(selectedMember);
            }
            else if (battleAction == BattleAction.Item)
            {

            }

            // 敵の行動
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver)
            {
                yield break;
            }
        }

        if (state != BattleState.BattleOver)
        {
            ActionSelection();
        }
    }


    // 技の実装(実行するUnit, 対象Unit, 技)
    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        if (!canRunMove)
        {
            // 技が使えない場合でダメージを受ける場合もある（混乱）
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}は{move.Base.Name}をつかった");

        // 命中したかどうかチェックする
        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon))
        {
            sourceUnit.PlayerAttackAnimation();
            yield return new WaitForSeconds(0.7f);
            targetUnit.PlayerHitAnimation();

            // ステータス変化なら
            if (move.Base.Category == MoveCategory.Stat)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            }
            else
            {
                // Enemyダメージ計算
                DamageDetails damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                // HP反映
                yield return targetUnit.Hud.UpdateHP();
                // 相性/クリティカルのメッセージ
                yield return ShowDamageDetails(damageDetails);
            }

            // 追加効果をチェック
            //・move.Base.Secondariesがnullではない
            //・move.Base.Secondariesがある
            //・ターゲットがやられていない
            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0)
            {
                // それぞれの追加効果を反映
                foreach (SecondaryEffects secondary in move.Base.Secondaries)
                {
                    // 一定確率で状態異常
                    if (Random.Range(1,101) <= secondary.Chance)
                    {
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                    }
                }
            }

            // 戦闘不能ならメッセージ
            if (targetUnit.Pokemon.HP <= 0)
            {
                yield return dialogBox.TypeDialog($"{targetUnit.Pokemon.Base.Name}は戦闘不能");
                targetUnit.PlayerFaintAnimation();
                yield return new WaitForSeconds(0.7f);
                CheckForBattleOver(targetUnit);
            }
        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}の攻撃は外れた.");
        }


    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver)
        {
            yield break;
        }
        // RunningTurnまで待機する
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        // ターン終了時
        //（状態異常ダメージを受ける）
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.UpdateHP();
        // 戦闘不能ならメッセージ
        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}は戦闘不能");
            sourceUnit.PlayerFaintAnimation();
            yield return new WaitForSeconds(0.7f);
            CheckForBattleOver(sourceUnit);
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
    {
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
            {
                // 自分に対してステータス変化をする
                source.ApplyBoosts(effects.Boosts);
            }
            else
            {
                // 相手に対してステータス変化をする
                target.ApplyBoosts(effects.Boosts);
            }
        }

        // 何かしらの状態異常技であれば
        if (effects.Status != ConditionID.None)
        {
            target.SetStatus(effects.Status);
        }

        if (effects.VolatileStatus != ConditionID.None)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }



        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    // 当たるかどうかを判断する関数
    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target)
    {
        if (move.Base.AnyHit)
        {
            return true;
        }
        float moveAccuracy = move.Base.Accuracy; // 90%

        // 技を出す側のブーストされている命中率
        int accuracy = source.StatBoosts[Stat.Accuracy];
        // 技を受ける側のブーストされた回避率
        int evasion = target.StatBoosts[Stat.Evasion];

        float[] boostValues = new float[] { 1f, 4f/3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        // 命中率のブースト
        if (accuracy > 0)
        {
            moveAccuracy *= boostValues[accuracy];
        }
        else
        {
            moveAccuracy /= boostValues[-accuracy];
        }

        // 回避率のブースト
        if (evasion > 0)
        {
            moveAccuracy /= boostValues[evasion];
        }
        else
        {
            moveAccuracy *= boostValues[-evasion];
        }


        return Random.Range(1, 101) <= moveAccuracy;
    }

    // ステータス変化のログを表示する関数
    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        // ログがなくなるまでくり返す
        while (pokemon.StatusChanges.Count > 0)
        {
            // ログの取り出し
            string message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }


    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if(damageDetails.Critical > 1f)
        {
            yield return dialogBox.TypeDialog($"急所にあたった");
        }
        if (damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogBox.TypeDialog($"効果はバツグンだ");
        }
        else if (damageDetails.TypeEffectiveness < 1f)
        {
            yield return dialogBox.TypeDialog($"効果はいまひとつ");
        }
    }


    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if (state == BattleState.AboutToUse)
        {
            HandleAboutToUse();
        }
    }

    void HandleAboutToUse()
    {
        // 上or下キーを押すと選択が切り替わる
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            aboutToUse = !aboutToUse;
        }
        dialogBox.UpdateChoiceBox(aboutToUse);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogBox.EnableChoiceBox(false);
            if (aboutToUse)
            {
                // yes
                // モンスター入れ替えスクリーンを出す
                preState = BattleState.AboutToUse;
                OpenPartyAction();
            }
            else
            {
                // no
                // 今まで通り、相手がモンスターを出してくる
                StartCoroutine(SendNextTrainerPokemon());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            // キャンセル
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }



    // PlayerActionでの行動を処理する
    void HandleActionSelection()
    {
        // 下を入力するとRun, 上を入力するとFightになる
        // 0:Fight   1:Bag
        // 2:Pokemon 3:Run
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentAction++;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentAction--;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentAction += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentAction -= 2;
        }

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        // 色をつけてどちらを選択してるかわかるようにする
        dialogBox.UpdateActionSelection(currentAction);


        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                MoveSelection();
            }
            if (currentAction == 2)
            {
                preState = state;
                OpenPartyAction();
            }
        }
    }

    // 0:左上, 1:右上, 2:左下, 3:右下　の技:技選択モード
    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentMove++;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentMove--;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentMove += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentMove -= 2;
        }

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);


        dialogBox.UpdateMoveSelection(currentMove, playerUnit.Pokemon.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            // PPがなければ処理しない
            Move move = playerUnit.Pokemon.Moves[currentMove];
            if (move.PP == 0)
            {
                return;
            }
            // 技決定
            // ・技選択のUIは非表示
            dialogBox.EnableMoveSelector(false);
            // ・メッセージ復活
            dialogBox.EnableDialogText(true);
            // ・技決定の処理
            StartCoroutine(RunTurns(BattleAction.Move));
        }
        // キャンセル
        if (Input.GetKeyDown(KeyCode.X))
        {
            // 技の非表示
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            // PlayerActionにする
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentMember++;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentMember--;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentMember += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentMember -= 2;
        }

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Pokemons.Count - 1);

        // 選択中のモンスター名に色をつける
        partyScreen.UpdateMemberSelection(currentMember);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            // モンスター決定
            Pokemon selectedMember = playerParty.Pokemons[currentMember];
            // 入れ替える:現在のキャラと戦闘不能は無理
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessage("そのモンスターは瀕死です");
                return;
            }
            if (selectedMember == playerUnit.Pokemon)
            {
                partyScreen.SetMessage("同じモンスターです");
                return;
            }

            // ポケモン選択画面を消す
            partyScreen.gameObject.SetActive(false);

            // 入れ替え処理
            // 戦闘不能時は敵の攻撃がない
            // Playerが入れ替えアクションをした場合は、敵の攻撃がある
            // ・ ActionSelectionによって入れ替えを行うのか？
            if (preState == BattleState.ActionSelection)
            {
                preState = null;
                // 自分で入れ替える
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            }
            else
            {
                // 戦闘不能によって入れ替える
                StartCoroutine(SwitchPokemon(selectedMember));
            }

        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (playerUnit.Pokemon.HP <= 0)
            {
                partyScreen.SetMessage("モンスターは瀕死です.入れ替えるモンスターを選んでください.");
                return;
            }
            // ポケモン選択画面を閉じたい
            partyScreen.gameObject.SetActive(false);
            if (preState == BattleState.AboutToUse)
            {
                // トレーナーにモンスターを出させる
                preState = null;
                StartCoroutine(SendNextTrainerPokemon());
            }
            else
            {
                ActionSelection();
            }
        }
    }

    IEnumerator SwitchPokemon(Pokemon newPokemon)
    {
        state = BattleState.Busy;
        // 修正:戦闘不能なら,戻れはいらない&相手のターンにならない
        // 前のやつを下げる
        if (playerUnit.Pokemon.HP > 0)
        {
            yield return dialogBox.TypeDialog($"戻れ！{playerUnit.Pokemon.Base.Name}！");
            playerUnit.PlayerFaintAnimation();
            yield return new WaitForSeconds(1.5f);
        }


        // 新しいのを出す
        playerUnit.Setup(newPokemon); // Playerの戦闘可能ポケモンをセット
        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
        yield return dialogBox.TypeDialog($"いけ！{playerUnit.Pokemon.Base.Name}！");

        if (preState == null)
        {
            // そうでないなら今まで通り
            state = BattleState.RunningTurn;
        }
        else if (preState == BattleState.AboutToUse)
        {
            // 倒した後の入れ替えなら、トレーナーがモンスターを出してくる
            preState = null;
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    IEnumerator SendNextTrainerPokemon()
    {
        state = BattleState.Busy;
        // 新しいのを出す
        Pokemon nextPokemon = trainerParty.GetHealthyPokemon();
        enemyUnit.Setup(nextPokemon); // Playerの戦闘可能ポケモンをセット
        yield return dialogBox.TypeDialog($"{trainer.Name}は{nextPokemon.Base.Name}を出してきた!");
        state = BattleState.RunningTurn;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    Vector2 input;
    Character character;
    public UnityAction OnEncounted;
    public UnityAction<Collider2D> OnEnterTrainersView; // トレーナーの視界に入った時に実行したいこと
    [SerializeField] new string name;
    [SerializeField] Sprite sprite;

    public string Name { get => name; }
    public Sprite Sprite { get => sprite; }


    private void Awake()
    {
        character = GetComponent<Character>();
    }

    public void HandleUpdate()
    {
        if (!character.IsMoving)
        {
            // キーボードの入力方向に動く
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            // 斜め移動
            if (input.x != 0)
            {
                input.y = 0;
            }

            // 入力があったら
            if (input != Vector2.zero)
            {
                StartCoroutine(character.Move(input, OnMoveOver));
            }
        }
        character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Interact();
        }
    }

    void Interact()
    {
        // 向いてる方向
        Vector3 faceDirection = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        // 干渉する場所
        Vector3 interactPos = transform.position + faceDirection;

        // 干渉する場所にRayを飛ばす
        Collider2D collider2D = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.Instance.InteractableLayer);
        if (collider2D)
        {
            collider2D.GetComponent<IInteractable>()?.Interact(transform.position);
        }
    }

    void OnMoveOver()
    {
        CheckForEncounters();
        CheckIfInTrainerView();
    }

    // 自分の場所から、円のRayを飛ばして、草むらLayerに当たったら、ランダムエンカウント
    void CheckForEncounters()
    {
        if (Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.Instance.LongGrassLayer))
        {
            // ランダムランカウント
            if (Random.Range(0, 100) < 10)
            {
                // Random.Range(0, 100):0〜99までのどれかの数字が出る
                // 10より小さいの数字は0〜9までの10個:10%の確率
                // 10以上の数字は10〜99までの90個
                OnEncounted();
                character.Animator.IsMoving = false;
            }
        }
    }
    // 移動したところが、トレーナーの視界なら、エンカウント
    void CheckIfInTrainerView()
    {
        Collider2D trainerCollider2D = Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.Instance.FovLayer);
        if (trainerCollider2D)
        {
            Debug.Log("トレーナーの視界に入った");
            character.Animator.IsMoving = false;
            OnEnterTrainersView?.Invoke(trainerCollider2D);
        }
    }
}

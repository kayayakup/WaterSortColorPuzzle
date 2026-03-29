using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public class MoveAction
    {
        public BottleController source;
        public BottleController target;
        public int transferCount;
    }

    public Stack<MoveAction> moveHistory = new Stack<MoveAction>();

    public BottleController FirstBottle;
    public BottleController SecondBottle;
    [SerializeField] List<Sprite> sprites;
    bool control = false;

    [Header("Animation Polish")]
    public float bottleLiftHeight = 0.2f;
    public float bottleLiftSpeed = 20f;

    [Header("VFX & SFX")]
    public GameObject gameStartEffect;
    public GameObject backgroundEffect;
    public GameObject clickEffect;
    public AudioClip clickSFX;
    public AudioClip winSFX;
    public AudioClip startSFX;

    void Start()
    {
        if (gameStartEffect != null) Instantiate(gameStartEffect, Vector3.zero, Quaternion.identity);
        if (backgroundEffect != null) Instantiate(backgroundEffect, Vector3.zero, Quaternion.identity);

        SoundManager.instance.PlaySFX(startSFX);
    }

    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null && hit.collider.GetComponent<BottleController>() != null)
            {
                var hitBottle = hit.collider.GetComponent<BottleController>();

                if (FirstBottle == null)
                {
                    // Kaynak şişe seçimi: boş olamaz, dökme veya hedef beklemesi olamaz
                    if (hitBottle.isPouringOut || hitBottle.incomingPours > 0) return;
                    if (hitBottle.numberOfColorInBottle == 0) return;

                    if (clickEffect != null) Instantiate(clickEffect, (Vector3)mousePos2D + Vector3.back, Quaternion.identity);
                    if (SoundManager.instance != null) SoundManager.instance.PlaySFX(clickSFX);

                    FirstBottle = hitBottle;
                    FirstBottle.LiftUp(bottleLiftHeight, bottleLiftSpeed);
                }
                else if (FirstBottle == hitBottle)
                {
                    if (clickEffect != null) Instantiate(clickEffect, (Vector3)mousePos2D + Vector3.back, Quaternion.identity);
                    if (SoundManager.instance != null) SoundManager.instance.PlaySFX(clickSFX);

                    FirstBottle.LowerDown(bottleLiftSpeed);
                    FirstBottle = null;
                }
                else
                {
                    SecondBottle = hitBottle;

                    // Hedef şişe dökme işleminde olamaz (ama hedef olarak birden fazla kez kullanılabilir)
                    if (SecondBottle.isPouringOut) return;

                    if (clickEffect != null) Instantiate(clickEffect, (Vector3)mousePos2D + Vector3.back, Quaternion.identity);
                    if (SoundManager.instance != null) SoundManager.instance.PlaySFX(clickSFX);

                    // Renk uyumu kontrolü StartColorTransfer içinde yapılacak
                    int tCount = FirstBottle.StartColorTransfer(SecondBottle);
                    if (tCount > 0)
                    {
                        moveHistory.Push(new MoveAction { source = FirstBottle, target = SecondBottle, transferCount = tCount });
                        FirstBottle = null;
                        SecondBottle = null;
                    }
                    else
                    {
                        FirstBottle.LowerDown(bottleLiftSpeed);
                        FirstBottle = null;
                        SecondBottle = null;
                    }
                }
            }
        }
    }

    public void UndoMove()
    {
        if (moveHistory.Count == 0) return;
        if (LevelController.levelWinPoint == 0) return;

        MoveAction lastMove = moveHistory.Peek();

        if (lastMove.source.isPouringOut || lastMove.source.incomingPours > 0 ||
            lastMove.target.isPouringOut || lastMove.target.incomingPours > 0)
        {
            return;
        }

        moveHistory.Pop();
        lastMove.target.InstantUndoTo(lastMove.transferCount, lastMove.source);
    }
}
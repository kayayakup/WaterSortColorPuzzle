using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BottleController : MonoBehaviour
{
    public List<Color> bottleColors;
    public SpriteRenderer bottleMaskSR;

    [Header("Bottle Visuals")]
    public SpriteRenderer bottleFrontSR;
    public SpriteRenderer bottleBackSR;
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("Animation Speeds")]
    public float bottleMoveSpeed = 3.0f;
    public float bottleMoveBackSpeed = 2.0f;

    public AnimationCurve scaleRotationMC;
    public AnimationCurve fillAmountC;
    public AnimationCurve RotattionSpeedMultiplier;

    public List<float> fillAmounts;
    public List<float> rotationsValues;

    [Range(0, 4)]
    public int numberOfColorInBottle = 4;

    public Color topColor;
    public int numberOfTopColorLayers = 1;

    public Transform leftRotatePoint;
    public Transform rightRotatePoint;

    public LineRenderer lineRenderer;

    [HideInInspector] public bool isPouringOut = false;
    [HideInInspector] public int incomingPours = 0;
    [HideInInspector] public int reservedSlots = 0;
    [HideInInspector] public Color reservedColor = Color.clear;

    private Coroutine liftCoroutine;

    public void LiftUp(float liftHeight, float speed)
    {
        if (liftCoroutine != null) StopCoroutine(liftCoroutine);
        UpdateTopColorValues();
        liftCoroutine = StartCoroutine(LerpPosition(originPos + Vector3.up * liftHeight, speed));
    }

    public void LowerDown(float speed)
    {
        if (liftCoroutine != null) StopCoroutine(liftCoroutine);
        liftCoroutine = StartCoroutine(LerpPosition(originPos, speed));
    }

    IEnumerator LerpPosition(Vector3 target, float speed)
    {
        Vector3 start = transform.position;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * speed;
            if (t > 1) t = 1;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }

    [SerializeField] MenuController menuController;

    [SerializeField] GameObject fullBottleEffect;

    [Header("VFX & SFX")]
    public GameObject pourEffectPrefab;
    public AudioClip pourSFX;
    public AudioClip fillSFX;
    public AudioClip winSFX;
    public GameObject winVFX;

    Vector3 originPos;

    private class PourData
    {
        public BottleController target;
        public int colorTransferCount;
        public int rotationIdx;
        public float dirMultiplier;
        public Transform rotatePoint;
        public Color pourColor;
        public Vector3 srcOriginPos;
        public GameObject pourEffect;
    }

    void Start()
    {
        menuController = GameObject.Find("GameController").GetComponent<MenuController>();
        bottleMaskSR.material.SetFloat("_FillAmount", fillAmounts[numberOfColorInBottle]);

        originPos = transform.position;

        UpdateColorsOnShaders();
        UpdateTopColorValues();

        if (bottleFrontSR != null && frontSprite != null) bottleFrontSR.sprite = frontSprite;
        if (bottleBackSR != null && backSprite != null) bottleBackSR.sprite = backSprite;
    }

    public void ReserveSlots(int count, Color color)
    {
        int startIdx = numberOfColorInBottle + reservedSlots;
        reservedSlots += count;
        reservedColor = color;

        for (int i = 0; i < count; i++)
        {
            if (startIdx + i < bottleColors.Count)
                bottleColors[startIdx + i] = color;
        }
        UpdateColorsOnShaders();
    }

    public void CommitPour(int count)
    {
        numberOfColorInBottle = Mathf.Clamp(numberOfColorInBottle + count, 0, 4);
        reservedSlots = Mathf.Max(0, reservedSlots - count);
        if (reservedSlots <= 0)
        {
            reservedSlots = 0;
            reservedColor = Color.clear;
        }
    }

    // ══════════════════════════════════════════════
    //  PUBLIC ENTRY POINT — called by GameController
    // ══════════════════════════════════════════════
    public int StartColorTransfer(BottleController target)
    {
        // Önce kaynak şişenin en üst rengini al
        UpdateTopColorValues();
        Color sourceColor = topColor;

        // Hedef şişenin bu rengi kabul edip edemeyeceğini kontrol et
        if (!target.FillBottleCheck(sourceColor))
            return 0;

        isPouringOut = true;
        target.incomingPours++;
        if (liftCoroutine != null) StopCoroutine(liftCoroutine);

        Transform rotatePoint;
        float dirMultiplier;
        if (transform.position.x > target.transform.position.x)
        {
            rotatePoint = leftRotatePoint;
            dirMultiplier = -1.0f;
        }
        else
        {
            rotatePoint = rightRotatePoint;
            dirMultiplier = 1.0f;
        }

        int totalExpected = target.numberOfColorInBottle + target.reservedSlots;
        int availableSlots = 4 - totalExpected;
        int transferCount = Mathf.Min(numberOfTopColorLayers, availableSlots);

        if (transferCount <= 0)
        {
            isPouringOut = false;
            target.incomingPours--;
            LowerDown(20f);
            return 0;
        }

        int rotIdx = 3 - (numberOfColorInBottle - Mathf.Min(availableSlots, numberOfTopColorLayers));
        rotIdx = Mathf.Clamp(rotIdx, 0, 3);

        target.ReserveSlots(transferCount, sourceColor);

        GetComponent<BoxCollider2D>().enabled = false;

        PourData pd = new PourData
        {
            target = target,
            colorTransferCount = transferCount,
            rotationIdx = rotIdx,
            dirMultiplier = dirMultiplier,
            rotatePoint = rotatePoint,
            pourColor = sourceColor,
            srcOriginPos = originPos,
            pourEffect = null
        };

        StartCoroutine(MoveBottle(pd));
        return transferCount;
    }

    public void InstantUndoTo(int count, BottleController originalSource)
    {
        Color colorToGive = topColor;

        numberOfColorInBottle -= count;
        originalSource.numberOfColorInBottle += count;

        for (int i = 0; i < count; i++)
        {
            bottleColors[numberOfColorInBottle + i] = Color.clear;
        }
        for (int i = 0; i < count; i++)
        {
            originalSource.bottleColors[originalSource.numberOfColorInBottle - 1 - i] = colorToGive;
        }

        UpdateColorsOnShaders();
        UpdateTopColorValues();
        bottleMaskSR.material.SetFloat("_FillAmount", fillAmounts[numberOfColorInBottle]);

        originalSource.UpdateColorsOnShaders();
        originalSource.UpdateTopColorValues();
        originalSource.bottleMaskSR.material.SetFloat("_FillAmount", originalSource.fillAmounts[originalSource.numberOfColorInBottle]);

        if (this.CompareTag("finish"))
        {
            this.tag = "Untagged";
            this.GetComponent<BoxCollider2D>().enabled = true;
            LevelController.levelWinPoint++;
        }
    }

    // ══════════════════════════════════════════════
    //  COROUTINE CHAIN — all state via PourData
    // ══════════════════════════════════════════════

    IEnumerator MoveBottle(PourData pd)
    {
        Vector3 moveStart = transform.position;
        Vector3 moveEnd;
        if (pd.rotatePoint == leftRotatePoint)
        {
            moveEnd = pd.target.rightRotatePoint.position;
        }
        else
        {
            moveEnd = pd.target.leftRotatePoint.position;
        }
        float t = 0;

        while (t <= 1)
        {
            transform.position = Vector3.Lerp(moveStart, moveEnd, t);
            t += Time.deltaTime * bottleMoveSpeed;
            yield return new WaitForEndOfFrame();
        }

        transform.position = moveEnd;

        StartCoroutine(RotateBottle(pd));
    }

    IEnumerator MoveBottleBack(PourData pd)
    {
        Vector3 moveStart = transform.position;
        Vector3 moveEnd = pd.srcOriginPos;

        float t = 0;

        while (t <= 1)
        {
            transform.position = Vector3.Lerp(moveStart, moveEnd, t);
            t += Time.deltaTime * bottleMoveBackSpeed;
            yield return new WaitForEndOfFrame();
        }
        transform.position = moveEnd;

        ControlBottle(pd);
    }

    private void ControlBottle(PourData pd)
    {
        isPouringOut = false;
        GetComponent<BoxCollider2D>().enabled = true;

        for (int i = numberOfColorInBottle; i < bottleColors.Count; i++)
        {
            bottleColors[i] = Color.clear;
        }
        UpdateColorsOnShaders();
        UpdateTopColorValues();

        if (pd.target != null)
        {
            pd.target.incomingPours--;
            pd.target.UpdateTopColorValues();
            CheckBottleComplete(pd.target);
        }

        CheckBottleComplete(this);
    }

    private void CheckBottleComplete(BottleController bottle)
    {
        if (bottle.numberOfColorInBottle == 4 && bottle.incomingPours == 0)
        {
            bool allSame = true;
            for (int x = 1; x < 4; x++)
            {
                if (!bottle.bottleColors[0].Equals(bottle.bottleColors[x]))
                {
                    allSame = false;
                    break;
                }
            }

            if (allSame)
            {
                Instantiate(fullBottleEffect, bottle.transform.position + new Vector3(0, 0.4f, 0), Quaternion.identity);
                if (SoundManager.instance != null)
                {
                    SoundManager.instance.PlaySFX(bottle.fillSFX);
                }
                bottle.tag = "finish";
                bottle.GetComponent<BoxCollider2D>().enabled = false;
                LevelController.levelWinPoint--;

                if (0 == LevelController.levelWinPoint)
                {
                    StartCoroutine(WaitWinEffect());
                }
            }
        }
    }

    IEnumerator WaitWinEffect()
    {
        if (winVFX != null)
        {
            Instantiate(winVFX, new Vector3(0, 0, 0), Quaternion.identity);
        }
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(winSFX);
        }
        menuController.WinPanel();
        yield return new WaitForSeconds(1.5f);
    }

    public void UpdateColorsOnShaders()
    {
        bottleMaskSR.material.SetColor("_C1", bottleColors[0]);
        bottleMaskSR.material.SetColor("_C2", bottleColors[1]);
        bottleMaskSR.material.SetColor("_C3", bottleColors[2]);
        bottleMaskSR.material.SetColor("_C4", bottleColors[3]);
    }

    public float timeToRotate = 1.0f;

    IEnumerator RotateBottle(PourData pd)
    {
        float t = 0;
        float lerpValue;
        float angleValue;
        float lastAngleValue = 0;

        int rIdx = Mathf.Clamp(pd.rotationIdx, 0, rotationsValues.Count - 1);

        while (t < timeToRotate)
        {
            lerpValue = t / timeToRotate;
            angleValue = Mathf.Lerp(0.0f, pd.dirMultiplier * rotationsValues[rIdx], lerpValue);

            transform.RotateAround(pd.rotatePoint.position, Vector3.forward, lastAngleValue - angleValue);

            bottleMaskSR.material.SetFloat("_SARM", scaleRotationMC.Evaluate(angleValue));

            if (fillAmounts[numberOfColorInBottle] > fillAmountC.Evaluate(angleValue) + 0.005f)
            {
                if (lineRenderer.enabled == false && fillAmountC.Evaluate(angleValue) < 0.63f)
                {
                    lineRenderer.startColor = pd.pourColor;
                    lineRenderer.endColor = pd.pourColor;
                    lineRenderer.SetPosition(0, pd.rotatePoint.position);
                    lineRenderer.SetPosition(1, pd.rotatePoint.position - Vector3.up * 1.45f);
                    lineRenderer.enabled = true;

                    if (pourEffectPrefab != null && pd.pourEffect == null)
                    {
                        pd.pourEffect = Instantiate(pourEffectPrefab, pd.rotatePoint.position, Quaternion.identity);
                        var ps = pd.pourEffect.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            var main = ps.main;
                            main.startColor = pd.pourColor;
                        }
                    }
                    if (SoundManager.instance != null && pourSFX != null)
                    {
                        SoundManager.instance.PlaySFX(pourSFX);
                    }
                }

                bottleMaskSR.material.SetFloat("_FillAmount", fillAmountC.Evaluate(angleValue));
                pd.target.FillUp(fillAmountC.Evaluate(lastAngleValue) - fillAmountC.Evaluate(angleValue));
            }

            t += Time.deltaTime * RotattionSpeedMultiplier.Evaluate(angleValue);
            lastAngleValue = angleValue;
            yield return new WaitForEndOfFrame();
        }

        angleValue = pd.dirMultiplier * rotationsValues[rIdx];
        bottleMaskSR.material.SetFloat("_SARM", scaleRotationMC.Evaluate(angleValue));
        bottleMaskSR.material.SetFloat("_FillAmount", fillAmountC.Evaluate(angleValue));

        numberOfColorInBottle = Mathf.Clamp(numberOfColorInBottle - pd.colorTransferCount, 0, 4);
        pd.target.CommitPour(pd.colorTransferCount);

        lineRenderer.enabled = false;
        if (pd.pourEffect != null)
        {
            Destroy(pd.pourEffect, 0.5f);
            pd.pourEffect = null;
        }
        StartCoroutine(RotateBottleBack(pd));
    }

    IEnumerator RotateBottleBack(PourData pd)
    {
        float t = 0;
        float lerpValue;
        float angleValue;

        int rIdx = Mathf.Clamp(pd.rotationIdx, 0, rotationsValues.Count - 1);

        float lastAngelValue = pd.dirMultiplier * rotationsValues[rIdx];

        while (t < timeToRotate)
        {
            lerpValue = t / timeToRotate;
            angleValue = Mathf.Lerp(pd.dirMultiplier * rotationsValues[rIdx], 0.0f, lerpValue);

            transform.RotateAround(pd.rotatePoint.position, Vector3.forward, lastAngelValue - angleValue);

            bottleMaskSR.material.SetFloat("_SARM", scaleRotationMC.Evaluate(angleValue));

            lastAngelValue = angleValue;

            t += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        UpdateTopColorValues();
        angleValue = 0f;
        transform.eulerAngles = new Vector3(0, 0, angleValue);
        bottleMaskSR.material.SetFloat("_SARM", scaleRotationMC.Evaluate(angleValue));

        StartCoroutine(MoveBottleBack(pd));
    }

    public void UpdateTopColorValues()
    {
        if (numberOfColorInBottle != 0)
        {
            numberOfTopColorLayers = 1;
            topColor = bottleColors[numberOfColorInBottle - 1];

            if (numberOfColorInBottle == 4)
            {
                if (bottleColors[3].Equals(bottleColors[2]))
                {
                    numberOfTopColorLayers = 2;
                    if (bottleColors[2].Equals(bottleColors[1]))
                    {
                        numberOfTopColorLayers = 3;
                        if (bottleColors[1].Equals(bottleColors[0]))
                        {
                            numberOfTopColorLayers = 4;
                        }
                    }
                }
            }
            else if (numberOfColorInBottle == 3)
            {
                if (bottleColors[2].Equals(bottleColors[1]))
                {
                    numberOfTopColorLayers = 2;
                    if (bottleColors[1].Equals(bottleColors[0]))
                    {
                        numberOfTopColorLayers = 3;
                    }
                }
            }
            else if (numberOfColorInBottle == 2)
            {
                if (bottleColors[1].Equals(bottleColors[0]))
                {
                    numberOfTopColorLayers = 2;
                }
            }
        }
        else
        {
            numberOfTopColorLayers = 0;
            topColor = Color.clear;
        }
    }

    public bool FillBottleCheck(Color colorToCheck)
    {
        if (isPouringOut) return false;

        int totalExpected = numberOfColorInBottle + reservedSlots;
        if (totalExpected >= 4) return false;

        if (totalExpected == 0) return true; // Boş şişe her rengi alır

        // Dolu veya kısmen dolu: üstteki renk veya rezerve edilen renk ile eşleşmeli
        if (reservedSlots > 0)
            return reservedColor.Equals(colorToCheck);
        else
            return bottleColors[numberOfColorInBottle - 1].Equals(colorToCheck);
    }

    private void FillUp(float fillAmountToAdd)
    {
        bottleMaskSR.material.SetFloat("_FillAmount", bottleMaskSR.material.GetFloat("_FillAmount") + fillAmountToAdd);
    }
}
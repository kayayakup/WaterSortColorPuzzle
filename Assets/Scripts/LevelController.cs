using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    [SerializeField] List<LevelCreateController> levels;
    [SerializeField] GameObject bottle;
    [SerializeField] List<Transform> bottleCreate;
    [SerializeField] LevelCreateController testLevel;
    [SerializeField] LevelCreateController activeLevel;

    [SerializeField] List<LevelCreateController> randomLevels;
    public static int levelWinPoint = 0;
    public Text levelText;
    float bottleSpace;
    int firstBlock, secondBlock;

    List<Color> colors, levelColors;

    void Start()
    {
        int randomLevel = UnityEngine.Random.Range(0, randomLevels.Count);

        activeLevel = MenuController.activeLevel < 50 ? levels[MenuController.activeLevel] : randomLevels[randomLevel];

        bottleSpace = activeLevel.bottles.Count < 11 ? 0.5f : 0.4f;

        CreateColorByLevel(activeLevel); // Seviyedeki renkleri oluştur (Level 27 dahil her seviye için)

        if (activeLevel.bottles.Count > 5)
        {
            firstBlock = activeLevel.bottles.Count / 2 + activeLevel.bottles.Count % 2;
            secondBlock = activeLevel.bottles.Count / 2;
            CreateBlocks(firstBlock, 0);
            CreateBlocks(secondBlock, 1);
        }
        else
        {
            firstBlock = activeLevel.bottles.Count;
            CreateBlocks(firstBlock, 2);
        }

        // Kazanma puanını hesapla: tüm katman sayısı / 4 (her tam şişe 4 katman)
        int totalColorLayers = 0;
        foreach (var b in activeLevel.bottles) totalColorLayers += b.numberBottle;
        levelWinPoint = totalColorLayers / 4;

        levelText.text = "LEVEL " + (MenuController.activeLevel + 1).ToString();
    }

    private void CreateColors(int count)
    {
        // Listeyi her seferinde sıfırdan oluştur veya temizle
        if (colors == null) colors = new List<Color>();
        colors.Clear();

        // Sabit renklerin (Önce bunları ekliyoruz)
        colors.Add(new Color(0.2235294f, 0.4862745f, 0.8666667f, 1)); // Mavi
        colors.Add(new Color(0.7830189f, 0.136659f, 0.136659f, 1));    // Kırmızı
        colors.Add(new Color(0.7568628f, 0.7450981f, 0.1921569f, 1));  // Sarı
        colors.Add(new Color(0.7960784f, 0.2156863f, 0.7803922f, 1));  // Pembe/Mor
        colors.Add(new Color(0.1680731f, 0.7843137f, 0.1372549f, 1));  // Yeşil
        colors.Add(new Color(0.4243051f, 0.05606978f, 0.5660378f, 1));
        colors.Add(new Color(0.9058824f, 0.4105133f, 0.04085084f, 1));
        colors.Add(new Color(0.4278213f, 0.4303301f, 0.4339623f, 1));
        colors.Add(new Color(0.1222447f, 0.1981132f, 0.002803484f, 1));
        colors.Add(new Color(0.02069241f, 0.05299112f, 0.8773585f, 1));

        // Eğer şişe sayısı sabit renk sayısından fazlaysa rastgele renk üret
        while (colors.Count < count)
        {
            colors.Add(new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1));
        }
    }

    private void CreateBlocks(int blockCount, int index)
    {
        int leftCount = 1, rightCount = 1;
        GameObject obj;
        for (int i = 0; i < blockCount; i++)
        {
            if (i == 0)
            {
                obj = Instantiate(bottle, bottleCreate[index].position, Quaternion.identity);
            }
            else if (i % 2 == 1)
            {
                obj = Instantiate(bottle, new Vector3(bottleCreate[index].position.x - leftCount * bottleSpace, bottleCreate[index].position.y, bottleCreate[index].position.z), Quaternion.identity);
                leftCount++;
            }
            else
            {
                obj = Instantiate(bottle, new Vector3(bottleCreate[index].position.x + rightCount * bottleSpace, bottleCreate[index].position.y, bottleCreate[index].position.z), Quaternion.identity);
                rightCount++;
            }

            int a = index >= 2 ? 0 : index;
            for (int j = 0; j < 4; j++)
            {
                obj.GetComponent<BottleController>().bottleColors[j] = activeLevel.bottles[i + a * firstBlock].colors[j];
            }
            obj.GetComponent<BottleController>().numberOfColorInBottle = activeLevel.bottles[i + a * firstBlock].numberBottle;
            obj.GetComponent<BottleController>().lineRenderer = GameObject.Find("LineRenderer").GetComponent<LineRenderer>();
            obj.transform.parent = gameObject.transform;

            obj = null;
        }
    }

    // Seviyedeki renkleri düzenle: her renk tam olarak 4 kez kullanılır.
    // İsteğe bağlı: farklı renk sayısını 4'ün katı yapmak için ek renk grupları eklenebilir.
    private void CreateColorByLevel(LevelCreateController lvl)
    {
        levelColors = new List<Color>();

        // Boş şişe sayısı (3 veya daha az şişe varsa 1 boş, daha fazlaysa 2 boş şişe bırak)
        int emptyCount = (lvl.bottles.Count <= 3) ? 1 : 2;
        int fullCount = Mathf.Max(1, lvl.bottles.Count - emptyCount);

        // Sadece ihtiyacımız olan kadar (fullCount) benzersiz renk hazırla
        CreateColors(fullCount);

        // Havuzu oluştur: Her benzersiz renkten tam 4 adet ekle
        for (int i = 0; i < fullCount; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                // colors[i] her zaman benzersizdir, döngü her renkten 4 tane ekler
                levelColors.Add(colors[i]);
            }
        }

        // Karışık bir şekilde şişelere dağıt
        for (int i = 0; i < fullCount; i++)
        {
            lvl.bottles[i].numberBottle = 4;
            
            // Liste boyutunu 4'e sabitle (Eskiden kalan 5. veya 6. renkleri tamamen temizler)
            if (lvl.bottles[i].colors == null) lvl.bottles[i].colors = new List<Color>();
            lvl.bottles[i].colors.Clear();

            for (int j = 0; j < 4; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, levelColors.Count);
                lvl.bottles[i].colors.Add(levelColors[randomIndex]);

                // Kullanılan rengi havuzdan çıkar
                levelColors.RemoveAt(randomIndex);
            }
        }

        // Boş şişeleri sıfırla (Önemli: ScriptableObject verisi kalıcı olabilir)
        for (int i = fullCount; i < lvl.bottles.Count; i++)
        {
            lvl.bottles[i].numberBottle = 0;
            if (lvl.bottles[i].colors == null) lvl.bottles[i].colors = new List<Color>();
            lvl.bottles[i].colors.Clear();
            for (int j = 0; j < 4; j++)
            {
                lvl.bottles[i].colors.Add(Color.clear);
            }
        }
    }
}
using HarmonyLib;
using ProLib.Extensions;
using ProLib.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProLib.UI
{
    public class LoadingScreen : MonoBehaviour
    {

        private Sprite BackgroundSprite;
        private TextMeshProUGUI Text;

        private float currentTime = Time.time;
        private int dots = 0;

        public void Awake()
        {
            this.BackgroundSprite = Assembly.GetExecutingAssembly().LoadSprite("Resources.background.png");
            GameObject canvas = new GameObject("Canvas");
            Canvas c = canvas.AddComponent<Canvas>();
            c.sortingLayerName = "UI Overlay";
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.scaleFactor = 3;


            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
            canvas.transform.SetParent(transform);


            GameObject background = new GameObject("Background");
            background.transform.SetParent(canvas.transform);
            background.transform.localScale = new Vector3(10, 10, 10);


            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = this.BackgroundSprite;
            backgroundImage.color = Color.black;


            GameObject TextObj = new GameObject("Text");
            Text = TextObj.AddComponent<TextMeshProUGUI>();
            Text.text = "Loading";
            Text.color = Color.white;
            TextObj.transform.SetParent(canvas.transform);
            TextObj.transform.localScale = new Vector3(1, 1, 1);
            TextObj.transform.localPosition = new Vector2(-100, -115);

            gameObject.HideAndDontSave();
        }

        public void Update()
        {
            float duration = Time.time - currentTime;

            if (duration > 1)
            {
                currentTime = Time.time;
                dots++;
                if (dots == 4) dots = 0;

                String text = "Loading";
                for (int i = 0; i < dots; i++)
                {
                    text += " .";
                }

                Text.text = text;
            }
        }
    }
}

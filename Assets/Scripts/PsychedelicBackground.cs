using UnityEngine;
using Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


public class PsychedelicBackground : MonoBehaviour
{
      public GameObject circlePrefab;
      public float spawnRate;
      public float expansionSpeed;
      public float fadeSpeed;
      
      [Header("Color Settings")]
      public Color[] player1Colors;
      public Color[] player2Colors;
      public Color[] player3Colors;
      public Color[] player4Colors;
      private Color[] currentColors;
      private int currentColorIndex = 0;
      private List<SpriteRenderer> activeRenderers = new List<SpriteRenderer>();
      
      private static PsychedelicBackground instance;
      
      //public Ball ball;
      
      /*private void OnEnable()
      {
         if (ball != null)
         {
            ball.OnPlayer1Hit += SwitchToPlayer1Colors;
            ball.OnPlayer2Hit += SwitchToPlayer2Colors;
         }
      }

      private void OnDisable()
      {
         if (ball != null)
         {
            ball.OnPlayer1Hit -= SwitchToPlayer1Colors;
            ball.OnPlayer2Hit -= SwitchToPlayer2Colors;
         }
      }*/
      
      private void OnEnable()
      {
         Ball.OnPlayerHit += HandleColorChange;
      }

      private void OnDisable()
      {
         Ball.OnPlayerHit -= HandleColorChange;
      }
      
      private void Awake()
      {
         if (instance == null)
            instance = this;
      }

      private void Start()
      {
         currentColors = player1Colors;
         // infinite loop coroutine
         StartCoroutine(SpawnLoop());
      }
      
      public static void TriggerGlobalColorChange(int playerId)
      {
         var instance = FindObjectOfType<PsychedelicBackground>();
         if (instance != null)
         {
            instance.HandleColorChange(playerId);
         }
      }
      private void HandleColorChange(int playerId)
      {
         switch (playerId)
         {
            case 1: currentColors = player1Colors; break;
            case 2: currentColors = player2Colors; break;
            case 3: currentColors = player3Colors; break;
            case 4: currentColors = player4Colors; break;
         }

         currentColorIndex = 0;
         UpdateActiveRenderersColors();
      }

      private IEnumerator SpawnLoop()
      {
         while (true)
         {
            SpawnCircle();
            yield return new WaitForSeconds(spawnRate);
         }
      }

      void SpawnCircle()
      {
         GameObject circle = Instantiate(circlePrefab, transform.position, Quaternion.identity, transform);
         circle.transform.localScale = Vector3.one;

         SpriteRenderer renderer = circle.GetComponent<SpriteRenderer>();

         // if randomnising player1Colors
         //renderer.color = player1Colors[Random.Range(0, player1Colors.Length)];

         // if appearing in order of array
         Color color = currentColors[currentColorIndex];
         color.a = 1f;
         renderer.color = color;
         currentColorIndex = (currentColorIndex + 1) % currentColors.Length;
         activeRenderers.Add(renderer); // Add renderer to list for color switching
         StartCoroutine(ExpandAndFade(circle, renderer));
      }

      IEnumerator ExpandAndFade(GameObject circle, SpriteRenderer renderer)
      {
         float alpha = 1f;
         Vector3 scale = circle.transform.localScale; 
         while (alpha > 0.01f)
         {
            scale += Vector3.one * expansionSpeed * Time.deltaTime;
            alpha -= fadeSpeed * Time.deltaTime;
            // Clamp
            alpha = Mathf.Clamp01(alpha);
            //scale = Vector3.Min(scale, Vector3.one * 100f);
            circle.transform.localScale = scale;
            Color fadeColor = renderer.color;
            fadeColor.a = alpha;
            renderer.color = fadeColor;
            if (alpha <= 0.01f) // prevent circle overlay
               break;

            yield return null;
         }
         activeRenderers.Remove(renderer);
         Destroy(circle);
      }
      
      // public void SwitchToPlayer1Colors()
      // {
      //    currentColors = player1Colors;
      //    currentColorIndex = 0;
      //    UpdateActiveRenderersColors();
      // }
      //
      // public void SwitchToPlayer2Colors()
      // {
      //    currentColors = player2Colors;
      //    currentColorIndex = 0;
      //    UpdateActiveRenderersColors();
      // }
      //
      // public void SwitchToPlayer3Colors()
      // {
      //    currentColors = player3Colors;
      //    currentColorIndex = 0;
      //    UpdateActiveRenderersColors();
      // }
      //
      // public void SwitchToPlayer4Colors()
      // {
      //    currentColors = player4Colors;
      //    currentColorIndex = 0;
      //    UpdateActiveRenderersColors();
      // }

      private void UpdateActiveRenderersColors()
      {
         for (int i = 0; i < activeRenderers.Count; i++)
         {
            if (activeRenderers[i] == null) continue;

            Color newColor = currentColors[i % currentColors.Length];
            newColor.a = activeRenderers[i].color.a; // keep current alpha
            activeRenderers[i].color = newColor;
         }
      }
   }



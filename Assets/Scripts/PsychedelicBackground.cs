using UnityEngine;
using Unity;
using System.Collections;


public class PsychedelicBackground : MonoBehaviour
{
   public GameObject circlePrefab; 
   public float spawnRate;
   public float expansionSpeed;
   public float fadeSpeed;
   public Color[] colors;
   
   private int currentColorIndex = 0;

   private void Start()
   {
      // infinite loop coroutine
      StartCoroutine(SpawnLoop());
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
      circle.transform.localScale = Vector3.zero;
      SpriteRenderer renderer = circle.GetComponent<SpriteRenderer>();
      
      // if randomnising colors
      //renderer.color = colors[Random.Range(0, colors.Length)];
      
      // if appearing in order of array
      Color color = colors[currentColorIndex];
      color.a = 1f;
      renderer.color = color;

      currentColorIndex = (currentColorIndex + 1) % colors.Length;

      StartCoroutine(ExpandAndFade(circle, renderer));
   }

   IEnumerator ExpandAndFade(GameObject circle, SpriteRenderer renderer)
   {
      float alpha = 1f;
      Vector3 scale = Vector3.zero;

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
      Destroy(circle);
   }
}

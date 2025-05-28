using UnityEngine;
using Unity;
using System.Collections;


public class PsychedelicBackground : MonoBehaviour
{
   public GameObject circlePrefab; 
   public float spawnRate = 0.2f;
   public float expansionSpeed = 1.5f;
   public float fadeSpeed = 0.5f;
   public Color[] colors;

   private float timer;

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
      renderer.color = colors[Random.Range(0, colors.Length)];

      StartCoroutine(ExpandAndFade(circle, renderer));
   }

   IEnumerator ExpandAndFade(GameObject circle, SpriteRenderer renderer)
   {
      float alpha = 1f;
      Vector3 scale = Vector3. zero;

      while (alpha > 0)
      {
         scale += Vector3.one * expansionSpeed * Time.deltaTime;
         alpha -= fadeSpeed * Time.deltaTime;

         circle.transform.localScale = scale;
         renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, alpha);
         yield return null;
      }
      Destroy(circle);
   }
}

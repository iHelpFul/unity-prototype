using UnityEngine;
using TMPro;
using System.Collections;

public class DamageNumberSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Canvas worldCanvas;

    private void OnEnable()
    {
        EventBus.Subscribe<DamageNumberEvent>(OnDamageNumber);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DamageNumberEvent>(OnDamageNumber);
    }

    private void OnDamageNumber(DamageNumberEvent e)
    {
        if (worldCanvas == null || damageTextPrefab == null)
            return;
            
        GameObject instance = Instantiate(damageTextPrefab, worldCanvas.transform);
        instance.transform.position = e.WorldPosition;

        TextMeshProUGUI text = instance.GetComponent<TextMeshProUGUI>();
        text.text = e.Damage.ToString();

        StartCoroutine(Animate(instance));
    }

    private IEnumerator Animate(GameObject obj)
    {
        float duration = 0.8f;
        float timer = 0f;

        Vector3 start = obj.transform.position;
        Vector3 end = start + Vector3.up * 1.25f;

        CanvasGroup group = obj.GetComponent<CanvasGroup>();

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            obj.transform.position = Vector3.Lerp(start, end, t);

            if (group != null)
                group.alpha = 1f - t;

            yield return null;
        }

        Destroy(obj);
    }
}

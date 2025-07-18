using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    public void MoveAlongPath(List<Vector2Int> path)
    {
        StartCoroutine(MoveAlongPathCor(path));
    }

    private IEnumerator MoveAlongPathCor(List<Vector2Int> path)
    {
        foreach (var step in path)
        {
            Vector3 target = new(step.x, step.y, 0);

            while ((transform.position - target).sqrMagnitude > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

                yield return null;
            }
        }

        print($"<color=cyan> Reached Goal !!!! </color>");
    }
}

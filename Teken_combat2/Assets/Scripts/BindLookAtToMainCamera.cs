using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

public class BindLookAtToMainCamera : MonoBehaviour
{
    IEnumerator Start()
    {
        var lookAt = GetComponent<LookAtConstraint>();
        if (!lookAt) yield break;

        while (!Camera.main) yield return null;

        var src = new ConstraintSource { sourceTransform = Camera.main.transform, weight = 1f };
        if (lookAt.sourceCount == 0) lookAt.AddSource(src);
        else lookAt.SetSource(0, src);

        lookAt.constraintActive = true;
        lookAt.enabled = true;
    }
}

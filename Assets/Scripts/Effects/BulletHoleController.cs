using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

//[RequireComponent(typeof(Rigidbody))]
public class BulletHoleController : MonoBehaviour
{
    // time it takes before decal starts fading
    public float timeBeforeFade = 2f;
    // time it takes to fade away
    public float fadeTime = 1;


    private float timeAlive = 0;
    private float originalFadeFactor = 1;
    private DecalProjector decalProjector;

    // Start is called before the first frame update
    void Start()
    {
        decalProjector = GetComponent<DecalProjector>();
        timeAlive = 0;
        originalFadeFactor = decalProjector.fadeFactor;
    }


    // Update is called once per frame
    void Update()
    {
        if (timeAlive > timeBeforeFade && timeAlive < fadeTime + timeBeforeFade)
        {
            decalProjector.fadeFactor = Mathf.Lerp(originalFadeFactor, 0, Mathf.Clamp01((timeAlive - timeBeforeFade)/fadeTime));
        }
        if(timeAlive > timeBeforeFade + fadeTime)
        {
            Destroy(this.gameObject);
        }
        timeAlive += Time.deltaTime;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeDices : MonoBehaviour
{
    public float shakeSpeed, shakeAmount, timeToShake;
    public float launchSpeed;

    public GameObject targetObject;
    public GameObject top;
    public List<GameObject> dices;

    private Vector3 initialPosition;    

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.position;
        foreach (var dice in dices)
            dice.transform.rotation = Random.rotation;
        
        StartCoroutine(ShakeAndLaunch());
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = new Vector3(initialPosition.x + Mathf.Sin(Time.time * speed) * amount, initialPosition.y + Mathf.Sin(Time.time * speed) * amount, transform.position.z);
    }

    IEnumerator ShakeAndLaunch()
    {
        var timer = 0f;

        while (timer < timeToShake)
        {
            var delta = Mathf.Sin(Time.time * shakeSpeed);
            transform.position = new Vector3(initialPosition.x + delta, initialPosition.y + delta, initialPosition.z + delta) * shakeAmount;
            timer += Time.deltaTime;
            yield return null;
        }

        top.SetActive(false);
        timer = 0;
        var targetDirection = targetObject.transform.position - transform.position;
        var target = Quaternion.LookRotation(transform.forward, targetDirection);

        foreach (var dice in dices)
            dice.GetComponent<DiceTest>().Initialize(targetDirection);

        while(timer <= 1)
        {
            yield return null;
            timer += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(transform.rotation, target, Mathf.Clamp01(timer * launchSpeed));
            foreach (var dice in dices)
                dice.GetComponent<DiceTest>().AddForce();
            
        }

    }

}

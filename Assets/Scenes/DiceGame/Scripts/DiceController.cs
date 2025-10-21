using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using System.Collections;
using UnityEngine;

public class DiceController : MonoBehaviour
{

    public GameObject diceObject;

    private Vector3 direction, rotation;
    //private float accelerationForce;

    private void Start()
    {
#if !UNITY_EDITOR
        CloudAnchorsController.instance.SessionOrigin.MakeContentAppearAt(transform, LaunchDice.instance.Turricola.GetNextSpawnPosition());
#endif

        if (!CloudAnchorsController.instance.IsHost())
            LaunchDice.instance.Turricola.AddDice(this);
    }

    public void Activate()
    {
        diceObject.SetActive(true);
        //GetComponent<Rigidbody>().isKinematic = false;
    }

    public void DiceLaunched()
    {
        StartCoroutine(WaitUntilMoving());
    }

    private IEnumerator WaitUntilMoving()
    {
#if false
        var oldPosition = transform.position;
        var oldRotation = transform.rotation;

        yield return new WaitForFixedUpdate();

        while (oldPosition != transform.position || oldRotation != transform.rotation)
        {
            //Debug.Log("still moving");
            oldPosition = transform.position;
            oldRotation = transform.rotation;
            yield return new WaitForFixedUpdate();

            /*if (!GetComponentInChildren<Renderer>().isVisible)
            {
                Debug.Log("destroying");
                LaunchDice.instance.DiceValue(-1);
                Destroy(gameObject);
            }*/
        }
#endif
        
        
        
        yield return new WaitForSeconds(3.5f);
        
        LaunchDice.instance.DiceValue(GetValue());
        //GetComponent<Rigidbody>().isKinematic = true;
        transform.SetParent(CloudAnchorsController.instance.Anchor.transform);
        //transform.SetParent(diceLauncher.GetTableObject().transform);

    }

    private int GetValue()
    {
        var sides = transform.GetComponentsInChildren<DiceSide>();
        var higherSide = sides[0];

        foreach (var side in sides)
        {
            if (side.transform.position.y > higherSide.transform.position.y)
                higherSide = side;
        }

        return higherSide.value;
    }
}

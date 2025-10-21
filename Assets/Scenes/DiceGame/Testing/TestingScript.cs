using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingScript : MonoBehaviour
{
    public void SimulateResolvedAnchor()
    {
        GetComponent<CloudAnchorsController>().OnAnchorResolved(true, "anchor resolved");
    }
}

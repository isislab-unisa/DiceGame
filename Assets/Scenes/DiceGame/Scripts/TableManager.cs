using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableManager : MonoBehaviour
{

    public void Start()
    {
        CloudAnchorsController.instance.Anchor = gameObject;
        LocalPlayerController.localPlayer.CmdAnchorResolved();
    }

}

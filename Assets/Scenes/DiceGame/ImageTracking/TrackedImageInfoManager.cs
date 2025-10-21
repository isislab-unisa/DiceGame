using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// This component listens for images detected by the <c>XRImageTrackingSubsystem</c>
/// and overlays some information as well as the source Texture2D on top of the
/// detected image.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
[RequireComponent(typeof(ARSessionOrigin))]
public class TrackedImageInfoManager : MonoBehaviour
{
    private GameObject anchor;

    ARTrackedImageManager m_TrackedImageManager;

    void Awake()
    {
        m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        m_TrackedImageManager.enabled = true;
    }

    void OnDisable()
    {
        m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }


    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            SpawnAnchor(trackedImage.transform.position);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            if (anchor == null)
                SpawnAnchor(trackedImage.transform.position);

            if(trackedImage.GetComponent<Renderer>().isVisible)
                anchor.transform.position = trackedImage.transform.position;
        }

    }

    private void SpawnAnchor(Vector3 position)
    {
        anchor = Instantiate(CloudAnchorsController.instance.anchorPrefab, position, Quaternion.identity);
        MessageHandler.instance.ShowMessage("In attesa dell'altro giocatore");
    }
}

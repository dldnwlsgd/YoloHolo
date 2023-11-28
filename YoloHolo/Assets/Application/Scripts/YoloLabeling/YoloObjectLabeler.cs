using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RealityCollective.ServiceFramework.Services;
using Unity.VisualScripting;
using UnityEngine;
using YoloHolo.Services;
using YoloHolo.Utilities;

namespace YoloHolo.YoloLabeling
{
    public class YoloObjectLabeler : MonoBehaviour
    {
        [SerializeField]
        private GameObject labelObject;

        [SerializeField]
        private int cameraFPS = 4;

        [SerializeField]
        private Vector2Int requestedCameraSize = new(896, 504);

        private Vector2Int actualCameraSize;

        [SerializeField]
        private Vector2Int yoloImageSize = new(320, 256);

        [SerializeField]
        private float virtualProjectionPlaneWidth = 1.356f;

        [SerializeField]
        private float minIdenticalLabelDistance = 0.3f;

        [SerializeField]
        private float labelNotSeenTimeOut = 5f;

        [SerializeField]
        private Renderer debugRenderer;

        private WebCamTexture webCamTexture;

        private IYoloProcessor yoloProcessor;

        private readonly List<YoloGameObject> yoloGameObjects = new();


        private CheckingBody checkingBody;

        Vector3?[] assignedPosition = new Vector3?[3] {null, null, null };
        bool isPointassigned = true;
        bool ischeckingMethod = false;


        private void Start()
        {
            yoloProcessor = ServiceManager.Instance.GetService<IYoloProcessor>();
            webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
            webCamTexture.Play();
            StartRecognizingAsync();
        }

        private async Task StartRecognizingAsync()
        {
            await Task.Delay(1000);

            actualCameraSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
            var renderTexture = new RenderTexture(yoloImageSize.x, yoloImageSize.y, 24); 
            if (debugRenderer != null && debugRenderer.gameObject.activeInHierarchy)
            {
                debugRenderer.material.mainTexture = renderTexture;
            }

            while (true)
            {
                var cameraTransform = Camera.main.CopyCameraTransForm();
                Graphics.Blit(webCamTexture, renderTexture);
                await Task.Delay(32);

                var texture = renderTexture.ToTexture2D();
                await Task.Delay(32);

                var foundObjects = await yoloProcessor.RecognizeObjects(texture);

                ShowRecognitions(foundObjects, cameraTransform);
                Destroy(texture);
                Destroy(cameraTransform.gameObject);

                if (ischeckingMethod) { IWantToCheck();  }
            }
        }


        private void ShowRecognitions(List<YoloItem> recognitions, Transform cameraTransform)
        {
            foreach (var recognition in recognitions)
            {
                var newObj = new YoloGameObject(recognition, cameraTransform,
                    actualCameraSize, yoloImageSize, virtualProjectionPlaneWidth);
                if (newObj.PositionInSpace != null && !HasBeenSeenBefore(newObj))
                {
                    yoloGameObjects.Add(newObj);

                    if(newObj.Name == "elephant")
                    {
                        AssignPointsForBodyModel(0, newObj);
                    } else if (newObj.Name == "cow")
                    {
                        AssignPointsForBodyModel(1, newObj);
                    } else if(newObj.Name == "zebra")
                    {
                        AssignPointsForBodyModel(2, newObj);
                    }
                }
            }

            for (var i = yoloGameObjects.Count - 1; i >= 0; i--)
            {
                if (Time.time - yoloGameObjects[i].TimeLastSeen > labelNotSeenTimeOut)
                {
                    Destroy(yoloGameObjects[i].DisplayObject);
                    yoloGameObjects.RemoveAt(i);
                }
            }
        }

        private void AssignPointsForBodyModel(int i, YoloGameObject newObj)
        {
            
            assignedPosition[i]  = newObj.PositionInSpace;
            Debug.Log($"num : {i}, {assignedPosition[i].Value}");
            

            newObj.DisplayObject = Instantiate(labelObject, newObj.PositionInSpace.Value, Quaternion.identity);
            newObj.DisplayObject.transform.parent = transform;
            var labelController = newObj.DisplayObject.GetComponent<ObjectLabelController>();
            labelController.SetText(newObj.Name, assignedPosition[i].Value);
            if (isPointassigned) { CheckPointState(); }
            
            //i = calculateBodyTransform.AssignPosition(i, assignedPosition[i].Value);
        }

        private bool HasBeenSeenBefore(YoloGameObject obj)
        {
            var seenBefore = yoloGameObjects.FirstOrDefault(
                ylo => ylo.Name == obj.Name &&
                Vector3.Distance(obj.PositionInSpace.Value,
                    ylo.PositionInSpace.Value) < minIdenticalLabelDistance);
            if (seenBefore != null)
            {
                seenBefore.TimeLastSeen = Time.time;
            }
            return seenBefore != null;
        }

        public void CheckPointState()
        {
            if (assignedPosition[0] != null && assignedPosition[1] != null && assignedPosition[2] != null)
            {
                Debug.Log("Position locate all !!");

                isPointassigned = false;
                ischeckingMethod = true;
                //isPosition = true;
            }
            else
            {
                Debug.Log("Position no locate all");
            }
        }



        public void IWantToCheck()
        {
            Debug.Log("adfasdfasdf");
        }
    }
}

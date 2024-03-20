using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Communication : MonoBehaviour
{

    public struct Frame{
        public Texture2D frameTexture;
        public float capturedTime;
        public void Capture(RenderTexture renderTexture){
            if(frameTexture == null)
                frameTexture = new Texture2D( renderTexture.width, renderTexture.height );
            RenderTexture.active = renderTexture;
            frameTexture.ReadPixels(new Rect( 0, 0, renderTexture.width, renderTexture.height ), 0, 0);
            frameTexture.Apply();

            capturedTime = Time.time;
        }
    }

    public struct CollisionData{
        public float[] distances;

        public int collisionCount;

        public bool collided;
        public int GetShortestDistanceIndex(){
            float minDist = float.MaxValue;
            int indx = 0;
            for(int i = 0; i < distances.Length; i++){
                if(distances[i] < minDist){
                    minDist = distances[i];
                    indx = i;
                }
            }
            return indx;
        }

    }





    public static CollisionData collisionData;


    [SerializeField] float inputLatency;
    [SerializeField] float signalReceivingLatency;
    [SerializeField] float cameraLatency;
    [SerializeField] Camera FPVCamera;
    RenderTexture renderTexture;
    [SerializeField] RenderTexture destRT;

    int bufferSize = 16;
    Frame[] storedFrames;
    public static bool cameraImageReceived = true;

    int currentFrameIndex = 0;
    int renderedFrameBufferIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        
        if (cameraLatency > 0f)
        {
            storedFrames = new Frame[bufferSize];
            renderTexture = new RenderTexture(960, 540, 16);
            FPVCamera.targetTexture = renderTexture;
            StartCoroutine(LaggedTransferCameraImages());
        } else
        {
            FPVCamera.targetTexture = destRT;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        //Debug.Log(RenderTexture.active);
    }

    IEnumerator LaggedTransferInputCommand(){
        yield return new WaitForEndOfFrame();
    }
    IEnumerator LaggedTransferCameraCommand(){
        yield return new WaitForEndOfFrame();
    }
    IEnumerator LaggedTransferCameraImages(){   
        while(true){
            yield return new WaitForEndOfFrame();
            if(cameraImageReceived){
                storedFrames[currentFrameIndex % bufferSize].Capture(renderTexture);
                for ( ; storedFrames[renderedFrameBufferIndex].capturedTime < ( Time.time - cameraLatency ) ; renderedFrameBufferIndex = ( renderedFrameBufferIndex + 1 ) % bufferSize ) ;
                Graphics.Blit(storedFrames[renderedFrameBufferIndex].frameTexture, destRT);
                currentFrameIndex++;
                cameraImageReceived = false;
            }
        }
    }

}

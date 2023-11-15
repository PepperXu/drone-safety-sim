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

    [SerializeField] float inputLatency;
    [SerializeField] float signalReceivingLatency;
    [SerializeField] float cameraLatency;
    [SerializeField] Camera FPVCamera;
    RenderTexture renderTexture;
    [SerializeField] RenderTexture destRT;

    int bufferSize = 1;
    Frame[] storedFrames;
    public static bool cameraImageReceived = true;

    int currentFrameIndex = 0;
    int renderedFrameBufferIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        storedFrames = new Frame[bufferSize];
        renderTexture = new RenderTexture( 1920, 1080, 24 );
        FPVCamera.targetTexture = renderTexture;
        StartCoroutine(LaggedTransferCameraImages());
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

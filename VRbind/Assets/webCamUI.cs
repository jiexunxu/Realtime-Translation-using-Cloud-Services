using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

 using System.Text;
 using System.IO; 

public class webCamUI : MonoBehaviour {

     public RawImage rawimage;
     private string text;
     public Text subtitlesText;
     void Start () 
     {
     	subtitlesText = subtitlesText.GetComponent<Text>();

         WebCamTexture webcamTexture = new WebCamTexture();
         rawimage.texture = webcamTexture;
         rawimage.material.mainTexture = webcamTexture;
         webcamTexture.Play();

         
     }

     void Update()
     {
     	text = System.IO.File.ReadAllText("Assets/Resources/subtitles.txt");
        Debug.Log(text);
     	
     	if(text[0].Equals('*')){
     		subtitlesText.text = text;
     		WindowsVoice.theVoice.speak(text);

     		}
     	else{
     			Debug.Log("Passed");
     		}
     }

}

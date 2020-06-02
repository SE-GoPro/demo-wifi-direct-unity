using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : WifiDirectBase {
    public GameObject canvas;
    public GameObject buttonList;
    public GameObject addrButton;
    public GameObject addrPanel;
    public GameObject cube;
    public GameObject colorPanel;
    public GameObject red;
    public GameObject green;
    public GameObject blue;
    public GameObject send;
	// Adds listeners to the color sliders and calls the initialize script on the library
	void Start () {
        colorPanel.SetActive(false);
        addrPanel.SetActive(false);
        cube.SetActive(false);
        canvas.SetActive(false);
        red.GetComponent<Slider>().onValueChanged.AddListener((blah) => {
            this.UpdateCube(this.GetSliderColors());
        });
        green.GetComponent<Slider>().onValueChanged.AddListener((blah) => {
            this.UpdateCube(this.GetSliderColors());
        });
        blue.GetComponent<Slider>().onValueChanged.AddListener((blah) => {
            this.UpdateCube(this.GetSliderColors());
        });
        send.GetComponent<Button>().onClick.AddListener(() => {
            base.PublishMessage("#" + ColorUtility.ToHtmlStringRGB(this.GetSliderColors())); //when send button is clicked, send the new rgb color
        });
        base.Initialize(this.gameObject.name);
    }
	//when the WifiDirect services is connected to the phone, begin broadcasting and discovering services
    public override void OnServiceConnected() {
        canvas.SetActive(true);
        addrPanel.SetActive(true);
        Dictionary<string, string> record = new Dictionary<string, string> {
            { "player", "unity" }
        };
        base.BroadcastService("hi", record);
        base.DiscoverServices();
    }
	//On finding a service, create a button with that service's address
    public override void OnServiceFound(string addr) {
        GameObject newButton = Instantiate(addrButton);
        newButton.GetComponentInChildren<Text>().text = addr;
        newButton.transform.SetParent(buttonList.transform, false);
        newButton.GetComponent<Button>().onClick.AddListener(() => {
            this.MakeConnection(addr);
        });
    }

    //When the button is clicked, connect to the service at its address
    private void MakeConnection(string addr) {
        base.ConnectToService(addr);
    }
	//When connected, begin rendering the cube
    public override void OnConnect() {
        addrPanel.SetActive(false);
        cube.SetActive(true);
        colorPanel.SetActive(true);
    }
	//Turns the slider values into a Color
    private Color GetSliderColors () {
        return new Color(red.GetComponent<Slider>().value, green.GetComponent<Slider>().value, blue.GetComponent<Slider>().value);
    }
	//Updates the color of the cube
    private void UpdateCube(Color c) {
        cube.GetComponent<Renderer>().material.color = c;
    }
	//When recieving a new message, parse the color and set it to the cube
    public override void OnReceiveMessage(string message) {
        Color c = new Color(0,0,0);
        ColorUtility.TryParseHtmlString(message, out c);
        this.UpdateCube(c);
    }
	//Kill Switch
    public override void OnServiceDisconnected() {
        base.Terminate();
        Application.Quit();
    }
	//Kill Switch
    public void OnApplicationPause(bool pause) {
        if(pause) {
            this.OnServiceDisconnected();
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System;
#if UNITY_ANDROID
/// <summary>
/// The base class of the library
/// </summary>
/// <remarks>
/// Either use this class, or a class that derives from this to have all the powers of the library
/// </remarks>
public class WifiDirectBase : MonoBehaviour {
	private static AndroidJavaObject _wifiDirect = null;
	/// <summary>
	/// Intializes the library, should only be called once.
	/// </summary>
	/// <param name="gameObjectName">
	/// Name of the GameObject that will control the Wifi Direct and recieve all the events
	/// </param> 
	public void Initialize (string gameObjectName) {
		if (_wifiDirect == null) {
			_wifiDirect = new AndroidJavaObject ("dev.gopros.se.unitywifidirect.UnityWifiDirect");
			_wifiDirect.CallStatic ("initialize", gameObjectName);
		}
        // After successfully initialize service, your gameObject is able to broastcast this service.
	}
	/// <summary>
	/// Terminates the library (use to gracefully exit)
	/// </summary> 
	public void Terminate () {
		if (_wifiDirect != null) {
			_wifiDirect.CallStatic ("terminate");
		}
	}
	/// <summary>
	/// Broadcasts a service for other devices to discover.
	/// </summary>
	/// <param name="service">
	/// The name that will be on the broadcasted service
	/// </param>
	/// <param name="record">
	/// The key value pairs to send along with the service
	/// </param>
	public void BroadcastService(string service, Dictionary<string, string> record) {
		using(AndroidJavaObject hashMap = new AndroidJavaObject("java.util.HashMap"))
		{
			foreach(KeyValuePair<string, string> kvp in record)
			{
				hashMap.Call<string> ("put", kvp.Key, kvp.Value);
			}
			_wifiDirect.CallStatic ("broadcastService", service, hashMap);
		}
	}
	/// <summary>
	/// Search for services (no timeout)
	/// </summary>
	public void DiscoverServices () {
		_wifiDirect.CallStatic ("discoverServices");
	}
	/// <summary>
	/// Stops searching for services
	/// </summary>
	public void StopDiscovering () {
		_wifiDirect.CallStatic ("stopDiscovering");
	}
	/// <summary>
	/// Connects to a service
	/// </summary>
	/// <param name="addr">
	/// The address to connect to
	/// </param>
	public void ConnectToService (string addr) {
		_wifiDirect.CallStatic ("connectToService", addr);
	}
	/// <summary>
	/// Sends a message to the connected device
	/// </summary>
	/// <param name="msg">
	/// The message string to send
	/// </param>
	public void PublishMessage (string msg) {
		_wifiDirect.CallStatic ("sendMessage", msg);
	}

	/// <summary>
	/// Returns if the library is ready
	/// </summary>
	/// <remarks>
	/// set to true by onServiceConnected() and set to false by terminate()
	/// </remarks>
	/// <returns>
	/// a bool stating if the library is ready
	/// </returns>
	public bool IsReady () {
		return _wifiDirect.GetStatic<bool> ("wifiDirectHandlerBound");
	}
	//events
	/// <summary>
	/// Called when the library is ready
	/// </summary>
	public virtual void OnServiceConnected () {
		Debug.Log ("service is legit");
	}
	/// <summary>
	/// Called when the library's backend is shutdown
	/// </summary>
	public virtual void OnServiceDisconnected () {
		Debug.Log ("service failed");
	}
	/// <summary>
	/// Called when a service without text records has been found
	/// </summary>
	/// <param name="addr">
	/// the address of the service
	/// </param>
	public virtual void OnServiceFound (string addr) {

	}
	/// <summary>
	/// Called when a service with text records has been found (includes deserializer)
	/// </summary>
	/// <remarks>
	/// Don't override this, override the onTxtRecord() method because the deserializer is necessary
	/// </remarks>
	/// <param name="stringifyRecord">
	/// The deserialized text reocrds
	/// </param>
	public void OnReceiveStringifyRecord (string stringifyRecord) {
        string[] addressWithRecords = stringifyRecord.Split('?');
        string addrEncoded = addressWithRecords[0];
        string[] records = addressWithRecords[1].Split('&');

		string addr = Encoding.Unicode.GetString(Convert.FromBase64String(addrEncoded));
		Dictionary<string, string> recordDict = new Dictionary<string, string> ();
        foreach(string record in records) {
            string[] kav = record.Split('_');
            string key = Encoding.Unicode.GetString(Convert.FromBase64String(kav[0]));
            string value = Encoding.Unicode.GetString(Convert.FromBase64String(kav[1]));

            recordDict.Add(key, value);
        }

		Debug.Log("stringify record found");
		this.OnTxtRecord (addr, recordDict);
	}
	/// <summary>
	/// Called when a service with text record is found (deserialized already)
	/// </summary>
	/// <param name="addr">
	/// The address of the service
	/// </param>
	/// <param name="record">
	/// The key value pairs of the text record
	/// </param>
	public virtual void OnTxtRecord(string addr, Dictionary<string, string> record) {
		
	}
	/// <summary>
	/// Called when connected to a client
	/// </summary>
	public virtual void OnConnect () {
		
	}
	/// <summary>
	/// Called when the other device has sent a message.
	/// </summary>
	/// <param name="message">
	/// The message sent
	/// </param>
	public virtual void OnReceiveMessage (string message) {
    
	}

    public void Send(Dictionary<string, string> dict) {
        string stringifyDict = "";
        foreach(KeyValuePair<string, string> pair in dict) {
            stringifyDict += pair.Key + "=" + pair.Value + "&";
        }
        this.PublishMessage(stringifyDict.TrimEnd('&'));
    }

    public Dictionary<string, string> OnReceive(string strDict) {
        Dictionary<string, string> result = new Dictionary<string, string>();
        string[] pairs = strDict.Split('&');
        foreach(string pair in pairs) {
            string[] kav = pair.Split('=');
            result.Add(kav[0], kav[1]);
        }
        return result;
    }
}
#endif

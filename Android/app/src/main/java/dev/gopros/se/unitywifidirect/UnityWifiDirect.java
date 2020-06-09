package dev.gopros.se.unitywifidirect;

/*
  References: Jefferson2000@gmail.com
  Modified by DuyPV
 */
import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.ServiceConnection;
import android.os.IBinder;
import android.support.v4.content.LocalBroadcastManager;

import android.util.Log;
import android.util.Base64;

import com.unity3d.player.*;

import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;

import edu.rit.se.wifibuddy.WifiDirectHandler;

public class UnityWifiDirect {
    @SuppressLint("StaticFieldLeak")
    public static Activity unityActivity;
    public static String TAG = "UnityWifiDirect";
    public static boolean wifiDirectHandlerBound = false;
    public static WifiDirectHandler wifiDirectHandler;
    public static String gameObject = "UnityWifiDirect";
    private UnityWifiDirect () {
    }
    private static void initialize() {
        Log.i(TAG, "initializing");
        unityActivity = UnityPlayer.currentActivity;

        //register broadcast receiver
        IntentFilter filter = new IntentFilter();
        filter.addAction(WifiDirectHandler.Action.SERVICE_CONNECTED);
        filter.addAction(WifiDirectHandler.Action.MESSAGE_RECEIVED);
        filter.addAction(WifiDirectHandler.Action.DEVICE_CHANGED);
        filter.addAction(WifiDirectHandler.Action.WIFI_STATE_CHANGED);
        filter.addAction(WifiDirectHandler.Action.DNS_SD_TXT_RECORD_AVAILABLE);
        filter.addAction(WifiDirectHandler.Action.DNS_SD_SERVICE_AVAILABLE);
        LocalBroadcastManager.getInstance(unityActivity).registerReceiver(broadcastReceiver, filter);

        //bind service
        Intent intent = new Intent(unityActivity, WifiDirectHandler.class);
        boolean bound = unityActivity.bindService(intent, wifiServiceConnection, Context.BIND_AUTO_CREATE);
        Log.i(TAG, "bound: " + bound);
    }
    public static void initialize(String go) {
        initialize();
        gameObject = go;
    }
    public static void terminate () {
        if(wifiDirectHandlerBound) {
            unityActivity.unbindService(wifiServiceConnection);
            wifiDirectHandlerBound = false;
        }
    }
    public static void broadcastService(String serviceName, HashMap<String, String> records) {
        Log.i(TAG, "broadcasting service: " + serviceName);
        wifiDirectHandler.addLocalService(serviceName, records);
    }
    public static void discoverServices () {
        wifiDirectHandler.continuouslyDiscoverServices();
        Log.i(TAG, "discovering services");
    }
    public static void stopDiscovering () {
        wifiDirectHandler.stopServiceDiscovery();
        Log.i(TAG, "discovery stopped");
    }
    public static void connectToService (String address) {
        wifiDirectHandler.initiateConnectToService(Objects.requireNonNull(wifiDirectHandler.getDnsSdServiceMap().get(address)));
        Log.i(TAG, "initiating connection to " + address);
    }
    public static void sendMessage (String msg) {
        try {
            wifiDirectHandler.getCommunicationManager().write(msg.getBytes(StandardCharsets.UTF_16));
        }
        catch (Exception ignored) {
        }
    }

    public static String getDeviceAddress() {
        return wifiDirectHandler.getThisDevice().deviceAddress;
    }

    //anonymous classes
    private static ServiceConnection wifiServiceConnection = new ServiceConnection() {

        @Override
        public void onServiceConnected(ComponentName name, IBinder service) {
            WifiDirectHandler.WifiTesterBinder binder = (WifiDirectHandler.WifiTesterBinder) service;

            wifiDirectHandler = binder.getService();
            wifiDirectHandlerBound = true;
            Log.i(TAG, "WifiDirectHandler service bound");

            UnityPlayer.UnitySendMessage(gameObject, "OnServiceConnected","");
        }

        @Override
        public void onServiceDisconnected(ComponentName name) {
            wifiDirectHandlerBound = false;
            Log.i(TAG, "WifiDirectHandler service unbound");
            UnityPlayer.UnitySendMessage(gameObject, "OnServiceDisconnected","");
        }
    };
    private static BroadcastReceiver broadcastReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive (Context context, Intent intent) {
            switch(Objects.requireNonNull(intent.getAction())) {
                case WifiDirectHandler.Action.DNS_SD_SERVICE_AVAILABLE:
                    String serviceAddress = intent.getStringExtra(WifiDirectHandler.SERVICE_MAP_KEY);
                    Log.i(TAG, "device found @ address " + serviceAddress);
                    UnityPlayer.UnitySendMessage(gameObject, "OnServiceFound", serviceAddress);
                    break;
                case WifiDirectHandler.Action.DNS_SD_TXT_RECORD_AVAILABLE:
                    String txtAddress = intent.getStringExtra(WifiDirectHandler.TXT_MAP_KEY);
                    Map<String, String> recordMap = wifiDirectHandler.getDnsSdTxtRecordMap().get(txtAddress).getRecord();
                    StringBuilder encoded = new StringBuilder();
                    encoded.append(txtAddress).append("_");
                    for(Map.Entry<String, String> entry : recordMap.entrySet()) {
                        encoded.append(entry.getKey()).append("?");
                        encoded.append(entry.getValue()).append("_");
                    }
                    String result = encoded.toString();
                    String formatted = result.substring(0, result.lastIndexOf('_'));
                    Log.i(TAG, "device found with text record, formatted string: " + formatted);
                    UnityPlayer.UnitySendMessage(gameObject, "OnReceiveStringifyRecord", formatted);
                    break;
                case WifiDirectHandler.Action.SERVICE_CONNECTED:
                    Log.i(TAG, "Connection made!");
                    UnityPlayer.UnitySendMessage(gameObject, "OnConnect", "");
                    break;
                case WifiDirectHandler.Action.MESSAGE_RECEIVED:
                    try {
                        String msg = new String(Objects.requireNonNull(intent.getByteArrayExtra(WifiDirectHandler.MESSAGE_KEY)), StandardCharsets.UTF_16);
                        Log.i(TAG, "Message received: "+msg);
                        UnityPlayer.UnitySendMessage(gameObject, "OnReceiveMessage", msg);
                    } catch (Exception ignored) {}
                    break;
                default:
                    break;
            }
        }
    };
}

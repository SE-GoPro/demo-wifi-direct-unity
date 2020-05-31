# `WifiDirectBase.cs`

## 1. Inheritance

```cs
public class WifiDirectBase : MonoBehavior {
    ...
}
```

Như thông thường, đây là 1 class cho phép 1 đối tượng game trong Unity kiểm soát cũng như điều khiển kết nối Wi-Fi P2P.

## 2. Property: `_wifiDirect`

```cs
    private static AndroidJavaObject _wifiDirect = null;
```

Thuộc tính duy nhất trong class `WifiDirectBase`, khởi tạo thư viện được xây dựng từ project **WifiBuddy** (Build module trong Android ra file `.aar` và thêm vào project Unity).

## 3. Methods

### 3.1. `Initialize(string gameObjectName)`

```cs
    public const string WIFI_DIRECT_CLASS_NAME = "<>"; // ví dụ: "dev.gopro.se.UnityWifiDirect"

    /// <summary>
    /// Khởi tạo thư viện, chỉ nên gọi 1 lần
    /// </summary>
    /// <param name="gameObjectName">
    /// Tên của đối tượng game điều khiển Wifi Direct và tiếp nhận tất cả các sự kiện.
    /// </param>
    public void Initialize(string gameObjectName) {
        if (_wifiDirect == null) {
            _wifiDirect = new AndroidJavaObject()
            _wifiDirect.CallStatic("initialize", gameObjectName);
        }
    }
```

Hàm này gọi đến phương thức tĩnh `initialize()` với tham số `gameObjectName` của thư viện, nằm trong lớp `UnityWifiDirect.UnityWifiDirect` nhằm khởi tạo dịch vụ Wi-Fi Direct.

```java
    public static Activity unityActivity;
    public static boolean wifiDirectHandlerBound = false;
    public static WifiDirectHandler wifiDirectHandler;
    public static String gameObject = "UnityWifiDirect";

    // Constructor
...
    // Định nghĩa đối tượng của lớp dịch vụ kết nối
    private static ServiceConnection wifiServiceConnection = new ServiceConnection() {

        @Override
        public void onServiceConnected(ComponentName name, IBinder service) {
            WifiDirectHandler.WifiTesterBinder binder = (WifiDirectHandler.WifiTesterBinder) service;

            wifiDirectHandler = binder.getService();
            wifiDirectHandlerBound = true;

            UnityPlayer.UnitySendMessage(gameObject, "onServiceConnected", "");
        }

        @Override public void onServiceDisconnected(ComponentName name) {
            wifiDirectHandlerBound = false;

            UnityPlayer.UnitySendMessage(gameObject, "onServiceDisconnected", "");
        }
    }

    // Init


    public static void initialize() {
        unityActivity = UnityPlayer.currentActivity;

        // Đăng ký broadcast receiver
        // --- Xây dựng Intent filter
        // ------ Tất cả các action đơn giản là các hằng chuỗi.
        IntentFilter filter = new IntentFilter();
        filter.addAction(WifiDirectHandler.Action.SERVICE_CONNECTED);
        filter.addAction(WifiDirectHandler.Action.MESSAGE_RECEIVED);
        filter.addAction(WifiDirectHandler.Action.DEVICE_CHANGED);
        filter.addAction(WifiDirectHandler.Action.WIFI_STATE_CHANGED);
        filter.addAction(WifiDirectHandler.Action.DNS_SD_TXT_RECORD_AVAILABLE);
        filter.addAction(WifiDirectHandler.Action.DNS_SD_SERVICE_AVAILABLE);
        // --- Đăng ký local broadcast receiver
        LocalBroadcastManager
            .getInstance(unityActivity)
            .registerReceiver(broadcastReceiver, filter);

        // Bind service
        Intent intent = new Intent(unityActivity, WifiDirectHandler.class);
        boolean isBound = unityActivity
            .bindService(intent, wifiServiceConnection, Context.BIND_AUTO_CREATE);
    }

    // Hàm khởi tạo ban đầu với game object
    public static void initialize(String gameObject) {
        initialize();
        this.gameObject = gameObject;
    }
```

### 3.2. `Terminate()`

```cs
    public void Terminate() {
        if (_wifiDirect != null) {
            _wifiDirect.CallStatic("terminate");
        }
    }
```

Hàm này hủy dịch vụ bằng cách gọi đến phương thức `terminate()` trong thư viện

```java
    public static void terminate() {
        if (wifiDirectHandlerBound) {
            unityActivity.unbindService(wifiServiceConnection);
            wifiDirectHandlerBound = false;
        }
    }

```

### 3.3. `BroadcastService(string service, Dictionary<string, string> record)`

```cs
    /// <summary>
    /// Broadcast 1 dịch bụ để các thiết bị khác có thể discover.
    /// </summary>
    /// <param name="service">
    /// Tên mà đưa lên broadcast service
    /// </param>
    /// <param name="record">
    /// Các cặp key, value gửi lên cùng dịch vụ
    /// </param>
    public void BroadcastService(string service, Dictionary<string, string> record) {
        using(AndroidJavaObject hashMap = new AndroidJavaObject("java.util.HashMap")) {
            foreach(KeyValuePair<string, string> kvp in record) {
                hashMap.Call<string> ("put", kvp.Key, kvp.Value);
            }
        _wifiDirect.CallStatic ("broadcastService", service, hashMap);
        }
    }
```

Phương thức tĩnh `broadcastService()` với tham số `service` và `hashMap` được triển khai trong thư viện như sau:

```java
    public static void broadcastService(String serviceName, HashMap<String, String> records) {
        wifiDirectHandler.addLocalService(serviceName, records);
    }
```

Phương thức `addLocalService()` của đối tượng lớp `WifiDirectHandler` thực hiện các công việc sau:

- Thêm thông tin dịch vụ:

  ```java
    ...
    wifiP2pServiceInfo = WifiP2pServiceInfo.newInstance(
        serviceName,
        ServiceType.PRESENCE_TCP.toString(), // "_presence._tcp"
        record
    );
  ```

- Thêm dịch vụ vào local service:

  ```java
    // Chỉ thêm 1 local service nếu làm sạch local serivces đã có sẵn.
    wifiP2pManager.clearLocalServices(channel, new WifiP2pManager.ActionListener() {

        @Override
        public void onSuccess() {
            // Thêm 1 local service
            wifiP2pManager.addLocalService(channel, wifiP2pServiceInfo, new WifiP2pManager.ActionListener(){
                @Override
                public void onSuccess() {
                }

                @Override
                public void onFailure(int reason) {
                    wifiP2pServiceInfo = null;
                }
            });
        }

        @Override
        public void onFailure(int reason) {
            wifiP2pServiceInfo = null;
        }
    });
  ```

Theo như **Tài liệu tham khảo** của **Android** thì sau khi đăng ký local service, framework sẽ tự động phản hồi tới các yêu cầu service discovery từ các thiết bị ngang hàng.

### 3.4. `DiscoverServices()`

```cs
    /// <summary>
    /// Tìm dịch vụ lân cận (không có timeout)
    /// </summary>
    public void DiscoverServices() {
        _wifiDirect.CallStatic("discoverServices");
    }
```

Phương thức này nhằm mục đích tìm kiếm các dịch vụ ngang hàng, trong thư viện Java, phương thức `discoverServices()` đơn giản là:

```java
    public static void discoverServices() {
        wifiDirectHandler.continuouslyDiscoverServices();
    }
```

Hàm `continuouslyDiscoverServices()` được triển khai như sau:

```java
private boolean serviceDiscoveryRegistered = false;
// Thuộc tính này để kiểm tra xem dịch vụ discovery đã được đăng ký chưa
private boolean isDiscovering = false;
// Thuộc tính này để kiểm tra xem có đang chạy dịch vụ discovery không
private List<ServiceDiscoveryTask> serviceDiscoveryTasks
// Danh sách để theo dõi các task discovery đang trong xử lý
private WifiP2pServiceRequest serviceRequest;
...
public void continuouslyDiscoverServices() {
    if (serviceDiscoveryRegistered == false) {
        registerServiceDiscoveryListeners();
        serviceDiscoveryRegistered = true;
    }

    if (isDiscovering) {
        // Do nothing
    } else {
        addServiceDiscoveryRequest();
        isDiscovering = true;
        serviceDiscoveryTasks = new ArrayList<>();
        discoverServices();
        submitServiceDiscoveryTask();
    }
}

// --- addServiceDiscoveryRequest
private void addServiceDiscoveryRequest() {
    serviceRequest = WifiP2pDnsSdServiceRequest.newInstance();

    // Nói cho framework ta muốn quét dịch vụ, tiền điều kiện cho việc discovery
    wifiP2pManager.addServiceRequest(
        channel,
        serviceRequest,
        new WifiP2pManager.ActionListener() {
            @Override
            public void onSuccess() {

            }

            @Override
            public void onFailure(int reason) {
                serviceRequest = null;
            }
        }
    );
}

// --- discoverServices
public void discoverServices() {
    // Khởi tạo service discovery method. Bắt đầu quét
    wifiP2pManager.discoverServices(channel, new WifiP2pManager.ActionListener() {
        @Override
        public void onSuccess() {

        }

        @Override
        public void onFailure(int reason) {
  
        }
    });
}

// --- submitServiceDiscoveryTask
private submitServiceDiscoveryTask() {
    int timeToWait = SERVICE_DISCOVERY_TIMEOUT; // init const, 120000ms
    ServiceDiscoveryTask serviceDiscoveryTask = new ServiceDiscoveryTask();
    Timer timer = new Timer();
    // Xác nhận tác vụ và thêm vào danh sách
    timer.schedule(serviceDiscoveryTask, timeToWait);
    serviceDiscoveryTasks.add(serviceDiscoveryTask);
}

// --- ServiceDiscoveryTask class
private class ServiceDiscoveryTask extends TimerTask {
    public void run() {
        discoverServices();
        if (isDiscovering) {
            submitServiceDiscoveryTask();
        }

        serviceDiscoveryTasks.remove(this)
    }
}

```

### 3.5. `StopDiscovering()`

```cs
/// <summary>
/// Stops searching for services
/// </summary>
public void StopDiscovering () {
    _wifiDirect.CallStatic ("stopDiscovering");
}
```

Phương thức này dừng việc tìm dịch vụ, có thể gọi khi đã kết nối thành công 1 dịch vụ nào đó. Trong thư viện:

```java
public static void stopDiscovering () {
        wifiDirectHandler.stopServiceDiscovery();
    }
```

Hàm `stopServiceDiscovery()`:

```java
private Map<String, DnsSdTxtRecord> dnsSdTxtRecordMap;
private Map<String, DnsSdService> dnsSdServiceMap;
...
public void stopServiceDiscovery() {
    if (isDiscovering) {
        dnsSdServiceMap = new HashMap<>();
        dnsSdTxtRecordMap = new HashMap<>();
        // Hủy tất cả các task discovery service đang tiến hành
        for (ServiceDiscoveryTask serviceDiscoveryTask : serviceDiscoveryTasks) {
            serviceDiscoveryTask.cancel();
        }
        serviceDiscoveryTasks = null;
        isDiscovering = false;
        clearServiceDiscoveryRequests();
    }
}

private void clearServiceDiscoveryRequests() {
    if (serviceRequest != null) {
        wifiP2pManager.clearServiceRequests(channel, new WifiP2pManager.ActionListener() {
            @Override
            public void onSuccess() {
                serviceRequest = null;
            }

            @Override
            public void onFailure(int reason) {

            }
        })
    }
}
```

### 3.6. `ConnectToService(string address)`

```cs
/// <summary>
/// Kết nối đến một dịch vụ
/// </summary>
/// <param name="address">
/// Địa chỉ (MAC) của dịch vụ
/// </param>
public void ConnectToService (string address) {
    _wifiDirect.CallStatic ("connectToService", address);
}
```

Phương thức này thiết lập kết nối đến 1 dịch vụ ở một thiết bị nào đó có địa chỉ MAC tương ứng là `address`. Trong thư viện:

```java
public static void connectToService (String address) {
    wifiDirectHandler.initiateConnectToService(wifiDirectHandler.getDnsSdServiceMap().get(address));
}
```

Hàm `initiateConnectToService()` nhận vào đối tượng thuộc lớp `DnsSdService`, lấy từ một map `DnsSdService`:

```java
public void initiateConnectToService(DnsSdService service) {
    // Thiết lập thông tin của thiết bị ngang hàng chứa dịch vụ
    WifiP2pConfig wifiP2pConfig = new WifiP2pConfig();
    wifiP2pConfig.deviceAddress = service.getService().deviceAddress;
    wifiP2pConfig.wps.setup = WpsInfo.PBC;

    // Bắt đầu 1 kết nối P2P với thiết bị được cấu hình ở trên
    wifiP2pManager.connect(channel, wifiP2pConfig, new WifiP2pManager.ActionListener() {
        @Override
        public void onSuccess() {

        }

        @Override
        public void onFailure(int reason) {

        }
    });
}
```

Về thuộc tính `dnsSdServiceMap` của lớp `WifiDirectHandler`

```java
/* Khởi tạo là thuộc tính có kiểu Map<String, DnsSdService>,
gồm key là địa chỉ và value là thông tin dịch vụ*/
private Map<String, DnsSdService> dnsSdServiceMap;
...
/* Constructor */
public WifiDirectHandler() {
    ...
    dnsSdServiceMap = new HashMap<>();
}
```

Ở trên, ta chưa nói đến hàm `registerServiceDiscoveryListeners()` được gọi trong hàm `continuouslyDiscoverServices()`. Ở hàm `registerServiceDiscoveryListeners()`, ta sẽ thêm các dịch vụ vào trong danh sách `dnsSdServiceMap`

```java
private void registerServiceDiscoveryListeners() {
    // DnsSdTxtRecordListener
    // Interface để gọi lại khi record Bonjour TXT là khả dụng cho 1 service
    // Sử dụng để nghe các record tới và lấy thông tin thiêt bị ngang hàng.
    // TODO: send object with intent
    WifiP2pManager.DnsSdTxtRecordListener txtRecordListener = new WifiP2pManager.DnsSdTxtRecordListener() {
        @Override
        public void onDnsSdTxtRecordAvailable(String fullDomainName, Map<String, String> txtRecordMap, WifiP2pDevice device) {
            Intent intent = new Intent(Action.DNS_SD_TXT_RECORD_AVAILABLE);
            intent.putExtra(TXT_MAP_KEY, device.deviceAddress);
            localBroadcastManager.sendBroadcast(intent);
            dnsSdTxtRecordMap.put(device.deviceAddress, new DnsSdTxtRecord(fullDomainName, txtRecordMap, device));
        }
    };

    // DnsSdServiceResponseListener
    // Interface để gọi lại khi nhận được phản hồi record Bonjour TXT
    // Sử dụng để lấy thông tin dịch vụ
    WifiP2pManager.DnsSdServiceResponseListener serviceResponseListener = new WifiP2pManager.DnsSdServiceResponseListener() {
        @Override
        public void onDnsSdServiceAvailable(String instanceName, String registrationType, WifiP2pDevice device) {
            dnsSdServiceMap.put(srcDevice.deviceAddress, new DnsSdService(instanceName, registrationType, device));
            Intent intent = new Intent(Action.DNS_SD_SERVICE_AVAILABLE);
            intent.putExtra(SERVICE_MAP_KEY, device.deviceAddress);
            localBroadcastManager.sendBroadcast(intent)
        }
    };

    wifiP2pManager.setDnsSdResponseListeners(channel, serviceResponseListener, txtRecordListener);
}
```

### 3.7. `PublishMessage(string msg)`

```cs
/// <summary>
/// Gửi 1 tin nhắn đến thiết bị đã được kết nối
/// </summary>
/// <param name="msg">
/// Tin nhắn cần gửi đi
/// </param>
public void PublishMessage (string msg) {
    _wifiDirect.CallStatic ("sendMessage", msg);
}
```

Sau khi đã kết nối, thiết bị có thể gửi tin nhắn với phương thức `sendMessage()` trong thư viện:

```java
public static void sendMessage(String msg) {
    try {
        wifiDirectHandler.getCommunicationManager().write(msg.getBytes("UTF-16"));
    } catch (Exception e) {
        // Có ngoại lệ vì một thằng ĐBRR nào đó không hỗ trợ UTF-16
    }
}
```

Và đây là lớp `CommunicationManager`

```java
import android.os.Handler;
import android.os.SystemClock;
import android.util.Log;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InterfaceAddress;
import java.net.Socket;
import java.nio.ByteBuffer;
import java.util.Arrays;

public class CommunicationManager implements Runnable {
    private Socket socket = null;
    private Handler handler;
    private OutputStream outputStream;

    public CommunicationManager(Socket socket, Handler handler) {
        this.socket = socket;
        this.handler = handler;
    }

    @Override
    public void run() {
        try {
            InputStream inputStream = socket.getInputStream();
            outputStream = socket.getOutputStream();
            byte[] messageSizeBuffer = new byte[Integer.SIZE/Byte.SIZE];
            int messageSize;
            byte[] buffer;
            int bytes;
            int totalBytes;
            handler.obtainMessage(WifiDirectHandler.MY_HANDLE, this).sendToTarget();

            while (true) {
                try {
                    bytes = inputStream.read(messageSizeBuffer);
                    if (bytes == -1) {
                        break;
                    }
                    messageSize = ByteBuffer.wrap(messageSizeBuffer).getInt();

                    buffer = new byte[messageSize];
                    bytes = inputStream.read(buffer);
                    totalBytes = bytes;
                    while (bytes != -1 && totalBytes < messageSize) {
                        bytes = inputStream.read(buffer, totalBytes, messageSize - totalBytes);
                        totalBytes += bytes;
                    }

                    if (bytes == -1) {
                        break;
                    }

                    handler.obtainMessage(WifiDirectHandler.MESSAGE_READ,
                            bytes, -1, buffer).sendToTarget();
                } catch (IOException e) {
                    handler.obtainMessage(WifiDirectHandler.COMMUNICATION_DISCONNECTED, this).sendToTarget();
                }
            }

        } catch (IOException e) {
            e.printStackTrace(); // ???
        } finally {
            try {
                socket.close();
            } catch (IOException e) {
                e.printStackTrace() // ???
            }
        }
    }

    public void write(byte[] message) {
        try {
            ByteBuffer sizeBuffer = ByteBuffer.allocate(Integer.Size/Byte.Size);
            byte[] sizeArray = sizeBuffer.putInt(message.length).array();
            byte[] completeMessage = new byte[sizeArray.length + message.length];
            System.arraycopy(sizeArray, 0, completeMessage, 0, sizeArra
            .length);
            System.arraycopy(message, 0, completeMessage, sizeArray.length, message.length);
            outputStream.write(completeMessage);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}
```

### 3.8. `OnReceiveStringifyRecord(string stringifyRecord)`

```cs
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
    int addrSplitAddress = stringifyRecord.IndexOf ('_');
    string addrEncoded = stringifyRecord.Substring (0, addrSplitAddress);
    string addr = Encoding.Unicode.GetString(Convert.FromBase64String(addrEncoded));
    string remaining = stringifyRecord.Substring (addrSplitAddress+1);
    int splitIndex = remaining.IndexOf ('_');
    Dictionary<string, string> record = new Dictionary<string, string> ();
    while (splitIndex > 0 && remaining.Length > 0) {
        int eqIndex = remaining.IndexOf ('?');
        string key = remaining.Substring (0, eqIndex);
        splitIndex = remaining.IndexOf ('_');
        string value = remaining.Substring (eqIndex + 1, splitIndex-eqIndex-1);
        remaining = remaining.Substring (splitIndex + 1);
        record.Add (Encoding.Unicode.GetString(Convert.FromBase64String(key)), Encoding.Unicode.GetString(Convert.FromBase64String(value)));
    }
    Debug.Log("stringify record found");
    this.OnTxtRecord (addr, record);
}
```

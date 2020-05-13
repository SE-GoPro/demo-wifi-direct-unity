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
    /// Khởi tạo thư viên, chỉ nên gọi 1 lần
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

### 3.5. `stopDiscovering()`

```cs
/// <summary>
/// Stops searching for services
/// </summary>
public void stopDiscovering () {
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

# Wi-Fi Direct Setup

## 1. Thiết lập các quyền của ứng dụng

Để có thể sử dụng Wi-Fi P2P, thêm các quyền sau vào file manifest:

- **`ACCESS_FINE_LOCATION`**: Quyền cho phép sử dụng các API sau
  - `discoverPeers`
  - `discoverServices`
  - `requestPeers`
- **`ACCESS_WIFI_STATE`**
- **`CHANGE_WIFI_STATE`**
- **`INTERNET`**: Wi-Fi P2P không yêu cầu kết nối Internet, nhưng sử dụng Java sockets tiêu chuẩn, yêu cầu phải có quyền `INTERNET`.

```XML
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.example.android.arss">
    ...
    <!-- Permissions -->
    <uses-permission
        android:required="true"
        android:name="android.permission.ACCESS_FINE_LOCATION" />
    <uses-permission
        android:required="true"
        android:name="android.permission.ACCESS_WIFI_STATE" />
    <uses-permission
        android:required="true"
        android:name="android.permission.CHANGE_WIFI_STATE"/>
    <uses-permission
        android:require="true"
        android:name="android.permission.INTERNET" />
    ...
</manifest>
```

## 2. Thiết lập một Broadcast Receiver và P2P Manager

Để sử dụng Wi-Fi P2P, ta cần nghe ngóng từ các broadcast intent. Trong ứng dụng của ta, khởi tạo một `Intent Filter` và thiết lập nghe các sự kiện sau:

- **`WIFI_P2P_STATE_CHANGED_ACTION`**: Cho biết liệu Wi-Fi P2P có được bật hay không.
- **`WIFI_P2P_PEERS_CHANGED_ACTION`**: Cho biết danh sách ghép cặp đã thay đổi.
- **`WIFI_P2P_CONNECTION_CHANGED_ACTION`**: Cho biết trạng thái của kết nối Wi-Fi P2P được thay đổi.
- **`WIFI_P2P_THIS_DEVICE_CHANGED_ACTION`**: Cho biết cấu hình chi tiết của thiết bị này đã thay đổi.

```Java
...
import android.content.IntentFilter;
...
import android.net.wifi.p2p.WifiP2pManager;

    private final IntentFilter intentFilter = new IntentFilter();
    ...
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);

        // Indicates a change in the Wi-Fi P2P status.
        intentFilter.addAction(WifiP2pManager.WIFI_P2P_STATE_CHANGED_ACTION);

        // Indicates a change in the list of available peers.
        intentFilter.addAction(WifiP2pManager.WIFI_P2P_PEERS_CHANGED_ACTION);

        // Indicates the state of Wi-Fi P2P connectivity has changed.
        intentFilter.addAction(WifiP2pManager.WIFI_P2P_CONNECTION_CHANGED_ACTION);

        // Indicates this device's details have changed.
        intentFilter.addAction(WifiP2pManager.WIFI_P2P_THIS_DEVICE_CHANGED_ACTION);
    ...
    }
```

Ở cuối phase `onCreate()`, lấy 1 instance của `WifiP2pManager`, gọi phương thức `initialize()`. Phương thức này trả về một đối tượng `WifiP2pManager.Channel` (Cái này sẽ được sử dụng sau).

```Java
...
Channel channel;
WifiP2pManager wifiP2pManager;
...
@Override
public void onCreate(Bundle savedInstanceState) {
    ...
    wifiP2pManager = (WifiP2pManager) getSystemService(Context.WIFI_P2P_SERVICE);
    channel = wifiP2pManager.initialize(this, getMainLooper(), null);
}
```

Lúc này, tạo 1 class extends `BroadcastReceiver` sử dụng để nghe ngóng thay đổi trạng thái Wi-Fi P2P của hệ thống. Trong phương thức `onReceive()`, thêm điều kiện để xử lý mỗi trạng thái P2P được liệt kê phía trên.

```Java
...
    private Boolean isWifiP2pEnabled;
    private WifiP2pDevice thisDevice;
    private LocalBroadcastManager localBroadcastManager;
...
    @Override
    private class WifiDirectBroadcastReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();

            if (WifiP2pManager.WIFI_P2P_STATE_CHANGED_ACTION.equals(action)) {
                // Determine if Wifi P2P mode is enabled or not, alert the activity
                int state = intent.getIntExtra(WifiP2pManager.EXTRA_WIFI_STATE, -1);
                if (state == WifiP2pManager.WIFI_P2P_STATE_ENABLED) {
                    isWifiP2pEnabled = true;
                } else {
                    isWifiP2pEnabled = false;
                }
            } else if (WifiP2pManager.WIFI_P2P_PEERS_CHANGED_ACTION.equals(action)) {
                // The peer list has changed! We should probably do something about that.
            } else if (WifiP2pManager.WIFI_P2P_CONNECTION_CHANGED_ACTION.equals(action)) {
                // Connection state changed! We should probably do something about that
            } else if (WifiP2pManager.WIFI_P2P_THIS_DEVICE_CHANGED_ACTION.equals(action)) {
                thisDevice = intent.getParcelableExtra(WifiP2pManager.EXTRA_WIFI_P2P_DEVICE);
                localBroadcastManager.sendBroadcast(new Intent("deviceChanged"));
            }
        }
    }

```

Cuối cùng, thêm code để đăng ký intent filter và broadcast receiver khi main activity trong trạng thái *active* và hủy đăng ký chúng khi activity bị tạm dừng. Khuyến nghị, đặt trong các phương thức `onResume()` và `onPaused()`.

```Java

@Override
public void onResume() {
    super.onResume();
    receiver = new WiFiDirectBroadcastReceiver(WifiP2pManager, channel, this);
    registerReceiver(receiver, intentFilter);
}

@Override
public void onPause() {
    super.onPause();
    unregisterReceiver(receiver);
}

```

## 3. Thiết lập peer discovery

Để bắt đầu tìm kiếm thiết bị gần kề với Wi-Fi P2P, gọi phương thức `discoveryPeers()` với 2 tham số:

- `WifiP2pManager.Channel`: channel nhận về khi thiết lập P2P mManager (`wifiP2pManager`)
- 1 implementation của `WifiP2pManager.ActionListener` với các phương thức mà hệ thống gọi khi tìm kiếm thành công hay thất bại.

```Java
wifiP2pManager.discoverPeers(channel, new WifiP2pManager.ActionListener() {

    @Override
    public void onSuccess() {
        /* Code for when the discovery initiation is successful goes here.
        No services have actually been discovered yet, so this method
        can often be left blank. Code for peer discovery goes in the
        onReceive method, detailed below. */
    }

    @Override
    public void onFailure(int reasonCode) {
        /* Code for when the discovery initiation fails goes here.
        Alert the user that something went wrong. */
    }
});
```

*Lưu ý rằng, `discoveryPeers()` mới chỉ thiết lập xong việc khám phá thiết bị ngang hàng, và thông qua 2 phương thức trong ActionListener để thông báo khởi tạo thành công hay thất bại. Hoạt động tìm kiếm vẫn tiếp tục cho đến khi kết nối được hình thành hoặc 1 nhóm P2P được hình thành.*

## 4. Lấy danh sách các thiết bị ngang hàng

Phần này nói đến việc lấy và xử lý danh sách thiết bị ngang hàng. Đầu tiên, implement interface `WifiP2pManager.PeerListListener`, nó sẽ đưa ra thông tin về các thiết bị ngang hàng mà Wi-Fi P2P phát hiện được. Những thông tin này cũng cho phép app biết được khi nào thiết bị ngang hàng tham gia hay rời khỏi mạng.

Snippet:

```Java
private List<WifiP2pDevice> peers = new ArrayList<WifiP2pDevice>();
...

private PeerListListener peerListListener = new PeerListListener() {
    @Override
    public void onPeerAvailable(WifiP2pDeviceList peerList) {
        if (!refreshedPeers.equals(peers)) {
            peers.clear();
            peers.addAll(refreshPeers);

            /*
            If an AdapterView is backed by this data, notify it
            of the change. For instance, if you have a ListView of
            available peers, trigger an update.*/

            // ((WiFiPeerListAdapter) getListAdapter()).notifyDataSetChanged();

            /*Perform any other updates needed based on the new list of
            peers connected to the Wi-Fi P2P network. */
        }

        if (peers.size() == 0) {
            // No device found. Return
            return;
        }
    }
}

```

Thay đổi trong phương thức `onReceive()` của broadcast receiver để gọi phương thức `requestPeers()` khi nhận được một intent với action `WIFI_P2P_PEERS_CHANGED_ACTION`. Bằng cách nào đó, phải truyền được listener vào receiver, ví dụ, gửi nó như 1 tham số vào constructor của broadcast receiver.

```Java
public void onReceive(Context context, Intent intent) {
    ...
    else if (WifiP2pManager.WIFI_P2P_PEERS_CHANGED_ACTION.equals(action)) {

        /* Request available peers from the wifi p2p manager. This is an
        asynchronous call and the calling activity is notified with a
        callback on PeerListListener.onPeersAvailable()*/
        if (wifiP2pManager != null) {
            wifiP2pManager.requestPeers(channel, peerListListener);
        }
        // Log.d(WiFiDirectActivity.TAG, "P2P peers changed");
    }
    ...
}

```

## 5. Kết nối tới một thiết bị ngang hàng

Để kết nối đến 1 thiết bị ngang hàng, tạo 1 đối tượng `WifiP2pConfig` và sao chép dữ liệu từ `WifiP2pDevice` đại diện cho thiết bị ta muốn kết nối. Sau đó, gọi phương thức `connect()`.

```Java
@Override
public void connect(int deviceIndex) {
    WifiP2pDevice device = peers.get(deviceIndex);

    WifiP2pConfig config = new WifiP2pConfig();
    config.deviceAddress = device.deviceAddress;
    config.wps.setup = WpsInfo.PBC;

    wifiP2pManager.connect(channel, config, new ActionListener() {

        @Override
        public void onSuccess() {
            // WiFiDirectBroadcastReceiver notifies us. Ignore for now.
        }

        @Override
        public void onFailure(int reasonCode) {
            // Handling errors.
        }
    });
}
```

Nếu mỗi thiết bị trong nhóm hỗ trợ Wi-Fi Direct, ta không cần phải hỏi rõ mật khẩu nhóm khi kết nối. Để cho phép thiết bị không hỗ trợ Wi-Fi Direct tham gia vào nhóm, mặt khác, chúng ta cần lấy mật khẩu bằng cách gọi `requestGroupInfo()`

```Java
wifiP2pManager.requestGroupInfo(channel, new GroupInfoListener() {
    @Override
    public void onGroupInfoAvailable(WifiP2pGroup group) {
        String groupPassword = group.getPassphrase();
    }
});
```

Để ý rằng, `WifiP2pManager.ActionListener` implement phương thức `connect()` chỉ thông báo khi đã thiết lập thành công hay thất bại. Để nghe ngóng sự thay đổi trạng thái kết nối, implement thêm interface `WifiP2pManager.ConnectionInfoListener` với phương thức `onConnectionInfoAvailable()`

```Java
@Override
public void onConnectionInfoAvailable(final WifiP2pInfo info) {
    // String from WifiP2pInfo struct
    String groupOwnerAddress = info.groupOwnerAddress.getHostAddress();

    // After the group negotiation, we can determine the group owner (server).
    if (info.groupFormed && info.isGroupOwner) {
        /* Do whatever tasks are specific to the group owner.
        One common case is creating a group owner thread and accepting
        incoming connections. */
    } else if (info.groupFormed) {
        /* The other device acts as the peer (client). In this case,
        you'll want to create a peer thread that connects
        to the group owner. */
    }
}
```

Lúc này, quay lại `onReceive()` của broadcast receiver và thêm vào phần `WIFI_P2P_CONNECTION_CHANGED_ACTION`. Khi nhận được intent này, gọi phương thức `requestConnectionInfo()`. Đây là một phương thức bất đồng bộ

```Java
...
} else if (WifiP2pManager.WIFI_P2P_CONNECTION_CHANGED_ACTION.equals(action)) {
    if (wifiP2pManager == null) {
        return;
    }

    NetworkInfo networkInfo = (NetworkInfo) intent
        .getParcelableExtra(WifiP2pManager.EXTRA_NETWORK_INFO);
    if (networkInfo.isConnected()) {
        // We are connected with the other device, request connection info to find group owner IP
        wifiP2pManager.requestConnectionInfo(channel, connectionListener);
    }
    ...
}
```

## 6. Tạo một nhóm

Nếu ta muốn thiết bị đang chạy ứng dụng của ta phục vụ như một group owner cho 1 mạng mà cho phép các thiết bị cũ không hỗ trợ Wi-Fi Direct, ta theo đúng các bước của phần [Kết nối tới một thiết bị ngang hàng](#5-k%E1%BA%BFt-n%E1%BB%91i-t%E1%BB%9Bi-m%E1%BB%99t-thi%E1%BA%BFt-b%E1%BB%8B-ngang-h%C3%A0ng), nhưng ta sẽ tạo 1 `WifiP2pManager.ActionListener` khác sử dụng phương thức `createGroup()` thay vì `connect()`.

```Java
wifiP2pManager.createGroup(channel, new WifiP2pManager.ActionListener() {
    @Override
    public void onSuccess() {
        // Device is ready to accept incoming connections from peers.
    }

    @Override
    public void onFailure(int reason) {
        // Handling errors.
    }
})
```

> ***Nếu tất cả các thiết bị trong mạng đều hỗ trợ Wi-Fi Direct thì ta có thể sử dụng phương thức `connect()` trên mỗi thiết bị, bởi vì phương thức sau đó sẽ tạo 1 nhóm và tự chọn group owner tự động.***

Sau khi tạo nhóm, ta có thể gọi `requestGroupInfo()` để lấy thông tin chi tiết của các thiết bị ngang hàng có trong mạng, bao gồm tên thiết bị và trạng thái kết nối.

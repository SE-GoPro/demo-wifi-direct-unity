# Sử dụng Wi-Fi Direct (P2P) trong service discovery

Viẹc sử dụng Wi-Fi Peer-to-Peer Service Discovery cho phép ta tìm kiếm các dịch vụ của thiết bị gần kề một cách trực tiếp, không cần phải kết nối tới 1 mạng nào cả. Ta cũng có thể "quảng bá" dịch vụ đang chạy trên thiết bị của ta. Các khả năng đó giúp ta giao tiếp giữa các ứng dụng, ngay cả khi không có mạng cục bộ hay 1 hotspot nào đó khả dụng.

## 1. Thiết lập manifest

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

## 2. Thêm 1 dịch vụ cục bộ

Nếu ứng dụng của ta thiết lập 1 dịch vụ cục bộ, ta cần đăng ký cho service discovery. Một khi đã được đăng ký, framework sẽ tự động phản hồi tới các yêu cầu service discovery từ các thiết bị ngang hàng.

Để tạo 1 dịch vụ cục bộ:

  1. Tạo 1 đối tượng `WifiP2pServiceInfo`

  2. Điền vào nó các thông tin liên quan đến dịch vụ

  3. Gọi `addLocalService()` để đăng ký cho service discovery

```Java
    private void startRegistation() {
        // Create a string map containing infomation about our service.
        Map record = new HashMap();

        record.put("listen_port", String.valueOf(SERVER_PORT));
        record.put("buddy_name", "DzuyVED" + (int) (Math.random() * 1000));
        record.put("available", "visible");

        /* Service infomation.
        Pass it an instance name, service type _protocol._transportlayer
        and the map containing information that other devices will want
        once they connect to this one.*/
        WifiP2pDnsSdServiceInfo serviceInfo = WifiP2pDnsSdServiceInfo.newInstance("_test", "_presence._tcp", record);

        /* Add the local service, sending the service info, network channel,
        and listener that will be used to indicate success or failure of the request.*/

        wifiP2pManager.addLocalService(channel, serviceInfo, new ActionListener() {
            @Override
            public void onSuccess() {
                /* Command successful! Code isn't necessarily needed here,
                Unless you want to update the UI or add logging statements. */
            }

            @Override
            public void onFailure(int reasonCode) {
                // Command failed.  Check for P2P_UNSUPPORTED, ERROR, or BUSY
            }
        });
    }
```

## 3. Tìm kiếm dịch vụ gần cạnh

Android sử dụng các phương thức callback để thông báo ứng dụng của ta về các dịch vụ khả dụng, do đó, điều đầu tiên là cần phải thiết lập chúng trước.
Tạo một `WifiP2pManager.DnsSdTxtRecordListener` để nghe các record đến. Record này có thể tùy ý được broadcast bởi các thiết bị khác. Khi 1 record đến, ta sao chép địa chỉ của thiết bị và các thông tin liên quan.
Ví dụ sau giả sử 1 record chứa `"buddy_name"` đi cùng với định danh của người dùng:

```Java
final HashMap<String, String> buddies = new HashMap<String, String>();
...
private void discoverService() {
    DnsSdTxtRecordListener txtListener = new DnsSdTxtRecordListener() {
        @Override
        /* Callback includes:
         * fullDomain: full domain name: e.g "printer._ipp._tcp.local."
         * record: TXT record dta as a map of key/value pairs.
         * device: The device running the advertised service.
         */

        public void  onDnsSdTxtRecordAvailable(
            String fullDomain, Map record, WifiP2pDevice device) {
                buddies.put(device.deviceAddress, record.get("buddy_name"));
        }
    };
}
```

Để lấy được thông tin của dịch vụ, tạo 1 `WifiP2pManager.DnsSdServiceResponseListener`. Nó sẽ nhận được mô tả chi tiết và thông tin của kết nối. Khi `DnsSdTxtRecordListener` và `DnsSdServiceResponseListener` được implement, thêm chúng vào `WifiP2pManager` sử dụng phương thức `setDnsSdResponseListener()`

```Java
private void discoverService() {
    ...
    DnsSdServiceResponseListener serviceListener = new DnsSdServiceResponseListener() {
        @Override
        public void onDnsSdServiceAvailable(String instanceName, String registationType, WifiP2pDevice resourceType) {
        /* Update the device name with the human-friendly version from
        the DnsTxtRecord, assuming one arrived.*/
        resourceType.deviceName = buddies
            .containKey(resourceType.deviceAddress) ? buddies
            .get(resourceType.deviceAddress) : resourceType.deviceName;

        /* Add to the custom adapter defined specifically for showing
        wifi devices.*/

        WiFiDirectServiceList fragment = (WiFiDirectServiceList) getFragmentManager()
            .findFragmentById(R.id.frag_peerlist);

        WiFiDevicesAdapter adapter = ((WiFiDevicesAdapter) fragment.getListAdapter());
        adapter.add(resourceType);
        adapter.notifyDataSetChanged();

        Log.d(TAG, "onBonjourServiceAvailable " + instanceName);
        }
    };

    wifiP2pManager.setDnsSdResponseListeners(channel, serviceListener, txtListener);
}
```

Lúc này, tạo 1 yêu cầu dịch vụ và gọi `addServiceRequest()`. Phương thức này cũng có 1 listener để báo cáo trạng thái thành công hay thất bại.

```Java
    serviceRequest = WifiP2pDnsSdServiceRequest.newInstance();
    wifiP2pManager.addServiceRequest(channel, serviceRequest, new ActionListener() {
        @Override
        public void onSuccess() {
            // Success!
        }

        @Override
        public void onFailure(int reasonCode) {
            // Command failed. Check for P2P_UNSUPPORTED, ERROR or BUSY
        }
    });
```

Cuối cùng, gọi phương thức `discoverServices()`.

```Java
    wifiP2pManager.discoverServices(channel, new ActionListener() {
        @Override
        public void onSuccess() {
            // Success!
        }

        @Override
        public void onFailure(int reasonCode) {
            // Command failed.  Check for P2P_UNSUPPORTED, ERROR, or BUSY
            if (reasonCode == WifiP2pManager.P2P_UNSUPPORTED) {
                Log.d(TAG, "P2P isn't supported on this device.");
            } else if(...)
                ...
        });
```

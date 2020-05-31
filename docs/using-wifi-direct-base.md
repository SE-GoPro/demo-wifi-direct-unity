# Sử dụng Wifi Direct Base

## Overview

Lớp base này có sẵn các phương thức sử dụng để kết nối Wifi P2P trong game build bằng Unity.
> Để sử dụng, import thư viện Android `UnityWifiDirect.aar` và script `WifiDirectBase.cs` vào project Unity và tạo 1 script kế thừa từ `WifiDirectBase` để sử dụng.

## Sử dụng

### Step 1. Khởi tạo service

Ví dụ ta có chế độ chơi qua mạng Wi-Fi Direct của 1 game nào đó, script chịu trách nhiệm kết nối cần phải khởi tạo kết nối trước.

```cs
...
public class WifiDirectController : WifiDirectBase {
    ...
    base.Initialize(this.gameObject.name);
    // Khởi tạo và gắn game object tương ứng với script này.
    ...
}
...
```

### Step 2. Broadcast và Discover service

Sau khi khởi tạo, `base` sử dụng phương thức `OnServiceConnected()` để bắt sự kiện khởi tạo thành công. Lúc này, ta có thể broadcast service để máy khác tìm kiếm, đồng thời tìm kiếm service bằng cách discover service.

```cs
public class WifiDirectController : WifiDirectBase {
    ...
    public override void OnServiceConnected() {
        Dictionary<string, string> initialRecord = new Dictionary<string, string> {
            { "playerName", "JohnCena" }
        }
        base.BroadcastService("multiPlayer", initialRecord);
        base.DiscoverService();
    }
    ...
}
```

### Step 3. Xử lý khi tìm thấy service từ máy khác

Hiện tại thư viện gốc cũng chưa xử lý gì với tên service và record khởi tạo nên tạm thời là cứ máy nào có service với gameObject trùng tên là kết nối được.

```cs
public class WifiDirectController : WifiDirectBase {
    ...
    public override void OnServiceFound(string address) {
       // Làm cái gì đó với địa chỉ `address` tìm được.
       // Ví dụ: In ra UI "Tìm thấy người chơi tại địa chỉ: `address`"
    }
    ...
}
```

### Step 4. Kết nối

Sau khi có địa chỉ của máy bắt cặp, ta gửi yêu cầu kết nối với máy đó.

```cs
public class WifiDirectController : WifiDirectBase {
    ...
    private void Connect(string address) {
        base.ConnectToService(address);
    }
    ...
}
```

### Step 5. Xử lý khi kết nối thành công

Sử dụng hàm `OnConnect()` của `base`

### Step 6. Gửi message (dạng String)

Sử dụng hàm `PublishMessage(string message)` của `base`

### Step 7. Xử lý khi nhận được message (dạng String)

Override hàm `OnReceiveMessage(string message)` của `base`

## TODO

Cách gửi dictionary ?

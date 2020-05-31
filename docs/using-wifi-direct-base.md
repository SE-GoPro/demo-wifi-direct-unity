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

Hiện tại thư viện gốc mới dừng lại ở chỉ khi có service đến với record được broadcast như trên. Ta có thể thêm thông tin serivce như "Tên người chơi", "loại máy" vào record khởi tạo.

Khi không có record đi kèm service

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

Khi có record khởi tạo đi kèm (hiện không sử dụng được)

```cs
...
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

Ở đây `base` đã xử lý sẵn record đến, việc xử lý của lớp controller là ở phương thức `OnTxtRecord`. Hãy `override` nó để hiển thị cho hợp lý.

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

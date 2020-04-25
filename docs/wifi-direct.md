# Wi-Fi Direct

> ***References**:
> <https://developer.android.com/training/connect-devices-wirelessly/wifi-direct>
> <https://www.howtogeek.com/178691/htg-explains-what-is-wi-fi-direct-and-how-does-it-work/>*

## 1. Giới thiệu về Wi-Fi Direct

Hiện tại, ngày càng có nhiều thiết bị mới sử dụng Wi-Fi Direct. Wi-Fi Direct cho phép hai thiết bị thiết lập một kết nối trực tiếp, ngang hàng (P2P) mà không cần đến các điểm truy cập không dây.

Wi-Fi Direct gần giống với khái niệm "ad-hoc" Wi-Fi mode. Tuy nhiên, khác với kết nối "ad-hoc" Wi-Fi, Wi-Fi Direct bao gồm một phương thức dễ dàng để tự động tìm kiếm các thiết bị gần cạnh và kết nối chúng.

### **Ví dụ**

Lấy ví dụ với thiết bị streaming media player Roku 3 và điều khiển từ xa của nó, thay vì sử đụng kết nối Bluetooth hay kết nối IR blaster thì nó sử dụng Wi-Fi Direct.

- Roku tạo 1 mạng Wi-Fi mới mà điều khiển từ xa kết nối vào nó và chúng giao tiếp thông qua mạng nhỏ đó.
- Khóa bảo mật được tự động trao đổi giữa 2 thiết bị (bên ngoài không thể biết).

Ví dụ khác: Miracast wireless display standard.

### **Android**

Android cũng hỗ trợ sẵn Wi-Fi Direct, mặc dù chưa nhiều ứng dụng sử dụng.

## 3. Cách thức hoạt động của Wi-Fi Direct

Wi-Fi Direct sử dụng một số chuẩn để thực hiện các chức năng:

- **Wi-Fi**:

  - Wi-Fi Direct sử dụng công nghệ Wi-Fi, cho phép thiết bị sử dụng để kết nối với router không dây.
  - Một thiết bị Wi-Fi Direct có thể thực hiện chức năng cơ bản như một điểm truy cập và các thiết bị khác có thể kết nối đến nó một cách trực tiếp.
  - Điều này đã có trong mạng "ad-hoc", nhưng Wi-Fi Direct mở rộng tính năng này với việc dễ thiết lập và tính năng "khám phá".

- **Thiết bị Wi-Fi Direct và dịch vụ khám phá (Discovery Service)**:

  - Giao thức này cho các thiết bị Wi-Fi Direct một khả năng để tìm kiếm/khám phá lẫn nhau và các dịch vụ chúng hỗ trợ trước khi kết nối.
  - *Ví dụ*: Một thiết bị Wi-Fi Direct có thể thấy được các thiết bị tương thích trong khu vực và sau đó thu hẹp lại danh sách chỉ gồm các thiết bị cho phép in được trước khi hiển thị một danh sách các máy in cho phép Wi-Fi Direct ở gần đó.

- **Thiết lập bảo vệ Wi-Fi**: Khi 2 thiết bị kết nối với nhau, chúng tự động kết nối thông qua *thiết lập bảo vệ Wi-Fi* - *Wi-Fi Protected Setup* hay *WPS*. Phương thức thiết lập cho WPS cần phải an toàn, không nên nguy hiểm như PIN WPS...
- **WPA2**: Các thiết bị Wi-Fi Direct có thể sử dụng mã hóa WPA2, hiện đang là phương thức mã hóa tốt nhất cho Wi-Fi.

Wi-Fi Direct cũng có thể gọi là Wi-Fi peer-to-peer hay Wi-Fi P2P. Các thiết bị Wi-Fi Direct có kết nối lẫn nhau một cách trực tiếp thay vì phải thông qua router không dây hay điểm truy cập.
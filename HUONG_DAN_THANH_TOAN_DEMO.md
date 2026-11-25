# Hướng dẫn cổng thanh toán mô phỏng TechMarket

## 1. Cổng thanh toán mới

TechMarket sử dụng các mã phương thức:
- `COD`: Thanh toán khi nhận hàng.
- `DEMOQR`: QR Demo.
- `DEMOBANK`: Ngân hàng Demo.
- `DEMOCARD`: Thẻ quốc tế Demo.

QR được tạo bằng QRCoder và chỉ chứa chuỗi dữ liệu mô phỏng của TechMarket. Không thể dùng QR này để chuyển tiền thật.

## 2. Package mới

Dự án đã thêm package `QRCoder` phiên bản `1.6.0`.

Trong Visual Studio:
1. Mở `Tech_Marrket.csproj`.
2. Restore NuGet Packages (hoặc Build để Visual Studio tự restore).
3. Nếu dùng Package Manager Console, chạy `dotnet restore` trong thư mục chứa file csproj.

## 3. Database

Model `Order` đã có:
- `PaymentStatus`
- `PaymentTransactionNo`
- `PaymentTransactionRef`
- `PaidAt`

Dự án đã có migration `20261119085139_AddVNPayPaymentFields` tạo các cột và unique index cần thiết. Không xóa migration này và không chạy `Remove-Migration` nếu migration đã được áp dụng vào database.

Nếu migration đã được áp dụng trước đó, không cần tạo migration mới cho phần thanh toán Demo vì cấu trúc database không đổi.

Nếu chưa từng cập nhật database, chạy trong Package Manager Console:
```powershell
Update-Database
```

## 4. Luồng hoạt động

1. Khách hàng thêm sản phẩm vào giỏ hàng.
2. Chọn COD, QR Demo, Ngân hàng Demo hoặc Thẻ Demo ở Checkout.
3. TechMarket tạo Order, OrderDetails và StoreOrder.
4. Tồn kho được trừ đúng một lần lúc tạo đơn.
5. Với COD: chuyển đến lịch sử đơn hàng.
6. Với phương thức Demo: chuyển đến trang `DemoPayment/Pay`.
7. Khách hàng chọn mô phỏng thành công hoặc thất bại.
8. Nếu thất bại, khách có thể thanh toán lại hoặc đổi phương thức trong chi tiết đơn hàng.
9. Nếu thành công, đơn hàng được đánh dấu `Đã thanh toán`; không thể đổi phương thức hoặc hủy bằng chức năng tự phục vụ.
10. StoreOwner không thể chuyển đơn online chưa thanh toán sang trạng thái giao hàng.

## 5. VNPay cũ

Trong bản ZIP ban đầu:
- Không có `VNPayService.cs`.
- Không có `VNPayController.cs`.
- `Program.cs` không đăng ký `VNPayService`.
- `appsettings.json` không có cấu hình VNPay.

Migration mang tên `AddVNPayPaymentFields` được giữ lại vì đây là migration đã tạo các cột thanh toán mà cổng Demo vẫn dùng. Không xóa hoặc đổi tên migration đã áp dụng.

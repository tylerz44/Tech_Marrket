# Hướng dẫn cài đặt mã giảm giá TechMarket

## Chức năng
- Admin: `/Admin/Discount` tạo/sửa/kích hoạt mã giảm giá.
- Khách hàng: `/Coupon` nhận mã trong kho voucher.
- Checkout: chọn mã đã nhận; máy chủ kiểm tra quyền sở hữu, thời hạn, lượt sử dụng và đơn tối thiểu.
- Đơn hàng lưu `DiscountCode`, `DiscountAmount`; `TotalAmount` là số tiền sau giảm.
- Hủy đơn chưa thanh toán sẽ trả lại lượt sử dụng và voucher cho khách.

## Tạo database migration
Mở Visual Studio 2022 > Tools > NuGet Package Manager > Package Manager Console, chọn project `Tech_Marrket`, chạy:

```powershell
Add-Migration AddDiscountCoupons
Update-Database
```

Nếu dùng CLI tại thư mục chứa file `.csproj`:

```bash
dotnet ef migrations add AddDiscountCoupons
dotnet ef database update
```

## Cách thử
1. Đăng nhập tài khoản Admin, mở Quản trị > Quản lý mã giảm giá.
2. Tạo mã, ví dụ `TECH10`, loại phần trăm, giá trị 10, đơn tối thiểu 100000, giới hạn 100, ngày bắt đầu/kết thúc hợp lệ.
3. Đăng nhập Customer, mở menu tài khoản > Kho mã giảm giá và nhấn Nhận mã.
4. Thêm sản phẩm vào giỏ, vào Checkout, chọn voucher đã nhận.
5. Đặt đơn COD hoặc Demo. Đơn hàng sẽ lưu số tiền giảm và tổng tiền sau giảm.

Lưu ý: Mã chỉ dùng được sau khi khách hàng đã nhận trong kho voucher. Số tiền giảm được tính và kiểm tra lại phía máy chủ, không lấy từ trình duyệt.

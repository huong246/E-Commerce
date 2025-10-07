E-Commerce 
- E-Commerce là hệ thống backend (API) mạnh mẽ cho một nền tảng thương mại điện tử với đa nhà cung cấp, được xây dựng bằng ASP.NET Core và C#. Dự án này hỗ trợ đầy đủ các tính năng cần thiết của một sàn thương mại điện tử như quản lý sản phẩm, quản lý giỏ hàng, đặt hàng, thanh toán trực tuyến, hỗ trợ đổi trả hàng, đánh giá sản phẩm sau mua, cho đến hệ thống chat giữa người dùng và thông báo real-time.
- Công Nghệ Sử Dụng:
  + Backend: .NET 9, ASP.NET Core Web API, ngôn ngữ C#
  + Database: Entity Framework Core, SQLite server.
  + Xác thực & Phân quyền: ASP.NET Core Identity, JWT (JSON Web Tokens).
  + Real-time Communication: SignalR.
  + Thanh toán & Dịch vụ: Tích hợp VNPAY (qua VnPayLibrary.cs), SendGrid (cho EmailService).
  + Tools: Swagger UI, Git, Postman, GitHub.
- Tính Năng:
  + Dành cho Khách hàng (Customer):
        * Xác thực & Người dùng: Đăng ký, đăng nhập, quản lý thông tin cá nhân (profile). Bảo mật người dùng và quản lý người dùng thông qua UserManager và SignInManager.
        * Sản phẩm: Tìm kiếm sản phẩm (hỗ trợ Full-Text Search), xem chi tiết, xem đánh giá sản phẩm sau mua.
        * Giỏ hàng: Thêm, xóa, sửa số lượng sản phẩm trong giỏ hàng.
        * Đặt hàng: Cho phép thực hiện quy trình đặt hàng từ nhiều shop khác nhau trong một đơn hàng.
        * Thanh toán: Tích hợp cổng thanh toán VNPAY để thanh toán trực tuyến hoặc qua ví sàn.
        * Quản lý đơn hàng: Theo dõi trạng thái đơn hàng, xem lịch sử mua hàng.
        * Tương tác: Để lại đánh giá cho sản phẩm sau khi đơn hàng hoàn thành, chat trực tiếp với người bán.
        *Đăng ký bán hàng: Gửi yêu cầu để trở thành người bán (Seller).
  + Dành cho Người bán (Seller):
        * Quản lý Shop: Tạo và quản lý thông tin cửa hàng.
        * Quản lý Sản phẩm: Đăng tải, cập nhật, xóa sản phẩm và hình ảnh.
        * Quản lý Đơn hàng: Xử lý các đơn hàng thuộc shop của mình (xác nhận, vận chuyển, hủy, trả hàng).
        * Quản lý Voucher: Tạo và quản lý các mã giảm giá cho shop.
  + Tính năng hệ thống:
        * Real-time: Tích hợp SignalR cho hệ thống Chat và Thông báo theo thời gian thực.
        * Bảo mật: Sử dụng JWT (JSON Web Tokens) cho việc xác thực và phân quyền API.
        * Thanh toán: Tích hợp thư viện VNPAY và ví sàn cho các giao dịch thanh toán.
        * Email Service: Gửi email xác nhận, thông báo qua SendGrid.
        * API: Tự động sinh tài liệu API với Swagger/OpenAPI.

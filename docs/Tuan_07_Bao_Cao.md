# BÁO CÁO THỰC HÀNH TUẦN 07 — KIỂM THỬ & HOÀN THIỆN HỆ THỐNG QTKD

**Môn học:** Phát triển ứng dụng Hệ thống Thông tin Hiện đại  
**Công nghệ:** C# · ASP.NET Core 10 Web API · Entity Framework Core · PostgreSQL · xUnit · Moq  
**Kết quả kiểm thử tự động mới nhất:** **31/31 Passed** (`0 Failed`, `0 Skipped`) — File minh chứng TRX: [`week07_test_results.trx`](test-results/week07_test_results.trx)

---

## 1. CẤU TRÚC PROJECT KIỂM THỬ (`tests/QTKD.Tests`)

Tuần 07 kế thừa trực tiếp mã nguồn và database của Tuần 05–06 trong cùng một repository, bổ sung project xUnit độc lập tại [`tests/QTKD.Tests/QTKD.Tests.csproj`](../tests/QTKD.Tests/QTKD.Tests.csproj) và tách biệt hai tầng kiểm thử:

1. **Unit Tests (`tests/QTKD.Tests/UnitTests/`)**: Kiểm thử trực tiếp tầng Service nghiệp vụ bằng **xUnit + Moq** (giả lập `IQtkdRepository`, không chạm DB thật):
   - [`ContractServiceRuleTests.cs`](../tests/QTKD.Tests/UnitTests/ContractServiceRuleTests.cs): Kiểm tra **CTR-01**, **CTR-02**, **CTR-03**, **AC01**, **AC05** (12 lượt test case đúng/sai).
   - [`CustomerServiceTests.cs`](../tests/QTKD.Tests/UnitTests/CustomerServiceTests.cs): Kiểm tra tạo và cập nhật Customer (3 lượt test case đúng/sai).
   - [`PricingServiceTests.cs`](../tests/QTKD.Tests/UnitTests/PricingServiceTests.cs): Kiểm tra tạo PriceItem và ràng buộc `UnitPrice > 0` (4 lượt test case đúng/sai).
2. **Integration Tests (`tests/QTKD.Tests/IntegrationTests/`)**: Kiểm thử toàn bộ HTTP pipeline (Routing $\rightarrow$ Controller $\rightarrow$ Middleware $\rightarrow$ Service $\rightarrow$ EF Core $\rightarrow$ PostgreSQL) bằng `WebApplicationFactory<Program>` trỏ tới **database kiểm thử riêng `QtkdTestDb`** (tự động `EnsureDeleted()` + `Migrate()` trước khi chạy, tuyệt đối không đụng DB chính `QtkdDb`):
   - [`CustomWebApplicationFactory.cs`](../tests/QTKD.Tests/IntegrationTests/CustomWebApplicationFactory.cs) & [`IntegrationTestCollection.cs`](../tests/QTKD.Tests/IntegrationTests/IntegrationTestCollection.cs)
   - [`CustomerIntegrationTests.cs`](../tests/QTKD.Tests/IntegrationTests/CustomerIntegrationTests.cs): `TC01`, `TC02`
   - [`ContractIntegrationTests.cs`](../tests/QTKD.Tests/IntegrationTests/ContractIntegrationTests.cs): `TC03`–`TC08`, `TC08b` (gửi đồng thời / Slide 20 TC06), `AC01`, `PricingApi`, và luồng demo Tuần 06 E2E.

---

## 2. BẢNG ĐỐI CHIẾU TEST CASE TỐI THIỂU (`TC01`–`TC08`) THEO MỤC 5.3 & 5.4

Mỗi Integration Test đều kiểm chứng đủ **3 lớp**: (1) **HTTP Status Code**, (2) **nội dung JSON trả về**, và (3) **trạng thái lưu thực tế trong PostgreSQL `QtkdTestDb`**.

| Mã Test | Dữ liệu / Thao tác | Kết quả mong đợi | Trạng thái | Hình thức thực hiện | Phương thức kiểm thử tương ứng |
|---|---|---|---|---|---|
| **TC01** | Tạo Customer hợp lệ (`Name`, `Status = Active`) | Trả về `201 Created`, tạo được và đọc lại khớp dữ liệu từ DB | **Pass** | Tự động (Integration Test) | `CustomerIntegrationTests.TC01_CreateValidCustomer_ShouldSucceed_AndReadBackFromDb` |
| **TC02** | Tạo Customer thiếu thông tin bắt buộc (`Name = "   "`) | Bị từ chối `400 Bad Request`; có mảng `errors` báo lỗi rõ ràng; DB không tăng bản ghi | **Pass** | Tự động (Integration Test) | `CustomerIntegrationTests.TC02_CreateCustomer_WhenNameIsEmpty_ShouldReturn400WithErrorMessage_AndNotSaveToDb` |
| **TC03** | Sửa Contract ở trạng thái `Draft` với dữ liệu hợp lệ | Trả về `200 OK`; dữ liệu mới (`ContractNo`, `FileUrl`, `ContractContent`) được lưu vào DB | **Pass** | Tự động (Integration Test) | `ContractIntegrationTests.TC03_UpdateContract_WhenDraftAndValid_ShouldReturn200_AndPersistInDb` |
| **TC04** | Sửa Contract khi đang ở `PendingApproval` (**CTR-01**) | Bị từ chối `409 Conflict` kèm thông báo vi phạm `CTR-01`; dữ liệu trong DB giữ nguyên | **Pass** | Tự động (Integration Test) | `ContractIntegrationTests.TC04_UpdateContract_WhenPendingApproval_ShouldReturn409_AndKeepOriginalData` |
| **TC05** | Submit Contract thiếu điều kiện **CTR-02** (thiếu `FileUrl` & thiếu Quốc hiệu) | Bị từ chối `400 Bad Request` kèm danh sách 2 điều kiện chưa đạt; trạng thái DB vẫn là `Draft`, `CurrentApprovalId = null` | **Pass** | Tự động (Integration Test) | `ContractIntegrationTests.TC05_SubmitContract_WhenMissingCtr02Conditions_ShouldReturn400WithErrors_AndRemainDraft` |
| **TC06** | Submit Contract hợp lệ đủ **CTR-02** với quyền `Sales` | Trả về `200 OK`; trạng thái trong JSON và DB chuyển sang `PendingApproval`, sinh `ApprovalId` | **Pass** | Tự động (Integration Test) | `ContractIntegrationTests.TC06_SubmitContract_WhenValid_ShouldReturn200_AndUpdateDbToPendingApproval` |
| **TC07** | Thử chuyển thẳng `Draft` $\rightarrow$ `Approved` qua `PUT /api/contracts/{id}` (**CTR-03**) | Bị từ chối `409 Conflict` (vi phạm `CTR-03`); trạng thái DB vẫn là `Draft` | **Pass** | Tự động (Integration Test) | `ContractIntegrationTests.TC07_TransitionDraftDirectlyToApproved_ShouldBeRejected_AndRemainDraft` |
| **TC08** | Submit lặp lại cùng `Idempotency-Key` hoặc submit lại khi thiếu key (**AC05 / AC02**) | Lặp cùng key trả `200 OK` với đúng `ApprovalId` cũ; lặp khác/thiếu key trả `409 Conflict`; DB chỉ giữ 1 phiên | **Pass** | Tự động (Integration Test) | `ContractIntegrationTests.TC08_SubmitContractTwice_ShouldNotCreateDuplicateApproval` |

> **Ghi chú tự động hóa:** Toàn bộ **8/8 testcase tối thiểu (`TC01–TC08`)** đều đã được **tự động hóa 100%** trong bộ xUnit Integration Tests (không có test case nào phải chạy tay thay thế). Ngoài ra bộ test còn phủ thêm:
> - **`TC08b` (Slide 20 TC06):** Gửi 2 yêu cầu `Submit` đồng thời (`Task.WhenAll`) $\rightarrow$ Đúng 1 request thành công (`200 OK`) và 1 request bị chặn (`409 Conflict`), không phát sinh hai workflow (`Pass`).
> - **`AC01` (Slide 20 TC01):** Người dùng không phải `Sales` (`X-User-Role: Guest`) gửi trình duyệt $\rightarrow$ Bị từ chối `403 Forbidden`, hợp đồng giữ nguyên `Draft` (`Pass`).
> - **`PricingApi`:** Tạo `PriceList`, từ chối `UnitPrice <= 0` (`400 Bad Request`), thêm `PriceItem` hợp lệ và kiểm tra DB (`Pass`).
> - **`Week06DemoFlow E2E`:** Tự động hóa toàn bộ 7 bước luồng demo Tuần 06 từ `Customer` $\rightarrow$ `Draft` $\rightarrow$ `Submit lỗi 400` $\rightarrow$ `PUT bổ sung` $\rightarrow$ `Submit thành công 200` $\rightarrow$ `GET đối chiếu DB` $\rightarrow$ `Chống sửa/gửi lặp` (`Pass`).

---

## 3. BẢNG ĐỐI CHIẾU UNIT TEST CHO CÁC QUY TẮC NGHIỆP VỤ (BƯỚC 15)

| Quy tắc | Trường hợp Đúng (Valid Case) | Trường hợp Sai (Invalid Case) | File kiểm thử |
|---|---|---|---|
| **CTR-01** | Cho phép `UpdateAsync` khi hợp đồng ở `Draft` và `RevisionRequested` (2 ca `[Theory]`) | Ném `StateConflictException` (`409`) khi hợp đồng ở `PendingApproval`, `Approved`, `Rejected` (3 ca `[Theory]`), không gọi `UpdateContractAsync` | [`ContractServiceRuleTests.cs`](../tests/QTKD.Tests/UnitTests/ContractServiceRuleTests.cs) |
| **CTR-02** | `SubmitContractAsync` thành công khi Customer `Active`, `EndDate > StartDate`, có `FileUrl` và Quốc hiệu | Ném `BusinessRuleException` (`400`) khi thiếu `FileUrl`/Quốc hiệu hoặc Customer không `Active`/`EndDate <= StartDate`, không cập nhật DB | [`ContractServiceRuleTests.cs`](../tests/QTKD.Tests/UnitTests/ContractServiceRuleTests.cs) |
| **CTR-03** | Submit hợp lệ chỉ chuyển từ `Draft` sang `PendingApproval`, tuyệt đối không thành `Approved` | Thử gửi `UpdateAsync` đổi trực tiếp `Status = Approved` từ `Draft` ném `StateConflictException` (`409`), trạng thái giữ nguyên `Draft` | [`ContractServiceRuleTests.cs`](../tests/QTKD.Tests/UnitTests/ContractServiceRuleTests.cs) |
| **Customer & Pricing** | Tạo Customer hợp lệ (trim khoảng trắng); thêm `PriceItem` khi `UnitPrice > 0` | Từ chối Customer tên trống (`400`), Customer không tồn tại (`404`); từ chối `UnitPrice <= 0` (`0`, `-50000`) và `PriceList` không tồn tại (`404`) | [`CustomerServiceTests.cs`](../tests/QTKD.Tests/UnitTests/CustomerServiceTests.cs), [`PricingServiceTests.cs`](../tests/QTKD.Tests/UnitTests/PricingServiceTests.cs) |

---

## 4. CÁC LỖI PHÁT HIỆN & ĐÃ SỬA TRONG QUÁ TRÌNH KIỂM THỬ (MỤC 5.4)

Trong quá trình xây dựng và chạy thử bộ test Tuần 07 trên nền mã nguồn Tuần 06 (commit gốc `68657e0`), đã phát hiện và khắc phục **4 lỗi**:

### Lỗi 1: Bỏ qua yêu cầu đổi trạng thái trực tiếp `Draft` $\rightarrow$ `Approved` (`TC07` / `CTR-03`)
*   **Cách tái hiện:** Gửi `PUT /api/contracts/{id}` đối với hợp đồng `Draft` kèm JSON `{"status": "Approved"}`.
*   **Hành vi lỗi trước khi sửa:** `UpdateContractDto` không có thuộc tính `Status`, nên ASP.NET Core âm thầm bỏ qua trường `"status"` và API vẫn trả về **`200 OK`** thay vì từ chối vi phạm `CTR-03`.
*   **Nguyên nhân gốc:** `UpdateContractDto` thiếu field `Status` và `ContractService.UpdateAsync` chưa có guard kiểm tra hành vi chuyển trạng thái trực tiếp theo `CTR-03`.
*   **Cách sửa:** Thêm `public ContractStatus? Status { get; set; }` vào [`ContractDtos.cs`](../DTOs/ContractDtos.cs) và bổ sung kiểm tra trong [`ContractService.cs`](../Services/ContractService.cs) ném `StateConflictException` (`409 Conflict`) khi cố tình chuyển `Draft` $\rightarrow$ `Approved` (hoặc thay đổi trạng thái trái phép qua `PUT`).

### Lỗi 2: Validation của `Customer` không đi qua `GlobalExceptionMiddleware` (`TC02`)
*   **Cách tái hiện:** Gửi `POST /api/customers` với body `{"name": "", "status": "Active"}`.
*   **Hành vi lỗi trước khi sửa:** `[Required]` DataAnnotation trên `CreateCustomerDto` / `UpdateCustomerDto` bị filter mặc định của `[ApiController]` chặn trước khi vào `CustomerService`, trả về format `ValidationProblemDetails` khác với cấu trúc `{ "error", "errors" }` thống nhất của hệ thống.
*   **Nguyên nhân gốc:** Khai báo trùng `[Required]` ở DTO gây xung đột với logic kiểm tra trong `CustomerService`.
*   **Cách sửa:** Gỡ bỏ `[Required]` trong [`CustomerDtos.cs`](../DTOs/CustomerDtos.cs) để mọi lỗi thiếu tên hoặc chỉ chứa khoảng trắng đều do [`CustomerService.cs`](../Services/CustomerService.cs) ném `BusinessRuleException` (`400 Bad Request`) theo đúng định dạng `{ error, errors }`.

### Lỗi 3: Nguy cơ Race Condition khi hai request `Submit` đến đồng thời (`TC08b` / Slide 20 `TC06`)
*   **Cách tái hiện:** Gửi song song 2 HTTP request `POST /api/contracts/{id}/submit` cùng lúc (bằng `Task.WhenAll`) cho cùng một hợp đồng `Draft`.
*   **Hành vi lỗi trước khi sửa:** Cả hai request cùng đọc DB khi hợp đồng còn ở `Draft` trước khi `SaveChangesAsync()` hoàn tất, dẫn đến cả hai cùng vượt qua kiểm tra trạng thái và ghi đè `CurrentApprovalId` (phát sinh xử lý trùng). Ngoài ra, `approvalId` dùng định dạng `yyyyMMddHHmmss` chỉ phân biệt tới giây nên hai lần xử lý trong cùng một giây dễ trùng mã.
*   **Nguyên nhân gốc:** Thiếu cơ chế khóa đồng bộ theo từng `ContractId` trong `ContractService.SubmitContractAsync`.
*   **Cách sửa:** Bổ sung `ConcurrentDictionary<int, SemaphoreSlim> _submitLocks` để tuần tự hóa thao tác gửi theo từng `contractId` và đổi timestamp của `approvalId` sang `yyyyMMddHHmmssfff` (độ chính xác mili-giây) trong [`ContractService.cs`](../Services/ContractService.cs).

### Lỗi 4: Cho phép cập nhật `ContractNo` thành chuỗi rỗng/khoảng trắng khi sửa hợp đồng `Draft`
*   **Cách tái hiện:** Gửi `PUT /api/contracts/{id}` với `{"contractNo": "   "}`.
*   **Hành vi lỗi trước khi sửa:** Điều kiện `if (!string.IsNullOrWhiteSpace(dto.ContractNo))` âm thầm bỏ qua giá trị rỗng và trả về `200 OK` thay vì báo lỗi đầu vào không hợp lệ.
*   **Cách sửa:** Phân tách kiểm tra `if (dto.ContractNo != null)` $\rightarrow$ nếu `string.IsNullOrWhiteSpace(dto.ContractNo)` thì ném `BusinessRuleException` (`400 Bad Request`), đồng thời vẫn giữ nguyên hỗ trợ cập nhật từng phần khi client không truyền `contractNo` (`null`).

> **Trạng thái commit các bản sửa lỗi:** Các chỉnh sửa trên mã nguồn API (`DTOs/ContractDtos.cs`, `DTOs/CustomerDtos.cs`, `Services/ContractService.cs`, `Program.cs`, `QTKD.Api.csproj`) và toàn bộ bộ test Tuần 07 (`tests/QTKD.Tests/`, `docs/Tuan_07_Bao_Cao.md`, `README.md`) đang ở trạng thái sẵn sàng để commit tách biệt rõ ràng giữa **commit sửa lỗi** và **commit bổ sung kiểm thử Tuần 07**.

---

## 5. GIỚI HẠN HIỆN TẠI & LỖI CÒN TỒN TẠI (BƯỚC 18)

Sau khi hoàn thành sửa lỗi, đã chạy kiểm chứng lại toàn bộ **luồng demo Tuần 06** cả qua máy chủ Web API thật (`QTKD.Api.dll` + Swagger `200 OK` + chuỗi gọi API `Customer -> Contract Draft -> Submit 400 -> PUT 200 -> Submit 200 PendingApproval -> GET xác nhận DB -> PUT 409 CTR-01`) lẫn qua test tự động `Week06DemoFlow_CustomerToSubmit_ShouldPassEndToEnd`.

Các **giới hạn thiết kế của phiên bản hiện tại** (đúng theo phạm vi Tuần 05–07 của đề bài):
1. **Mô phỏng phân quyền bằng header `X-User-Role`:** Chưa tích hợp ASP.NET Identity / JWT Bearer đầy đủ; việc xác thực quyền `Sales` ở **AC01** đọc từ header `X-User-Role` để thuận tiện kiểm thử trên Swagger/xUnit.
2. **Mô phỏng quy trình Approval:** Khi Submit thành công, hệ thống chuyển trạng thái sang `PendingApproval` và lưu `CurrentApprovalId` / `LastIdempotencyKey` trực tiếp trên bảng `Contracts` (chưa xây dựng bảng `ApprovalHistories` và các user story duyệt/từ chối của cấp quản lý).
3. **Đồng bộ phân tán (Multi-instance):** Khóa `_submitLocks` dùng `SemaphoreSlim` trong bộ nhớ (in-memory) đáp ứng tốt cho kiến trúc single-instance của đồ án; nếu triển khai nhiều instance API song song trong thực tế cần nâng cấp lên Optimistic Concurrency Token (`RowVersion`) trong PostgreSQL hoặc Distributed Lock.

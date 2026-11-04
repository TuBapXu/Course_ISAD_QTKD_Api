# BÁO CÁO THỰC HÀNH TUẦN 06 — HỆ THỐNG QTKD
**Môn học:** Phát triển ứng dụng Hệ thống Thông tin Hiện đại  
**Công nghệ:** C# · ASP.NET Core 10 Web API · Entity Framework Core · PostgreSQL  

---

## 1. BẢNG ĐỐI CHIẾU QUY TẮC NGHIỆP VỤ & ACCEPTANCE CRITERIA VỚI MÃ NGUỒN

| Mã quy tắc / AC | Ý nghĩa nghiệp vụ | File / Đoạn mã xử lý tương ứng | Kết quả HTTP |
|---|---|---|---|
| **CTR-01** | Chỉ cho phép chỉnh sửa hợp đồng ở trạng thái `Draft` hoặc `RevisionRequested`. Đang ở `PendingApproval` bị từ chối sửa. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L64-L71) (`UpdateAsync`) | `409 Conflict` |
| **CTR-02** | Chỉ Submit khi: Khách hàng hợp lệ (Active), ngày hiệu lực hợp lệ (`EndDate > StartDate`), đủ tệp bắt buộc (`FileUrl`) và đủ nội dung có Quốc hiệu. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L145-L177) (`SubmitContractAsync`) | `400 Bad Request` (trả danh sách lỗi) |
| **CTR-03** | Không được chuyển trực tiếp `Draft` sang `Approved`. Submit thành công chỉ chuyển sang `PendingApproval`. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L179-L192) (`SubmitContractAsync`) | `200 OK` (`status: "PendingApproval"`) |
| **AC01** | Người dùng không có quyền gửi (không phải Sales) $\rightarrow$ Từ chối và không đổi hợp đồng. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L118-L125) (`SubmitContractAsync`) | `403 Forbidden` |
| **AC02** | Hợp đồng không ở trạng thái cho phép gửi $\rightarrow$ Từ chối thao tác. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L140-L146) (`SubmitContractAsync`) | `409 Conflict` |
| **AC03** | Không đáp ứng CTR-02 $\rightarrow$ Không tạo quy trình phê duyệt, trả danh sách điều kiện chưa đạt. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L168-L174) & [`GlobalExceptionMiddleware.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Exceptions/GlobalExceptionMiddleware.cs#L36-L45) | `400 Bad Request` kèm mảng `errors` |
| **AC04** | Đáp ứng đầy đủ điều kiện $\rightarrow$ Tạo quy trình phê duyệt (`approvalId`) và chuyển sang `PendingApproval`. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L179-L192) (`SubmitContractAsync`) | `200 OK` |
| **AC05** | Gửi lặp / cạnh tranh đồng thời với cùng `Idempotency-Key` $\rightarrow$ Không tạo phiên phê duyệt mới, trả lại phiên cũ. | [`ContractService.cs`](file:///c:/Users/HP/Desktop/C++CodeLearn/HK1-2627/Phat_Trien_UD_HTTT_Hien_Dai/QTKD.Api/Services/ContractService.cs#L129-L141) (`SubmitContractAsync`) | `200 OK` (giữ nguyên `approvalId` cũ) |

---

## 2. DANH SÁCH ENDPOINTS API TUẦN 06 & REQUEST / RESPONSE MẪU

### 2.1 Quản lý Khách hàng (Customer)

#### `POST /api/customers`
*   **Mô tả:** Tạo mới khách hàng.
*   **Request Body:**
    ```json
    {
      "name": "Công ty TNHH Minh Phát Logistics",
      "status": "Active"
    }
    ```
*   **Response (201 Created):**
    ```json
    {
      "id": 1,
      "name": "Công ty TNHH Minh Phát Logistics",
      "status": "Active"
    }
    ```

#### `GET /api/customers`
*   **Mô tả:** Lấy danh sách tất cả khách hàng.
*   **Response (200 OK):** Mảng danh sách khách hàng.

#### `PUT /api/customers/{id}`
*   **Mô tả:** Cập nhật thông tin khách hàng.

---

### 2.2 Quản lý Bảng giá & Dịch vụ (Pricing)

#### `POST /api/prices`
*   **Mô tả:** Tạo bảng giá mới.
*   **Request Body:**
    ```json
    {
      "name": "Bảng giá Cảng biển 2026",
      "effectiveDate": "2026-06-01T00:00:00Z"
    }
    ```

#### `POST /api/prices/{id}/items`
*   **Mô tả:** Thêm đơn giá dịch vụ vào bảng giá (Kiểm tra `UnitPrice > 0`).
*   **Request Body:**
    ```json
    {
      "serviceCode": "PORT_STORAGE",
      "unitPrice": 1500000
    }
    ```
*   **Response Lỗi (400 Bad Request nếu đơn giá <= 0):**
    ```json
    {
      "error": "Đơn giá dịch vụ phải lớn hơn 0."
    }
    ```

---

### 2.3 Quản lý Hợp đồng & US03 (Contract & Submit)

#### `POST /api/contracts`
*   **Mô tả:** Tạo hợp đồng mới ở trạng thái `Draft` (Kiểm tra Customer tồn tại & `EndDate > StartDate`).
*   **Request Body:**
    ```json
    {
      "contractNo": "HD-2026-018",
      "customerId": 1,
      "startDate": "2026-01-01T00:00:00Z",
      "endDate": "2027-01-01T00:00:00Z",
      "fileUrl": null,
      "contractContent": null
    }
    ```

#### `PUT /api/contracts/{id}`
*   **Mô tả:** Sửa hợp đồng (Chỉ sửa được khi đang `Draft` hoặc `RevisionRequested` theo **CTR-01**).
*   **Request Body:**
    ```json
    {
      "fileUrl": "https://storage.qtkd.vn/contracts/HD-2026-018.pdf",
      "contractContent": "Cộng hòa xã hội chủ nghĩa Việt Nam. Hợp đồng cung cấp dịch vụ cảng biển..."
    }
    ```
*   **Response Lỗi (409 Conflict nếu sửa khi đang `PendingApproval`):**
    ```json
    {
      "error": "Vi phạm CTR-01: Không thể sửa hợp đồng ở trạng thái 'PendingApproval'. Chỉ được sửa khi ở trạng thái Draft hoặc RevisionRequested."
    }
    ```

#### `POST /api/contracts/{id}/submit` (Trọng tâm US03)
*   **Headers:**
    *   `X-User-Role: Sales` (Bắt buộc quyền Sales theo **AC01**)
    *   `Idempotency-Key: KEY-001` (Khóa chống gửi lặp theo **AC05**)
*   **Request Body (Tùy chọn):**
    ```json
    {
      "comment": "Trình duyệt hợp đồng HD-2026-018 đã hoàn thiện đầy đủ hồ sơ."
    }
    ```
*   **Trường hợp 1 — Thất bại do thiếu CTR-02 (400 Bad Request):**
    ```json
    {
      "error": "Không đáp ứng quy tắc nghiệp vụ",
      "errors": [
        "Thiếu tệp đính kèm bắt buộc (FileUrl không được để trống).",
        "Nội dung hợp đồng bắt buộc chưa đầy đủ (phải có Quốc hiệu 'Cộng hòa xã hội chủ nghĩa Việt Nam')."
      ]
    }
    ```
*   **Trường hợp 2 — Thất bại do không có quyền (403 Forbidden):**
    ```json
    {
      "error": "Vi phạm AC01: Người dùng không có quyền gửi hợp đồng trình duyệt. Yêu cầu quyền Nhân viên Kinh doanh (Sales)."
    }
    ```
*   **Trường hợp 3 — Thành công hợp lệ (200 OK):**
    ```json
    {
      "contractId": 1,
      "status": "PendingApproval",
      "approvalId": "APP-1-20261104100201"
    }
    ```
*   **Trường hợp 4 — Gửi lặp cùng `Idempotency-Key` (200 OK):**
    Hệ thống trả về chính kết quả phiên phê duyệt đã tạo trước đó, **không sinh thêm phiên xử lý trùng (AC05)**.
*   **Trường hợp 5 — Gửi lại khi hợp đồng đã ở `PendingApproval` mà không có key (409 Conflict):**
    ```json
    {
      "error": "Vi phạm AC02: Hợp đồng đã ở trạng thái Chờ phê duyệt (PendingApproval), không thể gửi lại."
    }
    ```

---

## 3. KỊCH BẢN DEMO BẮT BUỘC THEO ĐỀ BÀI (MỤC 13)

Thứ tự thực hiện demo chuẩn để quay video / chụp ảnh nộp bài:
1. **Tạo Customer:** `POST /api/customers` $\rightarrow$ Nhận Customer ID.
2. **Tạo Contract Draft:** `POST /api/contracts` với `fileUrl: null`, `contractContent: null`.
3. **Gửi khi thiếu dữ liệu (Thất bại):** `POST /api/contracts/{id}/submit` $\rightarrow$ Trả về mã lỗi **400 Bad Request** kèm danh sách thiếu tệp và thiếu nội dung Quốc hiệu (CTR-02).
4. **Bổ sung dữ liệu:** `PUT /api/contracts/{id}` bổ sung `fileUrl` và `contractContent` hợp lệ.
5. **Gửi lại (Thành công):** `POST /api/contracts/{id}/submit` $\rightarrow$ Trả về **200 OK**, trạng thái chuyển thành **PendingApproval**, sinh mã `approvalId`.
6. **Xem trạng thái trong DB:** `GET /api/contracts/{id}` xác nhận trạng thái đã đổi thành `PendingApproval`.
7. **Thử nghiệm mở rộng:**
    *   Thử gửi submit lần nữa không có key $\rightarrow$ Bị chặn lỗi **409 Conflict** (AC02).
    *   Thử sửa hợp đồng vừa chuyển PendingApproval $\rightarrow$ Bị chặn lỗi **409 Conflict** (CTR-01).

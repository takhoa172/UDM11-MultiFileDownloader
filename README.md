# UDM_11 - Ứng dụng GUI kéo thả & download nhiều file từ Server

## Thành viên

| STT | MSSV | Họ và tên | Vai trò |
|---:|---|---|---|
| 1 | 079206048355 | Nguyễn Đức Duy | Core TCP Socket, Protocol & Message, Test Case, Functional/Disconnect Test, Stress Test (Task 1–6). **Tính năng mới:** quản lý phiên (`SessionManager`), logout, chặn đăng nhập trùng, khóa `NetworkService`, vòng lặp reconnect. |
| 2 | 08020600023 | Nguyễn Trường Duy | DownloadManager (hàng đợi/tải đồng thời), Tiến trình & Tốc độ, Client File Saver, Xử lý file trùng, Cách ly ngoại lệ (Task 17–21). **Tính năng mới:** `UploadManager`, `SettingPage`/`AccountPage`. |
| 3 | 089206002578 | Dương Đăng Khoa | UI Layout & Trạng thái, Hiển thị danh sách file, Kéo-thả, UI Non-blocking, Trạng thái lỗi lên UI (Task 12–16). **Tính năng mới:** Sidebar, `Login/Register/Forgot`, trang ManageFile, MainForm. |
| 4 | 089205002762 | Trịnh Anh Khoa | Quản trị Git & Review, Integrity Engine (Hash SHA-256), Console Dashboard, Bandwidth Limiter, Server Logging, Data Validation (Task 22–27). **Tính năng mới:** Auth (`UserStore` PBKDF2+salt, `AuthHandler`, verify mật khẩu cũ), `BandwidthManager`, `SettingHandler`, tích hợp + fix. |
| 5 | 060206014874 | Võ Trương Khánh Huy | Server File Scanner & List, Quy tắc file trùng, Server Streamer Engine, Timeout & Giải phóng Socket, Xử lý lỗi file độc lập (Task 7–11). **Tính năng mới:** `UploadHandler`, `FileManager`. |

## Giới thiệu

**Mục tiêu:** Xây dựng ứng dụng desktop (GUI WinForms) cho phép người dùng **đăng nhập**, xem danh sách file trên Server và **kéo-thả một hoặc nhiều file** sang khu vực download để tải về máy (mỗi file có tiến trình, tốc độ, trạng thái riêng); **upload** file lên Server; **quản lý file** (đổi tên/xóa) và **cài đặt** tốc độ/số file tải.

**Đối tượng sử dụng:** người dùng cần tải/upload nhiều file với một Server nội bộ.

**Phạm vi:** Client (WinForms) và Server (Console) giao tiếp qua **TCP Socket thật** (2 tiến trình riêng biệt). Có xác thực (đăng ký/đăng nhập/đổi mật khẩu), upload (giới hạn 3GB), quản lý file, cài đặt. Không bao gồm Pause/Resume (thuộc UDM_13).

## Kiến trúc hệ thống

- **Mô hình:** Client–Server (TCP)
- **Protocol:** JSON-line — mỗi gói tin là **1 dòng JSON**, kết thúc bằng ký tự `\n`
- **Port mặc định:** `8080` (cấu hình tại `Code/Server/appsettings.json`)
- **Danh sách lệnh** (`PacketCommand`):
  `GET_LIST`, `DOWNLOAD_REQ`, `FILE_CHUNK`, `ERROR_RESP`, `PING`, `PONG`,
  `REGISTER`, `LOGIN`, `CHANGE_PASSWORD`, `AUTH_RESP`,
  `UPLOAD_REQ`, `UPLOAD_CHUNK`, `UPLOAD_DONE`,
  `RENAME_FILE`, `DELETE_FILE`, `SET_RATE_LIMIT`, `SETTING_RESP`, `LOGOUT`.
- **Cấu trúc message** (`ProtocolPacket`):

  | Field | Ý nghĩa |
  |---|---|
  | `Command` | Lệnh (xem danh sách trên) |
  | `FileName` / `NewFileName` | Tên file (yêu cầu/tải/đổi tên) |
  | `Username` / `PasswordHash` / `NewPasswordHash` | Tài khoản & mật khẩu (đã băm) |
  | `Token` | Mã phiên sau đăng nhập |
  | `Success` | Kết quả thành công/thất bại |
  | `RequestedRateBytesPerSecond` | Tốc độ client yêu cầu |
  | `ChunkIndex` / `TotalChunks` | Chỉ số & tổng số chunk (upload) |
  | `DataBase64` | Nội dung dữ liệu mã hóa Base64 |
  | `IsLastChunk` | Đánh dấu chunk cuối |
  | `FileHash` | SHA-256 nội dung file (xác thực toàn vẹn) |
  | `TotalSize` | Tổng kích thước file |
  | `ErrorCode` / `Message` | Mã lỗi và mô tả (khi `ERROR_RESP`) |

- **Luồng hoạt động:** Client → `ConnectionForm` (nhập IP/Port) → `LoginForm` (đăng nhập/đăng ký/đổi MK) → `MainForm` (Sidebar: **Download / Manage File / Setting**) → tải/upload/đổi tên/xóa/cài đặt. Server xác thực phiên bằng `Token`.

## Yêu cầu môi trường

- **Hệ điều hành:** Windows
- **Ngôn ngữ và phiên bản:** C# — **.NET SDK 10** (Client dùng WinForms)
- **Công cụ hoặc dependency:** .NET SDK 10; không cần thư viện ngoài (dùng thư viện chuẩn .NET)

## Cài đặt

```bash
git clone https://github.com/takhoa172/UDM11-MultiFileDownloader.git
cd UDM11-MultiFileDownloader
dotnet build Code/MultiFileDownloader.slnx
```

## Hướng dẫn chạy

### Server

```bash
dotnet run --project Code/Server
```

- Lắng nghe cổng `8080` (đổi được trong `Code/Server/appsettings.json`).
- Log in ra Console (có màu) và ghi ra `Code/Server/logs/server.log`.

### Client

```bash
dotnet run --project Code/Client
```

- Nhập **IP Server** (vd `127.0.0.1`) và **Port** (`8080`) → **đăng nhập** (hoặc đăng ký).
- Vào giao diện chính (Sidebar): **Download** (kéo-thả tải), **Manage File** (upload/đổi tên/xóa), **Setting** (cài đặt).

## Cấu hình

- **Server** — `Code/Server/appsettings.json`:
  ```json
  {
    "Server":    { "Port": 8080 },
    "RateLimit": { "TotalBytesPerSecond": 10485760 },
    "Timeout":   { "ReadMs": 300000, "WriteMs": 30000 },
    "Transfer":  { "BufferSize": 65536 }
  }
  ```
  - `TotalBytesPerSecond`: tổng băng thông Server (**10 MB/s**), **chia đều** khi nhiều client tải.
- **Client** — `Code/Client/appsettings.json`:
  ```json
  {
    "Download": { "MaxConcurrentDownloads": 3, "ConflictMode": "AutoRename", "SaveFolder": "" },
    "Network":  { "ServerIp": "127.0.0.1", "ServerPort": 8080, "RequestedRateMBps": 5 }
  }
  ```
  - `MaxConcurrentDownloads`: số file tải cùng lúc (**1–5**, chọn ở Setting).
  - `RequestedRateMBps`: tốc độ tải (**1/5/10 MB/s**, chọn ở Setting).
  - `ConflictMode`: `AutoRename` / `Overwrite` / `Skip`.
  - `SaveFolder`: để trống = `%USERPROFILE%\Downloads`.
- **Bảo mật:** không lưu password/secret trong source; mật khẩu **băm PBKDF2+salt** (Server) — client băm SHA-256 trước khi gửi.

## Chức năng

- [ ] **Account:** đăng ký / đăng nhập / đổi mật khẩu (băm PBKDF2+salt); phiên `Token`; logout; chặn đăng nhập trùng.
- [ ] Hiển thị danh sách file trên Server (Tên, Kích thước, SHA-256)
- [ ] Khu vực Download riêng biệt
- [ ] Kéo-thả 1 hoặc nhiều file sang khu vực download
- [ ] Tiến trình (%), tốc độ (MB/s), trạng thái riêng từng file
- [ ] Hàng đợi / tải đồng thời — **1–5 file** cùng lúc (cấu hình được)
- [ ] Lỗi 1 file không làm dừng các file khác
- [ ] Xử lý file trùng (tự đổi tên `(1)`, `(2)`…)
- [ ] Xác thực SHA-256 sau khi tải (sai → báo "File hỏng" + tự xóa)
- [ ] **Upload:** tải file lên Server, giới hạn **3GB**, trùng tên → báo lỗi
- [ ] **Manage File:** tìm, đổi tên, xóa, làm mới
- [ ] **Setting:** số file đồng thời (1–5), tốc độ (1/5/10 MB/s); Server **chia đều băng thông**
- [ ] Server ghi log sự kiện + Console Dashboard màu
- [ ] Chặn gói tin rác / tên file không hợp lệ
- [ ] Heartbeat giữ kết nối + **ngắt kết nối → dừng tải** (không tự tải tiếp)

## Kiểm thử

- **Functional test:** đăng ký/đăng nhập/đổi MK; kéo-thả tải 1/nhiều file; tiến trình/tốc độ; đổi tên file trùng; upload (≤3GB, >3GB, trùng tên); rename/delete/refresh; setting.
- **Test dữ liệu không hợp lệ:** gói rác, tên `../`, lệnh lạ → Server trả mã lỗi, không crash.
- **Test mất kết nối:** ngắt kết nối giữa chừng → **dừng tải** + xóa file dở; Server vẫn hoạt động.
- **Stress test:** Mức 1 (2 client – 5 file), Mức 2 (5 client – 20 file).
- **Performance test:** đo MB/s, CPU, RAM ở 2 mức tải.
- **Bandwidth test:** nhiều client cùng tải → Server chia đều.

Bằng chứng kiểm thử lưu tại `Extra/` (xem các file `Test-Case-*.md`).

## Demo

- Video: *(dán link Public/Unlisted khi có)*
- Slide: `PPTX/`
- Báo cáo: `DOCX/`

## Giới hạn

- **Không hỗ trợ Pause/Resume** (thuộc UDM_13) — file lỗi phải tải lại từ đầu.
- Server phục vụ file trong thư mục `Storage`; danh sách file mẫu phục vụ demo.
- Kết nối TCP **chưa mã hóa (không TLS)**; có xác thực đăng nhập (mật khẩu băm).
- Upload giới hạn **3GB**.
- Khi mất kết nối thật giữa chừng: **dừng tải**, không tự tải tiếp.

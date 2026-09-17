# UDM_11 - Ứng dụng GUI kéo thả & download nhiều file từ Server

## Thành viên

| STT | MSSV | Họ và tên | Vai trò |
|---:|---|---|---|
| 1 | 079206048355 | Nguyễn Đức Duy | Core TCP Socket, Protocol & Message, Test Case, Functional/Disconnect Test, Stress Test (Task 1–6) |
| 2 | 08020600023 | Nguyễn Trường Duy | DownloadManager (hàng đợi/tải đồng thời), Tiến trình & Tốc độ, Client File Saver, Xử lý file trùng, Cách ly ngoại lệ (Task 17–21) |
| 3 | 089206002578 | Dương Đăng Khoa | UI Layout & Trạng thái, Hiển thị danh sách file, Kéo-thả (Drag & Drop), UI Non-blocking, Trạng thái lỗi lên UI (Task 12–16) |
| 4 | 089205002762 | Trịnh Anh Khoa | Quản trị Git & Review, Integrity Engine (Hash SHA-256), Console Dashboard, Bandwidth Limiter, Server Logging, Data Validation (Task 22–27) |
| 5 | 060206014874 | Võ Trương Khánh Huy | Server File Scanner & List, Quy tắc file trùng, Server Streamer Engine, Timeout & Giải phóng Socket, Xử lý lỗi file độc lập (Task 7–11) |

## Giới thiệu

**Mục tiêu:** Xây dựng ứng dụng desktop (GUI WinForms) cho phép người dùng xem danh sách file trên Server và **kéo-thả một hoặc nhiều file** sang khu vực download để tải về máy; mỗi file có tiến trình, tốc độ và trạng thái riêng.

**Đối tượng sử dụng:** người dùng cần tải nhiều file từ một Server nội bộ.

**Phạm vi:** Client (WinForms) và Server (Console) giao tiếp qua **TCP Socket thật** (2 tiến trình riêng biệt). Không bao gồm Pause/Resume (thuộc UDM_13).

## Kiến trúc hệ thống

- **Mô hình:** Client–Server (TCP)
- **Protocol:** JSON-line — mỗi gói tin là **1 dòng JSON**, kết thúc bằng ký tự `\n`
- **Port mặc định:** `8080` (cấu hình tại `Code/Server/appsettings.json`)
- **Cấu trúc message** (`ProtocolPacket`):

  | Field | Ý nghĩa |
  |---|---|
  | `Command` | `GET_LIST`, `DOWNLOAD_REQ`, `FILE_CHUNK`, `ERROR_RESP`, `PING`, `PONG` |
  | `FileName` | Tên file (yêu cầu / tải) |
  | `ErrorCode` / `Message` | Mã lỗi và mô tả (khi `ERROR_RESP`) |
  | `DataBase64` | Nội dung dữ liệu mã hóa Base64 |
  | `IsLastChunk` | Đánh dấu chunk cuối |
  | `FileHash` | SHA-256 nội dung file (xác thực toàn vẹn) |
  | `TotalSize` | Tổng kích thước file |

- **Luồng hoạt động:** Client nhập IP/Port → kết nối → `GET_LIST` → Server trả danh sách file → Client kéo file sang vùng Download → `DOWNLOAD_REQ` → Server gửi `FILE_CHUNK` theo buffer → Client ghi file + xác thực SHA-256 → hoàn thành.

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

- Nhập **IP Server** (vd `127.0.0.1`) và **Port** (`8080`) → form hiện danh sách file bên trái.
- **Kéo file** từ bảng trái sang khu vực Download bên phải để bắt đầu tải.

## Cấu hình

- **Server** — `Code/Server/appsettings.json`:
  ```json
  {
    "Server":    { "Port": 8080 },
    "RateLimit": { "BytesPerSecond": 5242880, "MaxBurstBytes": 262144 },
    "Timeout":   { "ReadMs": 30000, "WriteMs": 30000 },
    "Transfer":  { "BufferSize": 65536 }
  }
  ```
- **Client** — `Code/Client/appsettings.json`:
  ```json
  {
    "Download": { "MaxConcurrentDownloads": 3, "ConflictMode": "AutoRename", "SaveFolder": "" },
    "Network":  { "ConnectTimeoutMs": 5000, "ReadTimeoutMs": 30000, "WriteTimeoutMs": 30000 }
  }
  ```
  - `MaxConcurrentDownloads`: số file tải cùng lúc (mặc định **3**).
  - `ConflictMode`: `AutoRename` / `Overwrite` / `Skip`.
  - `SaveFolder`: để trống = `%USERPROFILE%\Downloads`.
- **Client (UI):** nhập IP/Port trực tiếp trên giao diện.
- **Bảo mật:** không lưu password/secret trong source; dữ liệu demo là giả lập.

## Chức năng

- [ ] Hiển thị danh sách file trên Server (Tên, Kích thước, SHA-256)
- [ ] Khu vực Download riêng biệt
- [ ] Kéo-thả 1 hoặc nhiều file sang khu vực download
- [ ] Tiến trình (%), tốc độ (MB/s), trạng thái riêng từng file
- [ ] Hàng đợi / tải đồng thời — giới hạn **3 file** cùng lúc (cấu hình được)
- [ ] Lỗi 1 file không làm dừng các file khác
- [ ] Xử lý file trùng (tự đổi tên `(1)`, `(2)`…)
- [ ] Xác thực SHA-256 sau khi tải (sai → báo "File hỏng" + tự xóa)
- [ ] Server ghi log sự kiện + Console Dashboard màu
- [ ] Giới hạn băng thông (RateLimiter ~5 MB/s)
- [ ] Chặn gói tin rác / tên file không hợp lệ
- [ ] Heartbeat giữ kết nối + phát hiện mất kết nối

## Kiểm thử

- **Functional test:** kéo-thả tải 1/nhiều file; tiến trình/tốc độ; đổi tên file trùng.
- **Test dữ liệu không hợp lệ:** gói rác, tên `../`, lệnh lạ → Server trả mã lỗi, không crash.
- **Test mất kết nối:** ngắt kết nối giữa chừng → file báo lỗi + xóa file dở; Server vẫn hoạt động.
- **Stress test:** Mức 1 (2 client – 5 file), Mức 2 (5 client – 20 file).
- **Performance test:** đo MB/s, CPU, RAM ở 2 mức tải.

Bằng chứng kiểm thử lưu tại `Extra/` (xem các file `Test-Case-*.md`).

## Demo

- Video: *(dán link Public/Unlisted khi có)*
- Slide: `PPTX/`
- Báo cáo: `DOCX/`

## Giới hạn

- **Không hỗ trợ Pause/Resume** (thuộc UDM_13) — file lỗi phải tải lại từ đầu.
- Server phục vụ file trong thư mục `Storage`; danh sách file mẫu phục vụ demo.
- Kết nối TCP **chưa mã hóa (không TLS)**, chưa có đăng nhập/xác thực.
- Khi mất kết nối thật giữa chừng: tải thất bại (không tự tải tiếp), trừ khi TCP kịp truyền lại khi ngắt ngắn.

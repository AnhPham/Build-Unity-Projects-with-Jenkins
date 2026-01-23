# Hướng dẫn cấu hình Google Drive Service Account

Hướng dẫn này sẽ giúp bạn cấu hình Google Drive Service Account để tự động upload APK lên Google Drive trong Jenkins pipeline.

## Mục tiêu

- Upload APK tự động lên Google Drive của tài khoản `ni@zenga.com.vn`
- Thư mục đích: `Works/Projects/Coffee Mania/Build`
- Sử dụng Service Account để không cần xác thực thủ công

## Bước 1: Tạo Google Cloud Project

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Đăng nhập bằng tài khoản `ni@zenga.com.vn`
3. Tạo một project mới hoặc chọn project có sẵn:
   - Click vào dropdown project ở đầu trang
   - Click "New Project"
   - Đặt tên project (ví dụ: "Coffee Mania Build Upload")
   - Click "Create"

## Bước 2: Bật Google Drive API

1. Trong Google Cloud Console, vào **APIs & Services** > **Library**
2. Tìm kiếm "Google Drive API"
3. Click vào "Google Drive API"
4. Click nút **Enable** để bật API

## Bước 3: Tạo Service Account

1. Vào **APIs & Services** > **Credentials**
2. Click **Create Credentials** > **Service Account**
3. Điền thông tin:
   - **Service account name**: `coffeemania-build-uploader` (hoặc tên bạn muốn)
   - **Service account ID**: sẽ tự động tạo (có thể giữ nguyên)
   - **Description**: `Service account for uploading APK builds to Google Drive`
4. Click **Create and Continue**
5. Bước **Grant this service account access to project** (tùy chọn):
   - Có thể bỏ qua hoặc chọn role `Editor` nếu cần
   - Click **Continue**
6. Bước **Grant users access to this service account** (bỏ qua):
   - Click **Done**

## Bước 4: Tạo và tải JSON Key

1. Trong danh sách Service Accounts, click vào service account vừa tạo
2. Vào tab **Keys**
3. Click **Add Key** > **Create new key**
4. Chọn format **JSON**
5. Click **Create**
6. File JSON sẽ tự động tải về máy (tên file thường là: `project-name-xxxxx.json`)

⚠️ **QUAN TRỌNG**: Giữ file JSON này an toàn, không commit vào git!

## Bước 5: Chia sẻ thư mục Google Drive với Service Account

1. Mở file JSON vừa tải về, tìm trường `client_email` (ví dụ: `coffeemania-build-uploader@project-id.iam.gserviceaccount.com`)
2. Đăng nhập vào [Google Drive](https://drive.google.com) bằng tài khoản `ni@zenga.com.vn`
3. Tạo hoặc tìm thư mục `Works/Projects/Coffee Mania/Build`:
   - Nếu chưa có, tạo thư mục `Works` > `Projects` > `Coffee Mania` > `Build`
4. Right-click vào thư mục `Build` (hoặc thư mục cuối cùng trong đường dẫn)
5. Click **Share** (Chia sẻ)
6. Trong ô "Add people and groups", nhập email của service account (từ bước 1)
7. Chọn quyền **Editor** (Người chỉnh sửa)
8. **Bỏ tick** "Notify people" (Không cần gửi email)
9. Click **Share** (Chia sẻ)

## Bước 6: Cấu hình trong Jenkins

Có 3 cách để cấu hình credentials trong Jenkins. **Jenkinsfile đã được cấu hình để tự động thử các cách theo thứ tự ưu tiên.**

### Cách 1: Sử dụng Jenkins Credentials Store (Khuyến nghị - Tự động)

✅ **Jenkinsfile đã được cấu hình sẵn để tự động sử dụng Jenkins Credentials Store!**

1. Vào Jenkins Dashboard > **Manage Jenkins** > **Credentials**
2. Chọn domain (thường là `Global`)
3. Click **Add Credentials**
4. Điền thông tin:
   - **Kind**: `Secret text`
   - **Secret**: Mở file JSON đã tải về, copy toàn bộ nội dung và paste vào đây
   - **ID**: `google-drive-service-account-key` (⚠️ **Phải đúng ID này** hoặc set environment variable `GOOGLE_DRIVE_CREDENTIALS_ID`)
   - **Description**: `Google Drive Service Account JSON for Builds upload`
5. Click **OK**

6. **Không cần chỉnh sửa Jenkinsfile** - Nó sẽ tự động tìm và sử dụng credentials này!

   Nếu muốn dùng ID khác, set environment variable trong Jenkins:
   - **Name**: `GOOGLE_DRIVE_CREDENTIALS_ID`
   - **Value**: ID của credentials bạn muốn sử dụng

### Cách 2: Environment Variable trong Jenkins

1. Vào Jenkins Dashboard > **Manage Jenkins** > **Configure System**
2. Tìm phần **Global properties** > **Environment variables**
3. Click **Add**
4. Điền:
   - **Name**: `GOOGLE_DRIVE_SERVICE_ACCOUNT_JSON`
   - **Value**: Mở file JSON, copy toàn bộ nội dung và paste vào đây
5. Click **Save**

### Cách 3: Đặt file JSON trong workspace

1. Upload file JSON vào Jenkins server
2. Đặt file tại: `$JENKINS_HOME/workspace/[project-name]/service_account.json`
3. Hoặc đặt trong project root và commit vào git (⚠️ **KHÔNG KHUYẾN NGHỊ** vì lý do bảo mật)

## Thứ tự ưu tiên của Jenkinsfile

Jenkinsfile sẽ tự động thử các cách theo thứ tự sau:

1. ✅ **Jenkins Credentials Store** (credentials ID: `google-drive-service-account-key` hoặc từ env var `GOOGLE_DRIVE_CREDENTIALS_ID`)
2. ✅ **Environment variable**: `GOOGLE_DRIVE_SERVICE_ACCOUNT_JSON` (JSON string)
3. ✅ **Environment variable**: `GOOGLE_DRIVE_SERVICE_ACCOUNT_FILE` (đường dẫn đến file JSON)
4. ✅ **File**: `service_account.json` trong workspace root

Nếu không tìm thấy credentials ở bất kỳ cách nào, build sẽ fail với thông báo hướng dẫn chi tiết.

## Bước 7: Kiểm tra cấu hình

1. Chạy Jenkins build với:
   - **BUILD_TARGET**: `Android` hoặc `Both Android iOS`
   - **UPLOAD_TO_GOOGLE_DRIVE**: ✅ Checked
2. Kiểm tra console output để xem:
   - ✅ Service account đã authenticate thành công
   - ✅ Thư mục đã được tìm thấy/tạo thành công
   - ✅ APK đã được upload thành công

## Xử lý lỗi thường gặp

### Lỗi: "Service account credentials not found"
- **Nguyên nhân**: Chưa cấu hình credentials
- **Giải pháp**: Kiểm tra lại Bước 6, đảm bảo đã set environment variable hoặc credentials

### Lỗi: "Failed to authenticate"
- **Nguyên nhân**: JSON key không hợp lệ hoặc đã bị thu hồi
- **Giải pháp**: Tạo lại key mới (Bước 4) và cập nhật credentials

### Lỗi: "Permission denied" hoặc "Folder not found"
- **Nguyên nhân**: Service account chưa được chia sẻ quyền truy cập
- **Giải pháp**: Kiểm tra lại Bước 5, đảm bảo đã share thư mục với email của service account

### Lỗi: "Module not found" hoặc "ImportError"
- **Nguyên nhân**: Python packages chưa được cài đặt
- **Giải pháp**: Script sẽ tự động cài đặt, nhưng nếu vẫn lỗi, chạy thủ công:
  ```bash
  pip3 install google-auth google-auth-oauthlib google-auth-httplib2 google-api-python-client
  ```

## Bảo mật

⚠️ **LƯU Ý QUAN TRỌNG VỀ BẢO MẬT**:

1. **KHÔNG** commit file JSON vào git repository
2. **KHÔNG** chia sẻ file JSON công khai
3. Nếu file JSON bị lộ, hãy:
   - Xóa key cũ trong Google Cloud Console
   - Tạo key mới
   - Cập nhật credentials trong Jenkins
4. Sử dụng Jenkins Credentials Store thay vì hardcode trong code
5. Chỉ chia sẻ quyền truy cập vào thư mục cần thiết, không chia sẻ toàn bộ Drive

## Tài liệu tham khảo

- [Google Drive API Documentation](https://developers.google.com/drive/api/v3/about-sdk)
- [Service Accounts Overview](https://cloud.google.com/iam/docs/service-accounts)
- [Jenkins Credentials Plugin](https://plugins.jenkins.io/credentials-binding/)

## Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. Console log của Jenkins build
2. Google Cloud Console > APIs & Services > Credentials
3. Google Drive > Shared with me (để xem service account đã được share chưa)

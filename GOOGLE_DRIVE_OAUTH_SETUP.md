# Hướng dẫn cấu hình Google Drive OAuth 2.0

Hướng dẫn này sẽ giúp bạn cấu hình Google Drive OAuth 2.0 để tự động upload APK lên Google Drive trong Jenkins pipeline.

## Mục tiêu

- Upload APK tự động lên Google Drive của tài khoản `katori.norikarin@gmail.com`
- Thư mục đích: `Projects/Coffee Mania/Build`
- Sử dụng OAuth 2.0 để upload vào My Drive (không cần Google Workspace)

## Tại sao OAuth 2.0?

- ✅ **Không cần Google Workspace** - Hoạt động với tài khoản Google cá nhân
- ✅ **Upload vào My Drive** - Không bị giới hạn storage quota như Service Account
- ✅ **Tự động refresh token** - Script tự động làm mới token khi hết hạn
- ⚠️ **Cần xác thực ban đầu** - Phải authenticate một lần để lấy refresh token

## Bước 1: Tạo Google Cloud Project

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Đăng nhập bằng tài khoản `katori.norikarin@gmail.com`
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

## Bước 3: Tạo OAuth 2.0 Client Credentials

1. Vào **APIs & Services** > **Credentials**
2. Click **Create Credentials** > **OAuth client ID**
3. Nếu chưa có OAuth consent screen, bạn sẽ được yêu cầu cấu hình:
   - **User Type**: Chọn "External" (hoặc "Internal" nếu có Google Workspace)
   - Click "Create"
   - **App name**: `Coffee Mania Build Uploader` (hoặc tên bạn muốn)
   - **User support email**: Chọn email của bạn
   - **Developer contact information**: Nhập email của bạn
   - Click "Save and Continue"
   - **Scopes**: Click "Add or Remove Scopes"
     - Tìm và chọn: `../auth/drive` (Google Drive API)
     - Click "Update" > "Save and Continue"
   - **Test users** (nếu là External): Thêm `katori.norikarin@gmail.com`
   - Click "Save and Continue" > "Back to Dashboard"

4. Tạo OAuth Client ID:
   - **Application type**: Chọn **Desktop app**
   - **Name**: `Coffee Mania Build Uploader` (hoặc tên bạn muốn)
   - Click **Create**

5. **Download JSON credentials**:
   - Click vào OAuth client vừa tạo
   - Click nút **Download JSON**
   - File sẽ được tải về với tên như: `client_secret_xxxxx.json`
   - **Đổi tên file thành**: `credentials_oauth.json`

⚠️ **QUAN TRỌNG**: Giữ file JSON này an toàn, không commit vào git!

## Bước 4: Xác thực lần đầu (Lấy Refresh Token)

Bạn cần chạy script một lần trên máy local để xác thực và lấy refresh token.

### Cách 1: Sử dụng script helper (Khuyến nghị)

1. **Chuẩn bị môi trường**:
   ```bash
   pip3 install google-auth google-auth-oauthlib google-auth-httplib2 google-api-python-client
   ```

2. **Đặt file credentials**:
   - Copy file `credentials_oauth.json` vào cùng thư mục với `authenticate_oauth.py`

3. **Chạy script helper**:
   ```bash
   python3 authenticate_oauth.py
   ```

4. **Xác thực trong browser**:
   - Browser sẽ tự động mở
   - Đăng nhập bằng `katori.norikarin@gmail.com`
   - Cho phép quyền truy cập Google Drive
   - Sau khi xác thực thành công, script sẽ:
     - Tạo file `token.pickle`
     - Tạo file `token_base64.txt` chứa base64 string
     - In ra hướng dẫn các bước tiếp theo

5. **Copy base64 string**:
   - Mở file `token_base64.txt` hoặc copy từ console output
   - Sử dụng string này trong Bước 6

### Cách 2: Tạo script xác thực tùy chỉnh

Nếu muốn tự tạo script:

1. Copy `credentials_oauth.json` vào workspace
2. Tạm thời comment phần kiểm tra CI/CD trong script
3. Chạy build, script sẽ mở browser để xác thực
4. Sau khi xác thực, lấy `token.pickle` và encode base64

## Bước 5: Encode Token để lưu vào Jenkins

Nếu bạn đã sử dụng script helper `authenticate_oauth.py` ở Bước 4, file `token_base64.txt` đã được tạo tự động. Bạn có thể bỏ qua bước này.

Nếu bạn tự tạo script hoặc cần encode lại, sau khi có file `token.pickle`, encode nó thành base64:

```bash
# Trên macOS/Linux
base64 -i token.pickle | tr -d '\n' > token_base64.txt

# Hoặc dùng Python
python3 -c "import base64, pickle; print(base64.b64encode(open('token.pickle', 'rb').read()).decode('utf-8'))" > token_base64.txt
```

Copy nội dung file `token_base64.txt` - đây là giá trị bạn sẽ lưu vào Jenkins.

## Bước 6: Cấu hình trong Jenkins

Có 2 cách để cấu hình credentials trong Jenkins:

### Cách 1: Sử dụng Jenkins Credentials Store (Khuyến nghị)

✅ **Jenkinsfile đã được cấu hình sẵn để tự động sử dụng Jenkins Credentials Store!**

1. **Thêm OAuth Credentials**:
   - Vào Jenkins Dashboard > **Manage Jenkins** > **Credentials**
   - Chọn domain (thường là `Global`)
   - Click **Add Credentials**
   - Điền thông tin:
     - **Kind**: `Secret text`
     - **Secret**: Mở file `credentials_oauth.json`, copy toàn bộ nội dung và paste vào đây
     - **ID**: `google-drive-oauth-credentials` (⚠️ **Phải đúng ID này** hoặc set env var `GOOGLE_DRIVE_OAUTH_CREDENTIALS_ID`)
     - **Description**: `Google Drive OAuth Client Credentials`
   - Click **OK**

2. **Thêm OAuth Token**:
   - Click **Add Credentials** lần nữa
   - Điền thông tin:
     - **Kind**: `Secret text`
     - **Secret**: Paste base64 string từ Bước 5
     - **ID**: `google-drive-oauth-token` (⚠️ **Phải đúng ID này** hoặc set env var `GOOGLE_DRIVE_OAUTH_TOKEN_ID`)
     - **Description**: `Google Drive OAuth Token (Base64 encoded)`
   - Click **OK**

3. **Không cần chỉnh sửa Jenkinsfile** - Nó sẽ tự động tìm và sử dụng credentials này!

   Nếu muốn dùng ID khác, set environment variables trong Jenkins:
   - **GOOGLE_DRIVE_OAUTH_CREDENTIALS_ID**: ID của OAuth credentials
   - **GOOGLE_DRIVE_OAUTH_TOKEN_ID**: ID của OAuth token

### Cách 2: Environment Variables trong Jenkins

1. Vào Jenkins Dashboard > **Manage Jenkins** > **Configure System**
2. Tìm phần **Global properties** > **Environment variables**
3. Click **Add** cho mỗi biến:

   **Biến 1:**
   - **Name**: `GOOGLE_DRIVE_OAUTH_CREDENTIALS`
   - **Value**: Nội dung file `credentials_oauth.json` (JSON string)

   **Biến 2:**
   - **Name**: `GOOGLE_DRIVE_OAUTH_TOKEN`
   - **Value**: Base64 string từ Bước 5

4. Click **Save**

### Cách 3: Đặt files trong workspace

1. Upload `credentials_oauth.json` vào Jenkins server
2. Upload `token.pickle` vào Jenkins server
3. Đặt cả 2 files tại: `$JENKINS_HOME/workspace/[project-name]/`
4. ⚠️ **KHÔNG KHUYẾN NGHỊ** vì lý do bảo mật

## Bước 7: Kiểm tra cấu hình

1. Chạy Jenkins build với:
   - **BUILD_TARGET**: `Android` hoặc `Both Android iOS`
   - **UPLOAD_TO_GOOGLE_DRIVE**: ✅ Checked
2. Kiểm tra console output để xem:
   - ✅ OAuth credentials đã được tìm thấy
   - ✅ Token đã được load thành công
   - ✅ Token còn hiệu lực (hoặc đã tự động refresh)
   - ✅ Thư mục đã được tìm thấy/tạo thành công
   - ✅ APK đã được upload thành công

## Xử lý lỗi thường gặp

### Lỗi: "OAuth credentials not found"
- **Nguyên nhân**: Chưa cấu hình OAuth credentials
- **Giải pháp**: Kiểm tra lại Bước 6, đảm bảo đã thêm credentials vào Jenkins

### Lỗi: "OAuth token not found or expired"
- **Nguyên nhân**: Token chưa được lưu hoặc đã hết hạn và không có refresh token
- **Giải pháp**: 
  - Chạy lại Bước 4 để xác thực lại
  - Lưu token mới vào Jenkins Credentials Store

### Lỗi: "Token expired and no refresh token available"
- **Nguyên nhân**: Refresh token đã hết hạn (thường sau 6 tháng không dùng)
- **Giải pháp**: Chạy lại Bước 4 để xác thực lại và lấy refresh token mới

### Lỗi: "Failed to refresh token"
- **Nguyên nhân**: Refresh token không hợp lệ hoặc đã bị thu hồi
- **Giải pháp**: 
  - Kiểm tra xem user có thu hồi quyền truy cập không
  - Chạy lại Bước 4 để xác thực lại

### Lỗi: "Module not found" hoặc "ImportError"
- **Nguyên nhân**: Python packages chưa được cài đặt
- **Giải pháp**: Script sẽ tự động cài đặt, nhưng nếu vẫn lỗi, chạy thủ công:
  ```bash
  pip3 install google-auth google-auth-oauthlib google-auth-httplib2 google-api-python-client
  ```

## Token và Refresh Token

### Access Token
- **Thời hạn**: ~1 giờ
- **Tự động refresh**: Script tự động làm mới khi hết hạn
- **Không cần can thiệp**: Hoạt động tự động

### Refresh Token
- **Thời hạn**: ~6 tháng (hoặc không hết hạn tùy cấu hình)
- **Cần xác thực lại**: Khi refresh token hết hạn, cần chạy lại Bước 4
- **Lưu ý**: Giữ refresh token an toàn, đây là "chìa khóa" để lấy access token mới

### Tự động Refresh
Script tự động kiểm tra và refresh token:
- ✅ Kiểm tra thời hạn token trước khi sử dụng
- ✅ Tự động refresh nếu token hết hạn nhưng còn refresh token
- ✅ Cảnh báo nếu token sắp hết hạn
- ✅ Lưu token mới sau khi refresh

## Bảo mật

⚠️ **LƯU Ý QUAN TRỌNG VỀ BẢO MẬT**:

1. **KHÔNG** commit `credentials_oauth.json` vào git repository
2. **KHÔNG** commit `token.pickle` vào git repository
3. **KHÔNG** chia sẻ base64 token công khai
4. Nếu credentials hoặc token bị lộ:
   - Xóa OAuth client trong Google Cloud Console
   - Tạo OAuth client mới
   - Xác thực lại và cập nhật credentials trong Jenkins
5. Sử dụng Jenkins Credentials Store thay vì hardcode trong code
6. Định kỳ kiểm tra và refresh token để đảm bảo không bị hết hạn

## So sánh OAuth 2.0 vs Service Account

| Tiêu chí | OAuth 2.0 | Service Account |
|----------|-----------|-----------------|
| **Cần Google Workspace** | ❌ Không | ✅ Có (cho Shared Drive) |
| **Upload vào My Drive** | ✅ Có | ❌ Không (bị giới hạn quota) |
| **Token hết hạn** | Access: 1h (tự refresh)<br>Refresh: ~6 tháng | ❌ Không hết hạn |
| **Xác thực ban đầu** | ✅ Cần (1 lần) | ❌ Không cần |
| **Phù hợp CI/CD** | ✅ Có (sau khi setup) | ✅ Có |
| **Bảo mật** | ✅ Cao | ✅ Cao |

## Tài liệu tham khảo

- [Google Drive API Documentation](https://developers.google.com/drive/api/v3/about-sdk)
- [OAuth 2.0 for Desktop Apps](https://developers.google.com/identity/protocols/oauth2/native-app)
- [Jenkins Credentials Plugin](https://plugins.jenkins.io/credentials-binding/)

## Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. Console log của Jenkins build
2. Google Cloud Console > APIs & Services > Credentials
3. Google Account > Security > Third-party apps with account access (để xem/quản lý quyền truy cập)

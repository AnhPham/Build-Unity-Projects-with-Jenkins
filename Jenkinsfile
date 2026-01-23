pipeline {
    agent any

    environment {
        UNITY_PATH = '/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity'
        PROJECT_PATH = "${env.WORKSPACE}/UnityJenkins"
        BUILD_METHOD_ANDROID = 'BuildScript.BuildAndroid'
        BUILD_METHOD_IOS = 'BuildScript.BuildiOS'
        BUILD_METHOD_MACOS = 'BuildScript.BuildMacOS'
        BUILD_METHOD_WINDOWS = 'BuildScript.BuildWindows'
        KEYSTORE_PASS = 'unityjenkins'
        KEY_ALIAS_PASS = 'unityjenkins'
        POD_PATH = '/Users/anhpham/.gem/ruby/2.6.0/bin/pod'
    }

    parameters {
        string(name: 'BRANCH', defaultValue: 'develop', description: 'Git branch to build')
        choice(name: 'BUILD_TARGET', choices: ['Both Android iOS', 'Android', 'iOS', 'Both MacOS Windows', 'MacOS', 'Windows'], description: 'Build platforms')
        booleanParam(name: 'CLEAN_BUILD', defaultValue: false, description: 'Clean build by deleting Library folder')
        choice(name: 'BUILD_ANDROID_FORMAT', choices: ['APK', 'AAB', 'Both'], description: 'Build APK, AAB or both')
        choice(name: 'BUILD_IOS_FORMAT', choices: ['AdHoc', 'AppStore', 'Both'], description: 'Build AdHoc, AppStore or both')
        booleanParam(name: 'DEVELOPMENT_BUILD', defaultValue: false, description: 'Toggle Development Build, Autoconnect Profiler.')
        string(name: 'SCRIPTING_DEFINE_SYMBOLS', defaultValue: '', description: 'Scripting defines symbols separated by commas')
        booleanParam(name: 'UPLOAD_TO_GOOGLE_DRIVE', defaultValue: false, description: 'Upload APK to Google Drive (ni@zenga.com.vn)')
    }

    options { timestamps() }

    stages {
        stage('Checkout Code') {
            steps {
                script {
                    def repoUrl = sh(
                        script: '''
                            git remote -v | head -n 1 | awk '{print $2}'
                        ''',
                        returnStdout: true
                    ).trim()

                    if (!repoUrl) {
                        error "Unable to detect Repository URL — please check SCM configuration."
                    }

                    env.GIT_PATH = repoUrl
                    echo "Detected Git remote: ${env.GIT_PATH}"
                }

                checkout([
                    $class: 'GitSCM',
                    branches: [[ name: "*/${params.BRANCH}" ]],
                    userRemoteConfigs: [[ url: "${env.GIT_PATH}" ]]
                ])
            }
        }

        stage('Clean Library (Force Reimport All)') {
            when { expression { params.CLEAN_BUILD } }
            steps {
                sh '''
                echo "🧹 Cleaning Library folder to force Reimport All..."
                rm -rf "${PROJECT_PATH}/Library"
                echo "✅ Library cleaned. Unity will reimport all assets."
                '''
            }
        }

        stage('Build Android') {
            when { expression { params.BUILD_TARGET == 'Android' || params.BUILD_TARGET == 'Both Android iOS' } }
            environment {
                BUILD_ANDROID_FORMAT = "${params.BUILD_ANDROID_FORMAT}"
                DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}"
                SCRIPTING_DEFINE_SYMBOLS = "${params.SCRIPTING_DEFINE_SYMBOLS}"
            }
            steps {
                sh '''
                echo "🔨 Starting Unity Android build..."
                echo "⚙️ DEVELOPMENT_BUILD=${DEVELOPMENT_BUILD}"
                ${UNITY_PATH} -quit -batchmode -nographics -projectPath "${PROJECT_PATH}" -logfile 'unity_build_log_android.txt' -executeMethod ${BUILD_METHOD_ANDROID} -buildTarget android
                '''
            }
        }

        stage('Tag Android Output Name if DEV') {
            when { expression { params.BUILD_TARGET == 'Android' || params.BUILD_TARGET == 'Both Android iOS' } }
            environment {
                DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}"
                SCRIPTING_DEFINE_SYMBOLS = "${params.SCRIPTING_DEFINE_SYMBOLS}"
            }
            steps {
                sh '''
                PREFIX=""
                if [ "${DEVELOPMENT_BUILD}" = "true" ]; then
                  PREFIX="DEV_BUILD_"
                fi
                
                if [ -n "${SCRIPTING_DEFINE_SYMBOLS}" ]; then
                  # Sanitize SCRIPTING_DEFINE_SYMBOLS: replace commas/semicolons with underscore, remove special chars
                  SYMBOLS_PREFIX=$(echo "${SCRIPTING_DEFINE_SYMBOLS}" | sed 's/[,;]/_/g' | sed 's/[^a-zA-Z0-9_]/_/g' | sed 's/__*/_/g' | sed 's/^_//' | sed 's/_$//')
                  if [ -n "$SYMBOLS_PREFIX" ]; then
                    PREFIX="${SYMBOLS_PREFIX}_${PREFIX}"
                  fi
                fi
                
                if [ -n "$PREFIX" ]; then
                  echo "🏷️ Prefixing Android artifacts with ${PREFIX} ..."
                  for f in "${PROJECT_PATH}/Builds/Android/"*.apk "${PROJECT_PATH}/Builds/Android/"*.aab; do
                    [ -e "$f" ] || continue
                    base="$(basename "$f")"
                    dir="$(dirname "$f")"
                    if [[ "$base" != ${PREFIX}* ]]; then
                      mv "$f" "${dir}/${PREFIX}${base}"
                    fi
                  done
                else
                  echo "ℹ️ No prefix to apply."
                fi
                '''
            }
        }

        stage('Archive Unity Android Build Log') {
            when { expression { params.BUILD_TARGET == 'Android' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                script {
                    def logPath = "unity_build_log_android.txt"
                    archiveArtifacts artifacts: logPath, fingerprint: true, allowEmptyArchive: true
                }
            }
        }

        stage('Archive Android APK AAB') {
            when { expression { params.BUILD_TARGET == 'Android' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                script {
                    def relativePath = PROJECT_PATH == env.WORKSPACE ? '' : PROJECT_PATH - "${env.WORKSPACE}/"
                    def path = relativePath ? "${relativePath}/Builds/Android/*.apk, ${relativePath}/Builds/Android/*.aab" : "Builds/Android/*.apk, Builds/Android/*.aab"
                    archiveArtifacts artifacts: path, fingerprint: true
                }
            }
        }

        stage('Upload APK to Google Drive') {
            when {
                expression {
                    return (params.BUILD_TARGET == 'Android' || params.BUILD_TARGET == 'Both Android iOS') && params.UPLOAD_TO_GOOGLE_DRIVE
                }
            }
            steps {
                script {
                    echo "📤 Uploading APK files to Google Drive..."
                    echo "📧 Target account: ni@zenga.com.vn"
                    echo "📁 Target folder: Works/Projects/Coffee Mania/Build"
                    
                    // Tìm tất cả các file APK trong thư mục Builds/Android
                    def apkFiles = sh(
                        script: "find '${PROJECT_PATH}/Builds/Android' -name '*.apk' -type f",
                        returnStdout: true
                    ).trim().split('\n').findAll { it }
                    
                    if (apkFiles.isEmpty()) {
                        echo "⚠️ No APK files found to upload"
                        return
                    }
                    
                    echo "Found ${apkFiles.size()} APK file(s) to upload"
                    
                    // Credentials ID có thể được cấu hình qua environment variable
                    // Mặc định: 'google-drive-service-account-key' cho Service Account
                    def serviceAccountKeyId = env.GOOGLE_DRIVE_SERVICE_ACCOUNT_KEY_ID ?: 'google-drive-service-account-key'
                    echo "ℹ️ Using Service Account credentials ID: ${serviceAccountKeyId}"
                    
                    // Tạo Python script để upload lên Google Drive sử dụng Service Account
                    def uploadScript = '''
import os
import sys
import json

try:
    from google.oauth2 import service_account
    from googleapiclient.discovery import build
    from googleapiclient.http import MediaFileUpload
except ImportError:
    print("Installing required packages...")
    os.system(f"{sys.executable} -m pip install --quiet google-auth google-auth-httplib2 google-api-python-client")
    from google.oauth2 import service_account
    from googleapiclient.discovery import build
    from googleapiclient.http import MediaFileUpload

SCOPES = ['https://www.googleapis.com/auth/drive']
FOLDER_PATH = ["Works", "Projects", "Coffee Mania", "Build"]
SERVICE_ACCOUNT_KEY_FILE = 'service_account_key.json'

def get_service_account_credentials():
    """Lấy Service Account credentials từ environment variable hoặc file"""
    # Thử lấy từ environment variable (JSON string)
    service_account_json = os.environ.get('GOOGLE_DRIVE_SERVICE_ACCOUNT_KEY')
    if service_account_json:
        try:
            creds_data = json.loads(service_account_json)
            # Lưu vào file tạm để sử dụng
            with open(SERVICE_ACCOUNT_KEY_FILE, 'w') as f:
                json.dump(creds_data, f)
            print("✅ Loaded Service Account credentials from environment variable")
            return SERVICE_ACCOUNT_KEY_FILE
        except Exception as e:
            print(f"⚠️ Error parsing Service Account credentials from env: {str(e)}")
    
    # Thử lấy từ file
    if os.path.exists(SERVICE_ACCOUNT_KEY_FILE):
        print(f"✅ Loaded Service Account credentials from file: {SERVICE_ACCOUNT_KEY_FILE}")
        return SERVICE_ACCOUNT_KEY_FILE
    
    raise Exception("Service Account credentials not found. Please set GOOGLE_DRIVE_SERVICE_ACCOUNT_KEY or provide service_account_key.json file")

def authenticate():
    """Authenticate với Google Drive sử dụng Service Account"""
    credentials_file = get_service_account_credentials()
    
    # Tạo credentials từ Service Account key file
    creds = service_account.Credentials.from_service_account_file(
        credentials_file,
        scopes=SCOPES
    )
    
    print("✅ Service Account authentication successful")
    print(f"   Service Account Email: {creds.service_account_email}")
    
    return build('drive', 'v3', credentials=creds)

def find_or_create_folder(service, parent_id, folder_name):
    """Tìm hoặc tạo thư mục trong Google Drive"""
    # Tìm thư mục
    query = f"'{parent_id}' in parents and name='{folder_name}' and mimeType='application/vnd.google-apps.folder' and trashed=false"
    results = service.files().list(q=query, fields="files(id, name)").execute()
    items = results.get('files', [])
    
    if items:
        return items[0]['id']
    else:
        # Tạo thư mục mới
        file_metadata = {
            'name': folder_name,
            'mimeType': 'application/vnd.google-apps.folder',
            'parents': [parent_id]
        }
        folder = service.files().create(body=file_metadata, fields='id').execute()
        return folder.get('id')

def upload_file(service, file_path, folder_id):
    """Upload file lên Google Drive"""
    file_name = os.path.basename(file_path)
    print(f"📤 Uploading {file_name}...")
    
    file_metadata = {
        'name': file_name,
        'parents': [folder_id]
    }
    
    media = MediaFileUpload(file_path, resumable=True)
    file = service.files().create(
        body=file_metadata,
        media_body=media,
        fields='id, webViewLink'
    ).execute()
    
    print(f"✅ Successfully uploaded {file_name}")
    print(f"   Link: {file.get('webViewLink')}")
    return file.get('webViewLink')

if __name__ == "__main__":
    # Lấy danh sách file APK từ arguments
    apk_files = sys.argv[1:]
    
    if not apk_files:
        print("❌ No APK files provided")
        sys.exit(1)
    
    try:
        service = authenticate()
        
        # Bắt đầu từ root (My Drive)
        current_folder_id = "root"
        
        # Tạo/tìm từng thư mục trong đường dẫn
        for folder_name in FOLDER_PATH:
            current_folder_id = find_or_create_folder(service, current_folder_id, folder_name)
            print(f"📁 Found/Created folder: {folder_name}")
        
        # Upload từng file APK
        for apk_file in apk_files:
            if os.path.exists(apk_file):
                upload_file(service, apk_file, current_folder_id)
            else:
                print(f"⚠️ File not found: {apk_file}")
        
        print("✅ All files uploaded successfully to Google Drive!")
        
    except Exception as e:
        print(f"❌ Error uploading to Google Drive: {str(e)}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
'''
                    
                    // Lưu script vào file tạm
                    writeFile file: 'upload_to_gdrive.py', text: uploadScript
                    
                    // Chạy script Python để upload
                    def apkFilesStr = apkFiles.collect { "'${it}'" }.join(' ')
                    
                    // Thử sử dụng Jenkins Credentials Store cho Service Account key
                    try {
                        // Sử dụng Jenkins Credentials Store cho Service Account key
                        withCredentials([
                            string(credentialsId: serviceAccountKeyId, variable: 'GOOGLE_DRIVE_SERVICE_ACCOUNT_KEY')
                        ]) {
                            echo "✅ Using Jenkins Credentials Store for Service Account"
                            sh """
                                python3 upload_to_gdrive.py ${apkFilesStr}
                            """
                        }
                    } catch (Exception e) {
                        echo "ℹ️ Jenkins credentials not available, trying environment variables or files..."
                        
                        // Kiểm tra credentials từ environment variables hoặc files
                        def hasServiceAccountKey = sh(
                            script: 'test -n "$GOOGLE_DRIVE_SERVICE_ACCOUNT_KEY" || test -f "service_account_key.json"',
                            returnStatus: true
                        ) == 0
                        
                        if (!hasServiceAccountKey) {
                            error("""
⚠️ Google Drive Service Account credentials not found!

Please configure one of the following:

1. Jenkins Credentials Store (Recommended):
   - Add Service Account key with ID: ${serviceAccountKeyId}
     Type: Secret text
     Value: JSON content from Service Account key file (downloaded from Google Cloud Console)
   
2. Environment variable:
   - GOOGLE_DRIVE_SERVICE_ACCOUNT_KEY: JSON string of Service Account key

3. File in workspace:
   - service_account_key.json: Service Account key file

To create a Service Account:
1. Go to Google Cloud Console → IAM & Admin → Service Accounts
2. Create a new Service Account or use existing one
3. Enable Google Drive API
4. Create a JSON key and download it
5. Share the target Google Drive folder with the Service Account email address

See GOOGLE_DRIVE_SETUP.md for detailed instructions.
                            """)
                        }
                        
                        sh """
                            python3 upload_to_gdrive.py ${apkFilesStr}
                        """
                    }
                }
            }
        }

        stage('Cleanup APK AAB Folders') {
            when { expression { params.BUILD_TARGET == 'Android' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                sh '''
                echo "🧹 Cleaning up APK AAB files in ${PROJECT_PATH}/Builds/Android/"
                rm -f "${PROJECT_PATH}/Builds/Android/"*.apk "${PROJECT_PATH}/Builds/Android/"*.aab
                echo "✅ APK AAB files cleaned up."
                '''
            }
        }

        stage('Build iOS') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            environment {
                DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}"
                SCRIPTING_DEFINE_SYMBOLS = "${params.SCRIPTING_DEFINE_SYMBOLS}"
            }
            steps {
                sh '''
                echo "🔨 Starting Unity iOS build..."
                echo "⚙️ DEVELOPMENT_BUILD=${DEVELOPMENT_BUILD}"
                ${UNITY_PATH} -quit -batchmode -nographics -projectPath "${PROJECT_PATH}" -logfile 'unity_build_log_ios.txt' -executeMethod ${BUILD_METHOD_IOS} -buildTarget ios
                '''
            }
        }

        stage('Archive Unity iOS Build Log') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                script {
                    def relativePath = PROJECT_PATH == env.WORKSPACE ? '' : PROJECT_PATH - "${env.WORKSPACE}/"
                    def logPath = "unity_build_log_ios.txt"
                    archiveArtifacts artifacts: logPath, fingerprint: true, allowEmptyArchive: true
                }
            }
        }

        stage('Pod Install') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                sh '''\
                echo "📦 Running pod install..."
                cd "${PROJECT_PATH}/Builds/iOS"

                if [ ! -f "Podfile" ]; then
                    echo "⚠️  No Podfile found. Skipping pod install."
                    exit 0
                fi

                echo "📄 Podfile found. Running pod install..."
                LANG=en_US.UTF-8 ${POD_PATH} install --repo-update

                echo "✅ pod install completed."
                '''
            }
        }

        stage('Archive Xcode Project') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                sh '''
                echo "🔨 Archiving Xcode project..."

                IOS_PATH="${PROJECT_PATH}/Builds/iOS"
                WORKSPACE_PATH="$IOS_PATH/Unity-iPhone.xcworkspace"
                PROJECT_PATH_XCODE="$IOS_PATH/Unity-iPhone.xcodeproj"
                ARCHIVE_PATH="$IOS_PATH/build.xcarchive"

                if [ -d "$WORKSPACE_PATH" ]; then
                    echo "📁 Found xcworkspace. Using workspace build..."
                    xcodebuild -workspace "$WORKSPACE_PATH" -scheme Unity-iPhone -configuration Release -sdk iphoneos -archivePath "$ARCHIVE_PATH" archive
                elif [ -d "$PROJECT_PATH_XCODE" ]; then
                    echo "📁 No xcworkspace found. Falling back to xcodeproj..."
                    xcodebuild -project "$PROJECT_PATH_XCODE" -scheme Unity-iPhone -configuration Release -sdk iphoneos -archivePath "$ARCHIVE_PATH" archive
                else
                    echo "❌ ERROR: Neither .xcworkspace nor .xcodeproj found!"
                    exit 1
                fi
                '''
            }
        }

        stage('Export IPA Adhoc') {
            when {
                expression { ((params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS') && (params.BUILD_IOS_FORMAT == 'AdHoc' || params.BUILD_IOS_FORMAT == 'Both')) }
            }
            steps {
                sh '''
                echo "🔨 Exporting IPA (Adhoc)..."
                xcodebuild -exportArchive -archivePath "${PROJECT_PATH}/Builds/iOS/build.xcarchive" -exportOptionsPlist ExportOptions_Adhoc.plist -exportPath "${PROJECT_PATH}/Builds/iOS/ipa"
                '''
            }
        }

        stage('Rename IPA Adhoc') {
            when {
                expression { ((params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS') && (params.BUILD_IOS_FORMAT == 'AdHoc' || params.BUILD_IOS_FORMAT == 'Both')) }
            }
            steps {
                script {
                    def rawAppName = sh(script: "/usr/libexec/PlistBuddy -c 'Print :CFBundleDisplayName' \"${PROJECT_PATH}/Builds/iOS/Info.plist\"", returnStdout: true).trim()
                    def appName = rawAppName.replaceAll('[^a-zA-Z0-9_-]', '')
                    def version = sh(script: "/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' \"${PROJECT_PATH}/Builds/iOS/Info.plist\"", returnStdout: true).trim()
                    def datetime = sh(script: "TZ='Asia/Bangkok' date '+%d-%m-%Y-%H-%M-%S'", returnStdout: true).trim()
                    
                    def prefix = ""
                    if (params.DEVELOPMENT_BUILD) {
                        prefix = "DEV_BUILD_"
                    }
                    
                    if (params.SCRIPTING_DEFINE_SYMBOLS && params.SCRIPTING_DEFINE_SYMBOLS.trim()) {
                        def symbolsPrefix = params.SCRIPTING_DEFINE_SYMBOLS
                            .replaceAll(/[,;]/, '_')
                            .replaceAll(/[^a-zA-Z0-9_]/, '_')
                            .replaceAll(/_{2,}/, '_')
                            .replaceAll(/^_|_$/, '')
                        if (symbolsPrefix) {
                            prefix = "${symbolsPrefix}_${prefix}"
                        }
                    }
                    
                    def ipaName = "${prefix}${appName}_${version}_${datetime}_GMT+7_AdHoc.ipa"
                    echo "📦 Renaming ipa to: ${ipaName}"
                    sh """mv "${PROJECT_PATH}/Builds/iOS/ipa/${appName}.ipa" "${PROJECT_PATH}/Builds/iOS/ipa/${ipaName}" """
                }
            }
        }

        stage('Export IPA AppStore') {
            when {
                expression { ((params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS') && (params.BUILD_IOS_FORMAT == 'AppStore' || params.BUILD_IOS_FORMAT == 'Both')) }
            }
            steps {
                sh '''
                echo "🔨 Exporting IPA (AppStore)..."
                xcodebuild -exportArchive -archivePath "${PROJECT_PATH}/Builds/iOS/build.xcarchive" -exportOptionsPlist ExportOptions_Prod.plist -exportPath "${PROJECT_PATH}/Builds/iOS/ipa"
                '''
            }
        }

        stage('Rename IPA AppStore') {
            when {
                expression { ((params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS') && (params.BUILD_IOS_FORMAT == 'AppStore' || params.BUILD_IOS_FORMAT == 'Both')) }
            }
            steps {
                script {
                    def rawAppName = sh(script: "/usr/libexec/PlistBuddy -c 'Print :CFBundleDisplayName' \"${PROJECT_PATH}/Builds/iOS/Info.plist\"", returnStdout: true).trim()
                    def appName = rawAppName.replaceAll('[^a-zA-Z0-9_-]', '')
                    def version = sh(script: "/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' \"${PROJECT_PATH}/Builds/iOS/Info.plist\"", returnStdout: true).trim()
                    def datetime = sh(script: "TZ='Asia/Bangkok' date '+%d-%m-%Y-%H-%M-%S'", returnStdout: true).trim()
                    
                    def prefix = ""
                    if (params.DEVELOPMENT_BUILD) {
                        prefix = "DEV_BUILD_"
                    }
                    
                    if (params.SCRIPTING_DEFINE_SYMBOLS && params.SCRIPTING_DEFINE_SYMBOLS.trim()) {
                        def symbolsPrefix = params.SCRIPTING_DEFINE_SYMBOLS
                            .replaceAll(/[,;]/, '_')
                            .replaceAll(/[^a-zA-Z0-9_]/, '_')
                            .replaceAll(/_{2,}/, '_')
                            .replaceAll(/^_|_$/, '')
                        if (symbolsPrefix) {
                            prefix = "${symbolsPrefix}_${prefix}"
                        }
                    }
                    
                    def ipaName = "${prefix}${appName}_${version}_${datetime}_GMT+7_AppStore.ipa"
                    echo "📦 Renaming ipa to: ${ipaName}"
                    sh """mv "${PROJECT_PATH}/Builds/iOS/ipa/${appName}.ipa" "${PROJECT_PATH}/Builds/iOS/ipa/${ipaName}" """
                }
            }
        }

        stage('Archive iOS IPA') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                script {
                    def relativePath = PROJECT_PATH == env.WORKSPACE ? '' : PROJECT_PATH - "${env.WORKSPACE}/"
                    def path = relativePath ? "${relativePath}/Builds/iOS/ipa/*.ipa" : "Builds/iOS/ipa/*.ipa"
                    archiveArtifacts artifacts: path, fingerprint: true
                }
            }
        }

        stage('Cleanup IPA Folders') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                sh '''
                echo "🧹 Cleaning up IPA files in ${PROJECT_PATH}/Builds/iOS/ipa/"
                rm -f "${PROJECT_PATH}/Builds/iOS/ipa/"*.ipa
                echo "✅ IPA files cleaned up."
                '''
            }
        }

        stage('Archive dSYMs if AppStore') {
            when {
                expression {
                    return (params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS') &&
                           (params.BUILD_IOS_FORMAT == 'AppStore' || params.BUILD_IOS_FORMAT == 'Both')
                }
            }
            steps {
                sh '''
                echo "📦 Zipping dSYMs from build.xcarchive..."
                ARCHIVE_PATH="${PROJECT_PATH}/Builds/iOS/build.xcarchive"
                if [ -d "$ARCHIVE_PATH/dSYMs" ]; then
                  cd "$ARCHIVE_PATH"
                  zip -r dSYMs.zip dSYMs
                  cp dSYMs.zip "${PROJECT_PATH}/Builds/iOS/dSYMs.zip"
                else
                  echo "❌ No dSYMs folder found in $ARCHIVE_PATH"
                  exit 1
                fi
                '''
                archiveArtifacts artifacts: 'Builds/iOS/dSYMs.zip', fingerprint: true
            }
        }

        stage('Cleanup dSYMs.zip') {
            when {
                expression {
                    return (params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS') &&
                           (params.BUILD_IOS_FORMAT == 'AppStore' || params.BUILD_IOS_FORMAT == 'Both')
                }
            }
            steps {
                sh '''
                echo "🧹 Cleaning up dSYMs.zip file..."
                rm -f "${PROJECT_PATH}/Builds/iOS/dSYMs.zip"
                echo "✅ dSYMs.zip cleaned up."
                '''
            }
        }

        stage('Build MacOS') {
            when { expression { params.BUILD_TARGET == 'MacOS' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            environment {
                DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}"
                SCRIPTING_DEFINE_SYMBOLS = "${params.SCRIPTING_DEFINE_SYMBOLS}"
            }
            steps {
                sh '''
                echo "🔨 Starting Unity MacOS build..."
                echo "⚙️ DEVELOPMENT_BUILD=${DEVELOPMENT_BUILD}"
                ${UNITY_PATH} -quit -batchmode -nographics -projectPath "${PROJECT_PATH}" -logfile 'unity_build_log_macos.txt' -executeMethod ${BUILD_METHOD_MACOS} -buildTarget osxuniversal
                '''
            }
        }

        stage('Archive Unity MacOS Build Log') {
            when { expression { params.BUILD_TARGET == 'MacOS' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            steps {
                script {
                    def logPath = "unity_build_log_macos.txt"
                    archiveArtifacts artifacts: logPath, fingerprint: true, allowEmptyArchive: true
                }
            }
        }

        stage('Zip MacOS Build') {
            when { expression { params.BUILD_TARGET == 'MacOS' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            environment {
                DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}"
                SCRIPTING_DEFINE_SYMBOLS = "${params.SCRIPTING_DEFINE_SYMBOLS}"
            }
            steps {
                sh '''
                echo "📦 Zipping MacOS build..."
                cd "${PROJECT_PATH}/Builds"
                
                PREFIX=""
                if [ "${DEVELOPMENT_BUILD}" = "true" ]; then
                  PREFIX="DEV_BUILD_"
                fi
                
                if [ -n "${SCRIPTING_DEFINE_SYMBOLS}" ]; then
                  # Sanitize SCRIPTING_DEFINE_SYMBOLS: replace commas/semicolons with underscore, remove special chars
                  SYMBOLS_PREFIX=$(echo "${SCRIPTING_DEFINE_SYMBOLS}" | sed 's/[,;]/_/g' | sed 's/[^a-zA-Z0-9_]/_/g' | sed 's/__*/_/g' | sed 's/^_//' | sed 's/_$//')
                  if [ -n "$SYMBOLS_PREFIX" ]; then
                    PREFIX="${SYMBOLS_PREFIX}_${PREFIX}"
                  fi
                fi
                
                ZIPNAME="${PREFIX}MacOSBuild.zip"
                zip -r "${ZIPNAME}" MacOS
                echo "✅ MacOS build zipped as ${ZIPNAME}."
                '''
            }
        }

        stage('Build Windows') {
            when { expression { params.BUILD_TARGET == 'Windows' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            environment {
                DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}"
                SCRIPTING_DEFINE_SYMBOLS = "${params.SCRIPTING_DEFINE_SYMBOLS}"
            }
            steps {
                sh '''
                echo "🔨 Starting Unity Windows build..."
                echo "⚙️ DEVELOPMENT_BUILD=${DEVELOPMENT_BUILD}"
                ${UNITY_PATH} -quit -batchmode -nographics -projectPath "${PROJECT_PATH}" -logfile 'unity_build_log_windows.txt' -executeMethod ${BUILD_METHOD_WINDOWS} -buildTarget win64
                '''
            }
        }

        stage('Archive Unity Windows Build Log') {
            when { expression { params.BUILD_TARGET == 'Windows' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            steps {
                script {
                    def logPath = "unity_build_log_windows.txt"
                    archiveArtifacts artifacts: logPath, fingerprint: true, allowEmptyArchive: true
                }
            }
        }

        stage('Zip Windows Build') {
            when { expression { params.BUILD_TARGET == 'Windows' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            environment {
                DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}"
                SCRIPTING_DEFINE_SYMBOLS = "${params.SCRIPTING_DEFINE_SYMBOLS}"
            }
            steps {
                sh '''
                echo "📦 Zipping Windows build..."
                cd "${PROJECT_PATH}/Builds"
                
                PREFIX=""
                if [ "${DEVELOPMENT_BUILD}" = "true" ]; then
                  PREFIX="DEV_BUILD_"
                fi
                
                if [ -n "${SCRIPTING_DEFINE_SYMBOLS}" ]; then
                  # Sanitize SCRIPTING_DEFINE_SYMBOLS: replace commas/semicolons with underscore, remove special chars
                  SYMBOLS_PREFIX=$(echo "${SCRIPTING_DEFINE_SYMBOLS}" | sed 's/[,;]/_/g' | sed 's/[^a-zA-Z0-9_]/_/g' | sed 's/__*/_/g' | sed 's/^_//' | sed 's/_$//')
                  if [ -n "$SYMBOLS_PREFIX" ]; then
                    PREFIX="${SYMBOLS_PREFIX}_${PREFIX}"
                  fi
                fi
                
                ZIPNAME="${PREFIX}WindowsBuild.zip"
                zip -r "${ZIPNAME}" Windows
                echo "✅ Windows build zipped as ${ZIPNAME}."
                '''
            }
        }

        stage('Archive MacOS and Window ZIP') {
            when { expression { params.BUILD_TARGET == 'MacOS' || params.BUILD_TARGET == 'Windows' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            steps {
                script {
                    def relativePath = PROJECT_PATH == env.WORKSPACE ? '' : PROJECT_PATH - "${env.WORKSPACE}/"
                    def path = relativePath ? "${relativePath}/Builds/*.zip" : "Builds/*.zip"
                    archiveArtifacts artifacts: path, fingerprint: true
                }
            }
        }

        stage('Cleanup ZIP files') {
            when { expression { params.BUILD_TARGET == 'MacOS' || params.BUILD_TARGET == 'Windows' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            steps {
                sh '''
                echo "🧹 Cleaning up Zip files in ${PROJECT_PATH}/Builds/"
                rm -f "${PROJECT_PATH}/Builds/"*.zip
                echo "✅ ZIP files cleaned up."
                '''
            }
        }
    }

    post {
        success { echo '✅ Build completed successfully!' }
        failure { echo '❌ Build failed!' }
    }
}
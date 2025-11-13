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
    }

    parameters {
        string(name: 'BRANCH', defaultValue: 'develop', description: 'Git branch to build')
        choice(name: 'BUILD_TARGET', choices: ['Both Android iOS', 'Android', 'iOS', 'Both MacOS Windows', 'MacOS', 'Windows'], description: 'Build platforms')
        booleanParam(name: 'CLEAN_BUILD', defaultValue: false, description: 'Clean build by deleting Library folder')
        choice(name: 'BUILD_ANDROID_FORMAT', choices: ['APK', 'AAB', 'Both'], description: 'Build APK, AAB or both')
        choice(name: 'BUILD_IOS_FORMAT', choices: ['AdHoc', 'AppStore', 'Both'], description: 'Build AdHoc, AppStore or both')
        booleanParam(name: 'DEVELOPMENT_BUILD', defaultValue: false, description: 'Toggle Development Build, Deep Profiling Support, Autoconnect Profiler.')
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
            environment { DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}" }
            steps {
                sh '''
                if [ "${DEVELOPMENT_BUILD}" = "true" ]; then
                  echo "🏷️ Prefixing Android artifacts with DEV_BUILD_ ..."
                  for f in "${PROJECT_PATH}/Builds/Android/"*.apk "${PROJECT_PATH}/Builds/Android/"*.aab; do
                    [ -e "$f" ] || continue
                    base="$(basename "$f")"
                    dir="$(dirname "$f")"
                    if [[ "$base" != DEV_BUILD_* ]]; then
                      mv "$f" "${dir}/DEV_BUILD_${base}"
                    fi
                  done
                else
                  echo "ℹ️ DEVELOPMENT_BUILD=false, skip prefix."
                fi
                '''
            }
        }

        stage('Archive Unity Android Build Log') {
            when { expression { params.BUILD_TARGET == 'Android' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                script {
                    def relativePath = PROJECT_PATH == env.WORKSPACE ? '' : PROJECT_PATH - "${env.WORKSPACE}/"
                    def logPath = relativePath ? "${relativePath}/unity_build_log_android.txt" : "unity_build_log_android.txt"
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

        stage('Clean TTP Configs (iOS)') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                sh '''
                if [ -d "${PROJECT_PATH}/Assets/StreamingAssets/ttp/configurations" ]; then
                  echo "🧹 Clearing TTP configurations for iOS..."
                  rm -rf "${PROJECT_PATH}/Assets/StreamingAssets/ttp/configurations/"*
                else
                  echo "ℹ️ No TTP configuration folder found for iOS."
                fi
                '''
            }
        }

        stage('Build iOS') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            environment { DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}" }
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
                    def logPath = relativePath ? "${relativePath}/unity_build_log_ios.txt" : "unity_build_log_ios.txt"
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
                LANG=en_US.UTF-8 /Users/zenga_mac_mini_m4/.gem/ruby/3.4.0/bin/pod install --repo-update
                echo "✅ pod install completed."
                '''
            }
        }

        stage('Archive Xcode Project') {
            when { expression { params.BUILD_TARGET == 'iOS' || params.BUILD_TARGET == 'Both Android iOS' } }
            steps {
                sh '''
                echo "🔨 Archiving Xcode project..."
                xcodebuild -workspace "${PROJECT_PATH}/Builds/iOS/Unity-iPhone.xcworkspace" -scheme Unity-iPhone -configuration Release -sdk iphoneos -archivePath "${PROJECT_PATH}/Builds/iOS/build.xcarchive" archive
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
                    def prefix = params.DEVELOPMENT_BUILD ? "DEV_BUILD_" : ""
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
                    def prefix = params.DEVELOPMENT_BUILD ? "DEV_BUILD_" : ""
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
            environment { DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}" }
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
                    def relativePath = PROJECT_PATH == env.WORKSPACE ? '' : PROJECT_PATH - "${env.WORKSPACE}/"
                    def logPath = relativePath ? "${relativePath}/unity_build_log_macos.txt" : "unity_build_log_macos.txt"
                    archiveArtifacts artifacts: logPath, fingerprint: true, allowEmptyArchive: true
                }
            }
        }

        stage('Zip MacOS Build') {
            when { expression { params.BUILD_TARGET == 'MacOS' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            environment { DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}" }
            steps {
                sh '''
                echo "📦 Zipping MacOS build..."
                cd "${PROJECT_PATH}/Builds"
                ZIPNAME="MacOSBuild.zip"
                if [ "${DEVELOPMENT_BUILD}" = "true" ]; then
                  ZIPNAME="DEV_BUILD_MacOSBuild.zip"
                fi
                zip -r "${ZIPNAME}" MacOS
                echo "✅ MacOS build zipped as ${ZIPNAME}."
                '''
            }
        }

        stage('Build Windows') {
            when { expression { params.BUILD_TARGET == 'Windows' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            environment { DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}" }
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
                    def relativePath = PROJECT_PATH == env.WORKSPACE ? '' : PROJECT_PATH - "${env.WORKSPACE}/"
                    def logPath = relativePath ? "${relativePath}/unity_build_log_windows.txt" : "unity_build_log_windows.txt"
                    archiveArtifacts artifacts: logPath, fingerprint: true, allowEmptyArchive: true
                }
            }
        }

        stage('Zip Windows Build') {
            when { expression { params.BUILD_TARGET == 'Windows' || params.BUILD_TARGET == 'Both MacOS Windows' } }
            environment { DEVELOPMENT_BUILD = "${params.DEVELOPMENT_BUILD}" }
            steps {
                sh '''
                echo "📦 Zipping Windows build..."
                cd "${PROJECT_PATH}/Builds"
                ZIPNAME="WindowsBuild.zip"
                if [ "${DEVELOPMENT_BUILD}" = "true" ]; then
                  ZIPNAME="DEV_BUILD_WindowsBuild.zip"
                fi
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
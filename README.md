# Jenkins Setup Guide for Unity CI/CD

## General Setup

# **Step 1:** Install Jenkins  
```bash
brew install jenkins-lts
```

---

# **Step 2:** Start Jenkins  
```bash
brew services start jenkins-lts
```

---

# **Step 3:** Open Jenkins  
Visit:  
👉 http://localhost:8080  
Follow the on-screen instructions.

---

# Step 4: Create a Jenkins Job

1. Go to **Jenkins homepage**  
2. Click **Create a job**  
3. Enter an item (job) name  
4. Select **Pipeline** for the item type

---

# Step 5: Configure Jenkins

## ✔️ General
- Enable **Discard old builds**
  - Strategy: **Log Rotation**
  - Days to keep builds: **5**
  - Max builds to keep: **10**

## ✔️ Parameters  
Enable **This project is parameterized**, then add:

### **String Parameter**
- **Name:** `BRANCH`  
- **Default:** `develop`  
- **Description:** Git branch to build

### **Choice Parameter**
- **Name:** `BUILD_TARGET`  
- **Choices:**  
  ```
  Both Android iOS
  Android
  iOS
  Both MacOS Windows
  MacOS
  Windows
  ```
- **Description:** Build platforms

### **Boolean Parameter**
- **Name:** `CLEAN_BUILD`  
- **Default:** `false`  
- **Description:** Clean build by deleting Library folder

### **Choice Parameter**
- **Name:** `BUILD_ANDROID_FORMAT`  
- **Choices:**  
  ```
  APK
  AAB
  Both
  ```
- **Description:** Build APK, AAB or both

### **Choice Parameter**
- **Name:** `BUILD_IOS_FORMAT`  
- **Choices:**  
  ```
  AdHoc
  AppStore
  Both
  ```
- **Description:** Build AdHoc, AppStore or both

### **Boolean Parameter**
- **Name:** `DEVELOPMENT_BUILD`  
- **Default:** `false`  
- **Description:** Toggle Development Build, Autoconnect Profiler.

---

# Pipeline Configuration

### **Definition:** Pipeline script from SCM  
### **SCM:** Git  

#### **Repositories**
- **Repository URL:** *(your git repository URL)*  
- **Credentials:** `- none -`  
  - Ensure your build machine has the SSH key that can clone the repo.

#### **Branches to build**
- **Branch Specifier:**  
  ```
  */develop
  ```
- **Script Path:** `Jenkinsfile`

---

# Step 6: Add Jenkinsfile to Your Project Root  
Edit the following fields:

- **UNITY_PATH** – Path to Unity Editor executable  
- **PROJECT_PATH** – Path to your Unity project  
- **KEYSTORE_PASS** – Android keystore password  
- **KEY_ALIAS_PASS** – Android keystore alias password  
- **POD_PATH** – Run `which pod` in Terminal and paste the result here

---

# Step 7: Add BuildScript.cs  
Place the file in:  
```
/Assets/Editor/
```

---

# Step 8: Build  
1. Open http://localhost:8080  
2. Select your build job  
3. Click **Build with Parameters**  
4. Choose:
   - `BUILD_TARGET = Android`
5. Click **Build**

---

# iOS Build Setup

## Step 1  
Obtain the two `.mobileprovision` files (Adhoc / App Store) from the responsible person.

## Step 2  
Double-click both files on the build machine to import them (if not already imported).  
Record the information from the Adhoc provision: name, bundle ID, team ID, UUID.

Example:  
- **Provision Name:** Escape adhoc  
- **Bundle ID:** com.teamhoppi.escape.game.no1  
- **Team ID:** V9F8PB86RM  
- **UUID:** 1c1bc01b-c95f-43c6-b1b2-5dc6f6919a36  

Do the same for the App Store provision.

---

## Step 3 — Unity Project Setup  
Go to: **Edit → Project Settings → Player → Other Settings**

### Identification
- **Signing Team ID:** Enter the Team ID (Adhoc) saved in Step 2  
- **Automatically Sign:** Off  

### iOS Provisioning Profile
- **Profile ID:** Enter the UUID (Adhoc) saved in Step 2  
- **Profile Type:** Distribution  

---

## Step 4 — Setup .plist Files  
Copy `ExportOptions_Adhoc.plist` and `ExportOptions_Prod.plist` into the root directory of the Git project.

Edit **ExportOptions_Adhoc.plist** and replace the following values using the Adhoc provision data from Step 2:
- Team ID  
- Bundle ID  
- Provisioning profile name  

Do the same for **ExportOptions_Prod.plist** using the App Store provision.  
Push changes to the **develop** branch.

---

## Step 5 — Test iOS Build on Jenkins  
**Note:** To build any branch, that branch must contain all **5 required files**:  
- `Jenkinsfile`  
- `ExportOptions_Adhoc.plist`  
- `ExportOptions_Prod.plist`  
- `BuildScript.cs`  

This means you can only build branches that are created from `develop` or have already merged `develop` into them.
# Jenkins Setup Guide for Unity CI/CD

### **Step 1:** Install Jenkins  
```bash
brew install jenkins-lts
```

### **Step 2:** Start Jenkins  
```bash
brew services start jenkins-lts
```

### **Step 3:** Open Jenkins  
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

### **Note:**  
iOS Setup Guide will be updated soon.
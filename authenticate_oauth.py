#!/usr/bin/env python3
"""
Script helper để xác thực OAuth 2.0 lần đầu và lấy refresh token.
Chạy script này một lần để lấy token, sau đó lưu vào Jenkins Credentials Store.
"""

import os
import pickle
import base64
from google_auth_oauthlib.flow import InstalledAppFlow
from google.auth.transport.requests import Request

SCOPES = ['https://www.googleapis.com/auth/drive']
CREDENTIALS_FILE = 'credentials_oauth.json'
TOKEN_FILE = 'token.pickle'

def main():
    print("🔐 Google Drive OAuth 2.0 Authentication")
    print("=" * 50)
    
    # Kiểm tra file credentials
    if not os.path.exists(CREDENTIALS_FILE):
        print(f"❌ Error: {CREDENTIALS_FILE} not found!")
        print(f"\nPlease:")
        print(f"1. Download OAuth client credentials from Google Cloud Console")
        print(f"2. Save as '{CREDENTIALS_FILE}' in current directory")
        return 1
    
    print(f"✅ Found {CREDENTIALS_FILE}")
    
    # Kiểm tra token đã tồn tại
    if os.path.exists(TOKEN_FILE):
        print(f"\n⚠️  {TOKEN_FILE} already exists!")
        response = input("Do you want to re-authenticate? (y/N): ")
        if response.lower() != 'y':
            print("Cancelled.")
            return 0
    
    # Chạy OAuth flow
    print(f"\n🔐 Starting OAuth authentication flow...")
    print("   A browser window will open for authentication.")
    print("   Please login with: katori.norikarin@gmail.com")
    print()
    
    try:
        flow = InstalledAppFlow.from_client_secrets_file(CREDENTIALS_FILE, SCOPES)
        creds = flow.run_local_server(port=0)
        
        # Lưu token vào file
        with open(TOKEN_FILE, 'wb') as token:
            pickle.dump(creds, token)
        
        print("\n✅ Authentication successful!")
        print(f"✅ Token saved to: {TOKEN_FILE}")
        
        # Encode token thành base64 để lưu vào Jenkins
        token_base64 = base64.b64encode(pickle.dumps(creds)).decode('utf-8')
        
        print("\n" + "=" * 50)
        print("📋 NEXT STEPS:")
        print("=" * 50)
        print("\n1. Copy the base64 string below to Jenkins Credentials Store:")
        print("   - Credential ID: google-drive-oauth-token")
        print("   - Type: Secret text")
        print("   - Value: (paste the string below)")
        print("\n" + "-" * 50)
        print(token_base64)
        print("-" * 50)
        
        # Lưu vào file để dễ copy
        with open('token_base64.txt', 'w') as f:
            f.write(token_base64)
        print(f"\n✅ Base64 string also saved to: token_base64.txt")
        
        print("\n2. Also add OAuth credentials to Jenkins:")
        print("   - Credential ID: google-drive-oauth-credentials")
        print("   - Type: Secret text")
        print(f"   - Value: (content of {CREDENTIALS_FILE})")
        
        print("\n3. See GOOGLE_DRIVE_OAUTH_SETUP.md for detailed instructions.")
        
        return 0
        
    except Exception as e:
        print(f"\n❌ Error during authentication: {str(e)}")
        import traceback
        traceback.print_exc()
        return 1

if __name__ == "__main__":
    exit(main())

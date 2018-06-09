# ChromePlusRecord
This is the eye and mouse movements analysis system for webpages in Chrome

Following are step-by-step instructions for installing this system:
1. Open Chrome Extension Management page by navigating to chrome://extensions.
2. Enable Developer Mode by clicking the toggle switch next to "Developer mode".
3. Click the "LOAD UNPACKED" button and select the extension directory (ChromePlusRecord\CPR\CPR).
4. Modify "path of system execution file" and "extension ID" in the file (ChromePlusRecord\CPR\cpr.json).
path of system execution file: (ChromePlusRecord directory path)\ChromePlusRecord\ScreenRecordPlusChrome\ScreenRecordPlusChrome\bin\x64\Release\ScreenRecordPlusChrome.exe.
extension ID: ID of the extension in step 3. e.g., "kwhpccgfwcmffmibeeyjdejjlyclafic".
5. Open "regedit" on Windows and go to [HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts].
6. Create registry key named "cpr" and modify its data to the absolute path of the file (ChromePlusRecord\CPR\cpr.json).

System introduction videos:
https://www.youtube.com/watch?v=5cyRsRQjG5M
https://www.youtube.com/watch?v=D5RVOpr_JBE

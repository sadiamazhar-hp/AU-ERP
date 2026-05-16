# AU ERP Mobile — Setup Guide

React Native (bare) reporting app. Read-only management dashboard for Manufacturing, Sales, and Inventory.

## Prerequisites

| Tool | Required |
|------|---------|
| Node.js | ≥ 18 |
| JDK | 17 (for Android) |
| Android Studio | Latest (for emulator / SDK) |
| Xcode | 15+ (macOS only, for iOS) |

## 1 — Native scaffold (first time only)

The `android/` and `ios/` native directories are not included in source control.  
Generate them by running **in this folder**:

```bash
npx react-native@0.74 init TempScaffold --skip-install
# Then copy the native dirs into this project:
cp -r TempScaffold/android ./android
cp -r TempScaffold/ios    ./ios
rm -rf TempScaffold
```

> On Windows use PowerShell:
> ```powershell
> npx react-native@0.74 init TempScaffold --skip-install
> Copy-Item TempScaffold\android -Destination android -Recurse
> Copy-Item TempScaffold\ios    -Destination ios    -Recurse
> Remove-Item TempScaffold -Recurse -Force
> ```

## 2 — Install JS dependencies (already done if node_modules exists)

```bash
npm install
```

## 3 — Link native icons (react-native-vector-icons)

**Android**: Add to `android/app/build.gradle`:
```gradle
apply from: "../../node_modules/react-native-vector-icons/fonts.gradle"
```

**iOS**:
```bash
npx pod-install ios
```
Then in Xcode, add the fonts listed in `node_modules/react-native-vector-icons/README.md` to the bundle resources.

## 4 — Set the API base URL

Edit [src/api/client.ts](src/api/client.ts):

| Environment | URL to use |
|-------------|-----------|
| Android emulator → dev PC | `http://10.0.2.2:5242/api/mobile` (default) |
| iOS simulator → dev Mac | `http://localhost:5242/api/mobile` |
| Physical device (same LAN) | `http://<your-pc-ip>:5242/api/mobile` |

## 5 — Run

```bash
# Start Metro bundler
npm start

# Android (separate terminal)
npm run android

# iOS (separate terminal, macOS only)
npm run ios
```

---

## Project structure

```
AuErpMobile/
├── src/
│   ├── api/            Axios API calls per domain
│   ├── auth/           JWT AuthContext + token storage
│   ├── components/     KpiCard, ReportTable, Charts, FilterSheet …
│   ├── navigation/     Stack + BottomTab navigators
│   ├── screens/
│   │   ├── auth/       Login
│   │   ├── dashboard/  Home KPI cards
│   │   ├── production/ 6 manufacturing reports
│   │   ├── inventory/  Finished Goods
│   │   └── sales/      5 sales reports
│   └── theme/          Colors (AU ERP palette) + typography
├── App.tsx             Root component
└── index.js            Entry point
```

## Color palette (from web app)

| Token | Hex |
|-------|-----|
| Primary blue | `#0070f2` |
| Shell / header bg | `#1d2d3e` |
| Card bg | `#ffffff` |
| Page bg | `#f5f6f7` |
| Success green | `#188918` |
| Warning orange | `#e76500` |
| Error red | `#bb0000` |

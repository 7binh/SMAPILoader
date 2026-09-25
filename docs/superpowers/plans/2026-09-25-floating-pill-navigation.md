# Kế Hoạch Triển Khai: Material 3 Edge-to-Edge Floating Pill Navigation Bar

> **For agentic workers:** REQUIRED SUB-SKILL: Use sp-subagent-driven-development (recommended) or sp-executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hiện đại hóa giao diện SMAPILoader với kiến trúc Single-Activity, thanh điều hướng nổi dạng viên thuốc (Floating Pill Navigation Bar) lơ lửng trên lớp phủ mờ chuyển sắc (Bottom Scrim Gradient) điều phối 3 Tab: Home, Mods và Tools.

**Architecture:** Sử dụng `FrameLayout` làm gốc xếp tầng theo trục Z (Lớp 1: `ViewPager2` chứa 3 Fragment, Lớp 2: `View` Bottom Scrim cao 110dp, Lớp 3: `MaterialCardView` Floating Pill Bar cao 64dp). `LauncherActivity` điều phối chuyển đổi giữa các Fragment kèm rung phản hồi xúc giác (Haptics) và đồng bộ trạng thái active indicator.

**Tech Stack:** C# .NET 9 Android (Xamarin.AndroidX), AndroidX ViewPager2, Google Material Components (`MaterialCardView`, `MaterialButton`), Android Vector Drawables.

**Spec:** [docs/superpowers/specs/2026-09-25-floating-pill-navigation-design.md](file:///C:/Antigravity%20Projects/SMAPILoader/docs/superpowers/specs/2026-09-25-floating-pill-navigation-design.md)

## Global Constraints
- Target Framework: `net9.0-android` (API 35, Min SDK 28).
- Không phá vỡ quy trình build tự động vá `libmonosgen-2.0.so` qua `patch_apk.ps1`.
- Mọi danh sách cuộn (`ScrollView`, `RecyclerView`) phải có `paddingBottom="108dp"` và `clipToPadding="false"` để nội dung cuối không bị che khuất bởi thanh pill nổi.
- Bottom Scrim phải có `android:clickable="false"` để touch gestures luôn xuyên xuống nội dung bên dưới.
- Giữ vững tính tương thích của SMAPI và game runner khi khởi động.

## Review Focus
- **Cắt xén nội dung ở đáy (Bottom clipping)**: Kiểm tra xem item cuối cùng trong cả 3 Tab có thể cuộn vượt qua Floating Pill Bar để bấm được hay không.
- **Xung đột cử chỉ vuốt (Gesture conflict)**: Vuốt ngang chuyển trang trên ViewPager2 không được làm đơ hay giật thanh pill indicator.
- **Trạng thái nạp dữ liệu khi đổi tab**: Khi chuyển qua tab Mods hay Tools, dữ liệu mod hoặc trạng thái SMAPI phải được cập nhật tức thì.
- **Độ tương phản trên nền Dark/Light**: Màu sắc của Floating Pill và icon phải nổi bật rõ ràng, không bị chìm vào nền hay scrim.
- **Độ ổn định khi xoay màn hình hoặc ẩn app**: Trạng thái tab hiện tại được duy trì chính xác.

---

### Task 1: Tạo Tài Nguyên Đồ Họa & Vector Drawables (Scrim, Pill, Icons)

**Files:**
- Create: `SMAPIGameLoader/Resources/Drawable/bottom_scrim.xml`
- Create: `SMAPIGameLoader/Resources/Drawable/pill_tab_background.xml`
- Create: `SMAPIGameLoader/Resources/Drawable/ic_nav_home.xml`
- Create: `SMAPIGameLoader/Resources/Drawable/ic_nav_mods.xml`
- Create: `SMAPIGameLoader/Resources/Drawable/ic_nav_tools.xml`

**Interfaces:**
- Produces: `@drawable/bottom_scrim`, `@drawable/pill_tab_background`, `@drawable/ic_nav_home`, `@drawable/ic_nav_mods`, `@drawable/ic_nav_tools`

- [ ] **Step 1: Tạo `bottom_scrim.xml`**
  Tạo dải gradient dọc từ trong suốt (`#00000000`) ở đỉnh sang màu nền đục (`#141218` hoặc token background) ở đáy.

- [ ] **Step 2: Tạo `pill_tab_background.xml`**
  Tạo selector/shape cho indicator của tab khi được chọn (bo tròn 20dp, màu `?attr/colorSecondaryContainer`).

- [ ] **Step 3: Tạo các icon vector Material**
  - `ic_nav_home.xml`: Icon ngôi nhà (Home).
  - `ic_nav_mods.xml`: Icon hộp/tiện ích (Extension/Mods).
  - `ic_nav_tools.xml`: Icon cờ lê/bánh răng (Tools).

- [ ] **Step 4: Kiểm tra build resource**
  Chạy lệnh build kiểm tra XML hợp lệ.

- [ ] **Step 5: Commit**
  ```bash
  rtk git add SMAPIGameLoader/Resources/Drawable
  rtk git commit -m "feat(ui): add bottom scrim gradient and navigation vector drawables"
  ```

---

### Task 2: Tạo Layout Cho 3 Màn Hình Con (Fragment Layouts)

**Files:**
- Create: `SMAPIGameLoader/Resources/Layout/FragmentHome.xml`
- Create: `SMAPIGameLoader/Resources/Layout/FragmentMods.xml`
- Create: `SMAPIGameLoader/Resources/Layout/FragmentTools.xml`

**Interfaces:**
- Produces: Layout IDs `@layout/FragmentHome`, `@layout/FragmentMods`, `@layout/FragmentTools`

- [ ] **Step 1: Tạo `FragmentHome.xml`**
  - Di chuyển phần App Header (icon gà, title), Device & Game Status Card, nút Chọn file APK, Quét lại game từ `LauncherLayout.xml` sang.
  - Đặt nút **Start Game** kích thước lớn 56dp nổi bật (Primary CTA) ngay dưới thẻ trạng thái.
  - Cấu hình `ScrollView` với `android:paddingBottom="108dp"` và `android:clipToPadding="false"`.

- [ ] **Step 2: Tạo `FragmentMods.xml`**
  - Giao diện quản lý mod: Top bar gồm tiêu đề, số lượng mod, nút mở thư mục Mods ("Open Folder").
  - `RecyclerView` (hoặc `ListView`) hiển thị các thẻ `ModItemViewLayout`.
  - Cấu hình danh sách với `android:paddingBottom="108dp"` và `android:clipToPadding="false"`.

- [ ] **Step 3: Tạo `FragmentTools.xml`**
  - Thẻ SMAPI Platform: Phiên bản SMAPI, nút "Cài đặt SMAPI từ file Zip".
  - Thẻ Logs: Nút "Chia sẻ / Tải lên SMAPI Log".
  - Thẻ Saves & Storage: Nút "Nhập Save từ Saves.zip", nút "Dọn dẹp bộ nhớ đệm (Cache)".
  - Cấu hình `ScrollView` với `android:paddingBottom="108dp"` và `android:clipToPadding="false"`.

- [ ] **Step 4: Commit**
  ```bash
  rtk git add SMAPIGameLoader/Resources/Layout/Fragment*.xml
  rtk git commit -m "feat(ui): create FragmentHome, FragmentMods, and FragmentTools layouts"
  ```

---

### Task 3: Tái Cấu Trúc Khung Điều Hướng Chính (`LauncherLayout.xml`)

**Files:**
- Modify: `SMAPIGameLoader/Resources/Layout/LauncherLayout.xml`

**Interfaces:**
- Consumes: `@layout/FragmentHome`, `@layout/FragmentMods`, `@layout/FragmentTools`, `@drawable/bottom_scrim`, `@drawable/pill_tab_background`
- Produces: View IDs `@+id/mainViewPager`, `@+id/bottomScrim`, `@+id/floatingPillBar`, `@+id/tabHome`, `@+id/tabMods`, `@+id/tabTools`

- [ ] **Step 1: Thay thế layout gốc bằng `FrameLayout` phân tầng 3 lớp**
  - **Lớp 1**: `androidx.viewpager2.widget.ViewPager2` (hoặc `FrameLayout`) chiếm trọn màn hình (`match_parent`).
  - **Lớp 2**: `View` làm Bottom Scrim: `layout_height="110dp"`, `layout_gravity="bottom"`, `background="@drawable/bottom_scrim"`, `android:clickable="false"`.
  - **Lớp 3**: `com.google.android.material.card.MaterialCardView` làm Floating Pill Navigation Bar:
    - `layout_height="64dp"`, `layout_gravity="bottom|center_horizontal"`, `layout_marginBottom="20dp"`, `layout_marginHorizontal="24dp"`.
    - `app:cardCornerRadius="32dp"`, `app:cardElevation="6dp"`.
    - `app:cardBackgroundColor="?attr/colorSurfaceContainerHigh"`, `app:strokeWidth="1dp"`, `app:strokeColor="?attr/colorOutlineVariant"`.

- [ ] **Step 2: Thiết kế 3 Tab Item bên trong Floating Pill Bar**
  - `LinearLayout` chia 3 cột đều nhau (`layout_weight="1"`):
    - `tabHome`: Icon `ic_nav_home` + Text `Trang chủ`.
    - `tabMods`: Icon `ic_nav_mods` + Text `Mods`.
    - `tabTools`: Icon `ic_nav_tools` + Text `Công cụ`.

- [ ] **Step 3: Commit**
  ```bash
  rtk git add SMAPIGameLoader/Resources/Layout/LauncherLayout.xml
  rtk git commit -m "feat(ui): implement 3-tier layering with ViewPager2, Bottom Scrim and Floating Pill Bar"
  ```

---

### Task 4: Triển Khai Logic Cho Các Fragment (`HomeFragment`, `ModsFragment`, `ToolsFragment`)

**Files:**
- Create: `SMAPIGameLoader/Launcher/Fragments/HomeFragment.cs`
- Create: `SMAPIGameLoader/Launcher/Fragments/ModsFragment.cs`
- Create: `SMAPIGameLoader/Launcher/Fragments/ToolsFragment.cs`

**Interfaces:**
- Produces: Classes `HomeFragment : AndroidX.Fragment.App.Fragment`, `ModsFragment`, `ToolsFragment`

- [ ] **Step 1: Viết `HomeFragment.cs`**
  - Xử lý nạp thông tin launcher, phát hiện game (`StardewApkTool.DetectGame()`).
  - Xử lý sự kiện nút "Chọn file APK" (`OnClickSelectApk`).
  - Xử lý sự kiện nút "Quét lại game" (`OnClickRescanGame`).
  - Xử lý sự kiện nút "Start Game" (`OnClickStartGame`).

- [ ] **Step 2: Viết `ModsFragment.cs`**
  - Quản lý danh sách mod từ thư mục `Mods`.
  - Xử lý bật/tắt mod khi chạm hoặc gạt công tắc.
  - Xử lý nút mở thư mục Mods qua trình duyệt tệp (`FileTool.OpenAppFilesExternalFilesDir("Mods")`).
  - Nút Refresh quét lại danh sách.

- [ ] **Step 3: Viết `ToolsFragment.cs`**
  - Cài đặt SMAPI từ file Zip (`SMAPIInstaller.OnClickInstallSMAPIZip`).
  - Chia sẻ / Upload SMAPI Log (`LogParser.OnClickUploadLog`).
  - Quản lý file Save (`SaveManager.OnClickImportSaveZip`).
  - Dọn dẹp cache (`FileTool.ClearCache()`).

- [ ] **Step 4: Commit**
  ```bash
  rtk git add SMAPIGameLoader/Launcher/Fragments/
  rtk git commit -m "feat(ui): implement HomeFragment, ModsFragment, and ToolsFragment logic"
  ```

---

### Task 5: Tích Hợp ViewPager2 & Xử Lý Sự Kiện Floating Pill Trong `LauncherActivity.cs`

**Files:**
- Modify: `SMAPIGameLoader/Launcher/LauncherActivity.cs`

**Interfaces:**
- Consumes: `HomeFragment`, `ModsFragment`, `ToolsFragment`, `LauncherLayout`
- Produces: `LauncherPagerAdapter : FragmentStateAdapter`, ViewPager tab sync, active pill indicator styling, Haptic Feedback.

- [ ] **Step 1: Cấu hình Edge-to-Edge System Bars**
  - Thiết lập window flags cho phép nội dung tràn viền dưới thanh điều hướng hệ thống (Navigation Bar của Android).
  - Áp dụng WindowInsetsCompat cho container nếu cần.

- [ ] **Step 2: Cài đặt `FragmentStateAdapter` cho `ViewPager2`**
  - Kết nối 3 Fragment với ViewPager2 theo thứ tự: 0 -> `HomeFragment`, 1 -> `ModsFragment`, 2 -> `ToolsFragment`.

- [ ] **Step 3: Đồng bộ trạng thái chạm trên Floating Pill Bar**
  - Bấm vào Tab nào thì chuyển ViewPager2 tới trang đó (`viewPager.SetCurrentItem(index, true)`).
  - Kích hoạt rung phản hồi xúc giác (`view.PerformHapticFeedback(FeedbackConstants.ContextClick)`).

- [ ] **Step 4: Lắng nghe `OnPageChangeCallback` của ViewPager2**
  - Khi người dùng vuốt đổi trang: Tự động cập nhật visual indicator trên thanh Floating Pill (đổi nền container của tab được chọn sang `@drawable/pill_tab_background`, đổi màu text/icon tương ứng).

- [ ] **Step 5: Commit**
  ```bash
  rtk git add SMAPIGameLoader/Launcher/LauncherActivity.cs
  rtk git commit -m "feat(ui): wire ViewPager2 with Floating Pill Navigation Bar and haptic feedback"
  ```

---

### Task 6: Biên Dịch, Cài Đặt Thiết Bị & Xác Minh Thực Tế (End-to-End Verification)

**Files:**
- Output APK: `SMAPIGameLoader/bin/Release/net9.0-android/abc.smapi.gameloader-Signed.apk`
- Artifact screenshot: `launcher_pill_nav.png`

- [ ] **Step 1: Biên dịch Release APK**
  Chạy `dotnet build -c Release SMAPIGameLoader/SMAPIGameLoader.csproj` và đảm bảo 0 lỗi biên dịch, script `patch_apk.ps1` tự động chạy thành công.

- [ ] **Step 2: Cài đặt lên thiết bị kết nối (`R3CR40SFZSB`)**
  Chạy `rtk adb install -r SMAPIGameLoader/bin/Release/net9.0-android/abc.smapi.gameloader-Signed.apk`.

- [ ] **Step 3: Khởi động app và kiểm thử điều hướng**
  - Khởi chạy app trên máy: `adb shell am start -n abc.smapi.gameloader/...LauncherActivity`.
  - Thử chạm chuyển đổi giữa 3 Tab (Trang chủ -> Mods -> Công cụ).
  - Thử vuốt ngang màn hình để kiểm tra độ mượt của ViewPager2.
  - Cuộn danh sách ở cả 3 tab để kiểm tra xem item cuối cùng có lướt qua lớp Bottom Scrim và Floating Pill một cách hoàn hảo hay không.

- [ ] **Step 4: Chụp ảnh màn hình bằng chứng**
  Chụp ảnh màn hình thiết bị và lưu vào artifacts để kiểm tra trực quan.

- [ ] **Step 5: Commit & Push lên GitHub Fork**
  ```bash
  rtk git push origin master
  ```

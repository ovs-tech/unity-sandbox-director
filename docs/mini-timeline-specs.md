Bạn là Unity Senior Engineer. Nhiệm vụ của bạn: xây dựng và hoàn thiện **Mini Timeline System** cho game sandbox 3D trên mobile.  

## Bối cảnh
- Unity engine, URP, target mobile (Android tầm trung trở lên).
- Timeline này là công cụ in-game để người chơi tự tạo "scene phim" bằng cách kéo thả animation, camera, morph, audio…
- Yêu cầu chạy mượt, tối ưu, dễ lưu/đọc dữ liệu, dễ mở rộng thêm track mới.

## Yêu cầu kỹ thuật
1. **Kiến trúc**
   - Có `MiniTimelineDirector` quản lý thời gian, play/pause/scrub.
   - Track & Clip theo interface: `IMiniTrack`, `IMiniClip`.
   - Các loại track: `AnimTrack`, `MorphTrack`, `ExpressionTrack`, `CameraTrack`, `IKTrack`, `AudioTrack`, `LightFxTrack`, `EventTrack`.
   - Mỗi track có Evaluate(time, scrub).

2. **Data & Serialization**
   - Dùng JSON lightweight để lưu dự án (versioned).
   - Clip có: id, start, duration, payload (tham chiếu asset qua Addressables).
   - BindingContext: map từ string key ("charA", "camA") tới GameObject trong scene.

3. **Runtime**
   - `Director` update mỗi frame, gọi Evaluate tất cả track.
   - Scrubbing: khi Seek(time), phải force evaluate ngay.
   - Animation: dùng Animator/Playables; Morph: blendshape weights; Camera: switch cut hoặc path; Event: trigger callback.

4. **UI/UX (editor in-game)**
   - Timeline có ruler, zoom, scrub head.
   - Track hiển thị dạng hàng ngang, clip là block có thể kéo/dãn.
   - Gesture mobile: tap chọn, drag move, long-press menu, pinch zoom.
   - Undo/Redo (command pattern).
   - Snap theo frame grid.

5. **Hiệu năng**
   - Target 45–60 FPS với 6–8 track active, 20 clip.
   - Cache AnimatorController và Addressables.
   - Không GC per-frame.

6. **Export**
   - MVP: screenshot sequence + save project.
   - Phase 2: render video (không cần ngay).


7. **Thành phần runtime (chi tiết)**

**MiniTimelineDirector (MonoBehaviour)**
- **Vai trò**: Clock trung tâm; quản lý state (Play/Pause/Stop/Loop), tốc độ phát, scrub; điều phối Evaluate cho toàn bộ track.
- **Thuộc tính chính**:
  - `float Length` — tổng thời lượng project (giây).
  - `float Time` — thời điểm hiện tại.
  - `float PlaybackSpeed` — hệ số tốc độ (0.1–2.0).
  - `bool Loop` — lặp lại khi hết timeline.
  - `IReadOnlyList<IMiniTrack> Tracks` — danh sách track đã build.
  - `bool IsPlaying` — trạng thái.
- **Phương thức công khai**:
  - `void Play()`, `void Pause()`, `void Stop()`, `void Seek(float t)`.
  - `void SetProject(MiniTimelineProject project, BindingContext ctx)` — parse JSON → build tracks.
  - `T GetTrack<T>(string id)` — truy xuất track theo id/kiểu.
  - `void SetPlaybackSpeed(float s)`, `void SetLoop(bool on)`.
- **Vòng đời**:
  - `Update()` khi `IsPlaying` → tăng `Time` theo `deltaTime*PlaybackSpeed` → `Evaluate(false)`.
  - `Seek()`/scrub → `Evaluate(true)` để cập nhật tức thời.
- **Đảm bảo**:
  - Không cấp phát GC mỗi frame; cache lists/buffers.
  - Đánh dấu **frame dirty** khi project thay đổi (`MarkDirty()`), tránh rebuild không cần thiết.

**IMiniTrack (interface)**
- **Vai trò**: Hợp đồng chung cho mọi loại track.
- **API**:
  - `string Id { get; }`
  - `string BindKey { get; }` — chìa khóa tới thực thể (charA/camA/…).
  - `void Bind(BindingContext ctx)` — gắn对象 cần điều khiển.
  - `void Prepare()` — khởi tạo nội bộ (cache controller, graph node…).
  - `void Evaluate(float time, bool scrub)` — cập nhật theo thời gian.
  - `IEnumerable<IMiniClip> GetClips()` — phục vụ UI/undo.
  - `void OnProjectClosed()` — giải phóng.
- **Yêu cầu**:
  - Tự chịu trách nhiệm **blend nội bộ** (vd. Animation mixer, morph accumulation).
  - Có thể sử dụng **MiniPlayableGraph** hoặc custom evaluator.

**IMiniClip (interface)**
- **Vai trò**: Dữ liệu clip tối thiểu, thuần serializable.
- **API**:
  - `string Id { get; }`
  - `float Start { get; }`
  - `float Duration { get; }`
  - `bool Contains(float t)` — helper kiểm tra phạm vi.
- **Quy ước**:
  - `Duration == 0` cho **marker** (Event/Signal).

**MiniPlayableGraph**
- **Vai trò**: Lớp tiện ích bọc Unity `Playables` để tái sử dụng Mixer/Outputs.
- **Thành phần**:
  - `PlayableGraph Graph` — graph dùng chung hoặc per-track tùy thiết kế.
  - **Animation**: `AnimationPlayableOutput`, `AnimationMixerPlayable`, cache `AnimationClipPlayable`.
  - **Audio**: `AudioPlayableOutput`, `AudioMixerPlayable`.
- **Chức năng**:
  - Tạo mixer 2-slot/4-slot cho crossfade liền kề clip.
  - Cấp API: `PlayClip(clip, fadeIn)`, `CrossFade(nextClip, time, ease)`, `SetNormalizedTime(u)`.
  - Quản lý **lifecycle**: tạo khi `Prepare()`, destroy khi `OnProjectClosed()`.

**BindingContext**
- **Vai trò**: Sổ địa chỉ ánh xạ `BindKey -> UnityEngine.Object`.
- **API**:
  - `void Bind(string key, Object obj)`
  - `T Resolve<T>(string key) where T: Object`
  - `bool TryResolve(string key, out Object obj)`
- **Hỗ trợ**:
  - Bind asset scene (MainCamera/Light), instance runtime (nhân vật spawn), hoặc proxy (controller service).
  - Log cảnh báo nếu thiếu binding (track sẽ **no-op** thay vì gây crash).


8. **Track types (đề xuất, chi tiết)**

> Ghi chú: mỗi track có `clips[]`. Clip payload tham chiếu asset qua **Addressables** (`addr:`) hoặc object trong scene (`scene:`). Track tự blend nếu 2 clip chồng nhau (tùy loại).

**AnimTrack → AnimClip**
- **Mục đích**: Phát `AnimationClip` lên `Animator` (Humanoid).
- **Payload**:
  - `anim` (string) — key Addressables `AnimationClip`.
  - `speed` (float) — hệ số tốc độ.
  - `wrap` (enum) — Loop/Clamp.
  - `layer` (int) — layer hoạt ảnh (nếu dùng nhiều layer).
  - `fadeIn`, `fadeOut` (float) — thời gian chuyển.
- **Hành vi**:
  - Tạo `AnimationMixerPlayable` 2–4 slot; crossfade mượt khi chuyển clip.
  - Tính `localT = (time - Start) * speed` (clamp/loop theo `wrap`) → set normalized time.
- **Ràng buộc**:
  - Nếu skeleton scale thay đổi do morph, dùng **scale compensation** giảm trượt tiếp xúc.

**PoseIKTrack → IKClip**
- **Mục đích**: Điều khiển IK (tay/chân/đầu/mắt) bằng `Animation Rigging`.
- **Payload**:
  - `limb` (enum) — LeftHand/RightHand/Head/Eyes/Foot.
  - `target` (scene:/addr:) — Transform mục tiêu.
  - `weightCurve` — AnimationCurve (0..1), áp theo tiến độ clip.
  - `hint` (optional) — Transform gợi ý khớp (khuỷu gối).
- **Hành vi**:
  - Khi Evaluate: set weight constraint theo `weightCurve(u)`.
  - Nếu `target` null → no-op, log warning.

**MorphTrack → MorphKeyClip / MorphCurve**
- **Mục đích**: Điều khiển **blendshape** (body/face) theo thời gian.
- **Payload (MorphKeyClip)**:
  - `keys[]`: `{ id: "Smile", v0:0, v1:80, curve:Linear }` — nội suy từ v0→v1.
- **Payload (MorphCurve)**:
  - `channels[]`: `{ id:"BrowUp", curve: [ (t,w)... ] }` — nhiều điểm key bất kỳ trong clip.
- **Hành vi**:
  - Tại Evaluate: tích lũy giá trị từ **tất cả clip active** (nếu nhiều clip trùm) theo **chế độ tổng/trộn**:
    - `Mode = Additive` (mặc định, clamp 0..100).
    - `Mode = Override` (ưu tiên clip có `priority` cao nhất).
- **Tối ưu**:
  - Cache chỉ số blendshape theo id ngay `Prepare()`.

**ExpressionTrack → ExpressionClip**
- **Mục đích**: Ánh xạ preset biểu cảm (tập morph + viseme).
- **Payload**:
  - `expressionId` — key đến `ExpressionClip` (ScriptableObject) chứa mapping morph.
  - `blend` — 0..1.
  - `ease` — curve.
- **Hành vi**:
  - Tương tự MorphTrack nhưng dùng preset; ưu tiên chạy **sau MorphTrack** để có thể override một phần.

**CameraTrack → CameraCutClip, CameraPathClip**
- **Mục đích**: Điều khiển camera (Cinemachine hoặc camera thường).
- **CameraCutClip Payload**:
  - `virtualCam` — tên/đường dẫn Virtual Camera.
  - `fov` (optional), `dof` (optional), `blendIn` (s).
- **CameraPathClip Payload**:
  - `path` — spline/waypoints (addr/scene).
  - `ease` — curve.
  - `lookAt` — target Transform (optional).
  - `fovCurve`/`roll` (optional).
- **Hành vi**:
  - Cut: bật vcam mục tiêu, tắt vcam trước; áp FOV/DOF nhanh theo `blendIn`.
  - Path: nội suy theo chiều dài cung (arc-length) để tốc độ đều; cập nhật vị trí/rotation mỗi frame.

**AudioTrack → AudioClipRef**
- **Mục đích**: Phát audio (nhạc/voice/SFX) đồng bộ thời gian.
- **Payload**:
  - `clip` — Addressables `AudioClip`.
  - `volume` (0..1), `pitch`, `loop`.
  - `fadeIn`, `fadeOut`.
- **Hành vi**:
  - Dùng Playables hoặc trực tiếp `AudioSource.time = localT`.
  - Crossfade khi 2 clip chồng nhau.
  - Tôn trọng scrub: khi `scrub==true`, nhảy `timeSample` tức thì mà không phát “click”.

**FxLightTrack → LightClip, PostFxClip**
- **Mục đích**: Điều khiển đèn và Post-processing URP.
- **LightClip Payload**:
  - `intensityCurve`, `colorGradient`, `rangeCurve`, `spotAngleCurve`.
  - `enable` (bool) — bật/tắt đèn trong khoảng clip.
- **PostFxClip Payload**:
  - Tham số Volume: `exposure`, `contrast`, `saturation`, `temperature`, `vignette`, `LUT`…
  - Mỗi tham số có `curve` riêng; không tạo/destroy Volume mỗi frame — chỉ đổi `weight`/override.
- **Hành vi**:
  - Evaluate: ghi các giá trị vào `MaterialPropertyBlock` hoặc Volume override; gộp thay đổi trong 1 pass/frame.

**EventTrack → SignalClip (marker)**
- **Mục đích**: Phát tín hiệu cho hệ thống khác (UI capture, đổi outfit, đặt flag).
- **Payload**:
  - `eventId` (string), `payload` (string/float/json nhỏ).
  - `edge` — `OnEnter` (khi playhead đi qua), `OnExit`, `Both`.
- **Hành vi**:
  - Director theo dõi `previousTime -> currentTime`, nếu vượt qua vị trí marker → bắn sự kiện **một lần**.
  - `scrub==true` không bắn trừ khi bật chế độ `FireOnScrub`.

#### Gợi ý thứ tự Evaluate (per-frame)
1) EventTrack (để kịp thay đổi ràng buộc trước)
2) AnimTrack (base motion)
3) PoseIKTrack (đè IK lên pose)
4) MorphTrack (hình dạng/biểu cảm)
5) ExpressionTrack (preset, override tinh)
6) CameraTrack (vị trí/ngắt cảnh)
7) FxLightTrack (ánh sáng/hậu kỳ)
8) AudioTrack (đồng bộ audio theo final time)

#### Quy tắc blend & ưu tiên chung
- **Clip overlap** trong cùng track:
  - Nếu track hỗ trợ blend: crossfade theo `fadeIn/fadeOut` hoặc `curve`.
  - Nếu không: ưu tiên clip có `priority` cao, hoặc clip nằm **muộn hơn** (right-most wins).
- **Xung đột đa-track** (vd. Morph vs Expression): áp **thứ tự Evaluate** như trên hoặc cho phép config `Order`.

## Output mong muốn
- Code C# cho Unity: `MiniTimelineDirector`, interface track/clip, ví dụ AnimTrack & MorphTrack.
- Hướng dẫn tạo thêm track mới (Audio/Camera/Event).
- Ví dụ JSON project + code load vào runtime.
- UI layout gợi ý (UGUI/UIToolkit).
- Checklist test: play, pause, scrub, snap, blend.

Hãy viết mã rõ ràng, comment đầy đủ, dễ bảo trì.
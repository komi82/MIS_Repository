# オプション機能実装ガイド

このドキュメントは、ゲームに追加されたオプション機能の設定方法と使用方法を説明します。

## 概要

以下の機能が追加されました：

1. **BGM音量調整** - スライダーで0～100%の間で調整可能
2. **SE（効果音）音量調整** - スライダーで0～100%の間で調整可能  
3. **キー操作ガイド表示/非表示** - トグルスイッチで表示状態を切り替え可能
4. **設定の永続化** - PlayerPrefsを使用してオプション設定を保存

## 新規追加スクリプト

### 1. OptionsManager.cs
**役割**: オプション設定を一元管理するシングルトンマネージャー

**主な機能**:
- PlayerPrefsを使用した設定の永続化
- BGM/SE音量の設定・取得
- キー操作ガイド表示設定の管理
- 変更時のコールバック機能
- デフォルト値へのリセット機能

**デフォルト値**:
```
BGM音量: 0.8 (80%)
SE音量: 1.0 (100%)
キー操作ガイド表示: true (表示)
```

### 2. OptionsUIPanel.cs
**役割**: オプション画面のUIパネルを制御

**必要なUI要素**:
- `Slider` (bgmVolumeSlider) - BGM音量用
- `Slider` (sfxVolumeSlider) - SE音量用
- `TextMeshProUGUI` (bgmVolumeText) - BGM音量表示用
- `TextMeshProUGUI` (sfxVolumeText) - SE音量表示用
- `Toggle` (showControlGuideToggle) - キー操作ガイド表示切り替え用
- `TextMeshProUGUI` (showControlGuideLabel) - ガイド表示ラベル用
- `Button` (resetButton) - リセット用 (オプション)
- `Button` (closeButton) - 閉じるボタン用 (オプション)
- `CanvasGroup` (panelCanvasGroup) - フェードアニメーション用 (オプション)

**主な機能**:
- スライダーからのユーザー入力を処理
- UI要素の表示更新
- フェードイン/フェードアウトアニメーション
- 設定のリセット機能

### 3. ControlGuideVisibilityController.cs
**役割**: キー操作ガイドの表示/非表示を制御

**必要なUI要素**:
- `controlGuideObjects` リスト - ガイドオブジェクトの参照
- `CanvasGroup` - フェードアニメーション用

**主な機能**:
- OptionsManagerの設定変更をリッスン
- ガイドのフェードイン/フェードアウト
- 自動的に表示状態を同期

## Unity内での設定手順

### ステップ1: オプションマネージャーのセットアップ

1. 新しいGameObjectを作成し、「OptionsManager」と命名
2. 以下のコンポーネントを追加:
   - OptionsManager.cs (Script)
3. インスペクターで以下を設定:
   - SoundManager.Instanceが自動的に参照されることを確認

### ステップ2: オプション画面UIの作成

1. Canvas内に新しいPanel (OptionsUIPanel) を作成
2. 以下のUIコンポーネントを配置:

**BGM音量**:
```
Panel
├─ Label: "BGM音量"
├─ Slider (bgmVolumeSlider)
└─ Text: "80%" (bgmVolumeText)
```

**SE音量**:
```
Panel
├─ Label: "SE音量"
├─ Slider (sfxVolumeSlider)
└─ Text: "100%" (sfxVolumeText)
```

**キー操作ガイド**:
```
Panel
├─ Label: "キー操作ガイド"
├─ Toggle (showControlGuideToggle)
└─ Text: "表示する" (showControlGuideLabel)
```

**ボタン**:
```
Panel
├─ Button "リセット" (resetButton)
└─ Button "閉じる" (closeButton)
```

3. OptionsUIPanel.csをPanelに追加
4. インスペクターで各UI要素を割り当て

### ステップ3: キー操作ガイドの制御設定

1. キー操作ガイドのメインコンテナに、ControlGuideVisibilityController.csを追加
2. インスペクターで以下を設定:
   - `controlGuideObjects`: ガイドを表示/非表示にしたいすべてのオブジェクト
   - `canvasGroup`: ガイドのCanvasGroup (フェード用)

## 使用方法

### プログラムからの利用

```csharp
// 現在の設定を取得
float bgmVolume = OptionsManager.Instance.GetBGMVolume();
float sfxVolume = OptionsManager.Instance.GetSFXVolume();
bool showGuide = OptionsManager.Instance.GetShowControlGuide();

// 設定を変更
OptionsManager.Instance.SetBGMVolume(0.5f);
OptionsManager.Instance.SetSFXVolume(0.8f);
OptionsManager.Instance.SetShowControlGuide(false);

// デフォルト値にリセット
OptionsManager.Instance.ResetToDefault();

// 設定変更時のコールバック登録
OptionsManager.Instance.OnBGMVolumeChanged(() => {
    Debug.Log("BGM音量が変更されました");
});

// UIパネルの表示/非表示
optionsUIPanel.Open();
optionsUIPanel.Close();
```

## データの永続化

すべての設定はPlayerPrefsに保存されます。キー名は以下の通り:

- `BGM_Volume` - BGM音量 (0.0 ~ 1.0)
- `SFX_Volume` - SE音量 (0.0 ~ 1.0)
- `Show_Control_Guide` - キー操作ガイド表示 (0 = 非表示, 1 = 表示)

## トラブルシューティング

### オプション画面が表示されない
- CanvasGroupがPanelに正しく割り当てられているか確認
- すべてのUI要素がインスペクターで正しく割り当てられているか確認

### 音量変更が反映されない
- SoundManagerがシーンに存在し、初期化されているか確認
- SoundManagerのInstance参照が存在するか確認

### キー操作ガイドが切り替わらない
- ControlGuideVisibilityControllerが正しく配置されているか確認
- controlGuideObjectsリストにガイドオブジェクトが含まれているか確認
- CanvasGroupが存在し、正しく設定されているか確認

## 今後の拡張予定

- 画面輝度調整
- ゲーム難易度設定
- 言語選択
- ゲームパッド設定
- アクセシビリティオプション

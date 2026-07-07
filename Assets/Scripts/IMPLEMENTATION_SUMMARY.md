# ゲームオプション機能 - 実装完了

## 📋 概要

ゲームに以下のオプション機能が追加されました：

✅ **BGM音量調整** (スライドバー: 0～100%)
✅ **SE音量調整** (スライドバー: 0～100%)  
✅ **キー操作ガイド表示/非表示** (トグル)
✅ **設定の自動保存** (PlayerPrefs)

---

## 📁 追加ファイル一覧

### スクリプト
| ファイル名 | 説明 |
|-----------|------|
| **OptionsManager.cs** | オプション設定を一元管理するマネージャー |
| **OptionsUIPanel.cs** | オプション画面のUI制御 |
| **ControlGuideVisibilityController.cs** | キー操作ガイドの表示/非表示制御 |
| **OptionsButtonController.cs** | オプションボタンの実装例 |

### ドキュメント
| ファイル名 | 説明 |
|-----------|------|
| **OPTIONS_GUIDE.md** | 詳細な実装ガイド |
| **IMPLEMENTATION_SUMMARY.md** | この実装サマリー |

---

## 🎮 機能説明

### 1. BGM・SE音量調整
- Sliderコンポーネントで直感的に調整可能
- リアルタイムに音量が変更される
- パーセンテージ表示で現在の音量を確認可能
- SoundManagerと自動的に連携

### 2. キー操作ガイド表示/非表示
- Toggleコンポーネントで簡単に切り替え可能
- チェックボックスで設定状態を視覚化
- 変更時に自動的にガイドUI（または指定オブジェクト）のフェード表示/非表示
- 複数のガイドオブジェクトに対応

### 3. 設定の永続化
- PlayerPrefsを使用してデータ保存
- アプリケーション再起動後も設定を保持
- ゲーム終了時に自動保存

---

## 🚀 クイックスタート

### 最小限の設定手順

#### 1️⃣ OptionsManager のセットアップ
```
1. Hierarchy で右クリック → Create Empty
2. 名前を「OptionsManager」に変更
3. Inspector で Add Component → OptionsManager
4. 完了！
```

#### 2️⃣ オプション画面UIの作成
```
1. MainUICanvas内に新しいPanel作成
2. Panel に以下のUI要素を配置:
   - BGM音量スライダー (Slider)
   - SE音量スライダー (Slider)
   - キー操作ガイド トグル (Toggle)
   - ボタン (Button) - 任意

3. Panel に OptionsUIPanel.cs を Add Component
4. Inspector でUI要素を割り当て
5. 完了！
```

#### 3️⃣ キー操作ガイドの制御（オプション）
```
1. ガイドUIのルートGameObjectを選択
2. Add Component → ControlGuideVisibilityController
3. controlGuideObjects にガイドオブジェクトを登録
4. 完了！
```

---

## 📝 サンプルコード

### オプション画面を開く
```csharp
// UIボタンから開く場合
optionsUIPanel.Open();

// キーボードショートカット
if (Input.GetKeyDown(KeyCode.O))
{
    optionsUIPanel.Open();
}
```

### 現在の設定を取得
```csharp
float bgmVol = OptionsManager.Instance.GetBGMVolume();
float sfxVol = OptionsManager.Instance.GetSFXVolume();
bool showGuide = OptionsManager.Instance.GetShowControlGuide();

Debug.Log($"BGM: {bgmVol * 100}%, SE: {sfxVol * 100}%, ガイド: {showGuide}");
```

### 設定を変更
```csharp
OptionsManager.Instance.SetBGMVolume(0.5f);  // 50%に設定
OptionsManager.Instance.SetShowControlGuide(false);  // ガイド非表示
```

### デフォルト値にリセット
```csharp
OptionsManager.Instance.ResetToDefault();
```

---

## 🔧 カスタマイズ例

### デフォルト値を変更したい場合
**OptionsManager.cs** の以下の部分を編集:
```csharp
private const float DEFAULT_BGM_VOLUME = 0.8f;     // ← BGMデフォルト値
private const float DEFAULT_SFX_VOLUME = 1f;       // ← SEデフォルト値
private const bool DEFAULT_SHOW_CONTROL_GUIDE = true;  // ← ガイド表示デフォルト
```

### UIのフェード時間を変更したい場合
**OptionsUIPanel.cs** または **ControlGuideVisibilityController.cs** の以下を編集:
```csharp
[SerializeField]
private float fadeAnimationTime = 0.3f;  // ← 秒単位（0.3秒 = 300ms）
```

### キーボードショートカットを変更したい場合
**OptionsButtonController.cs** の以下を編集:
```csharp
[SerializeField]
private KeyCode optionsMenuKey = KeyCode.O;  // ← ショートカットキー
```

---

## ✨ 主な特徴

✅ **シングルトンパターン** - ゲーム全体で統一的な設定管理
✅ **イベント駆動** - コールバック機能で柔軟な拡張性
✅ **PlayerPrefs統合** - 自動的に設定を保存
✅ **SoundManager統合** - 既存の音声システムとシームレスに動作
✅ **フェードアニメーション** - プロフェッショナルな見た目
✅ **日本語対応** - 日本語のUIラベルに対応
✅ **スケーラブル** - 新しいオプション項目を簡単に追加可能

---

## 🐛 トラブルシューティング

### 音量が変更されない
```
チェック項目:
- SoundManager が Hierarchy に存在するか
- SoundManager.Instance が null でないか
```

### ガイドが切り替わらない
```
チェック項目:
- ControlGuideVisibilityController が割り当てられているか
- controlGuideObjects リストにオブジェクトが含まれているか
- CanvasGroup が存在するか
```

### 設定が保存されない
```
チェック項目:
- PlayerPrefs.Save() が自動的に呼ばれているか
- デバイスのストレージ容量は充分か
```

---

## 📚 ファイル詳細

### OPTIONS_GUIDE.md
詳細な実装方法とAPI仕様が記載されています。
UI要素の具体的な配置方法やコード例を参照してください。

### スクリプトのコメント
各スクリプトには詳細なXMLコメントが記載されています。
IntelliSenseで機能の説明を確認できます。

---

## 🎯 次のステップ（オプション拡張例）

- 画面輝度調整
- ゲーム難易度設定
- 言語選択
- ゲームパッド設定
- アクセシビリティオプション

---

## 📞 サポート

不明な点や問題がある場合は、OPTIONS_GUIDE.md を参照するか、
各スクリプトのコメントを確認してください。

---

**実装完了日**: 2026-07-07
**バージョン**: 1.0

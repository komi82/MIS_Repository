# チュートリアル・アイコン 仮調整

Ryzくんの本対応前に確認できるよう、既存素材を使った仮案として分離しています。

## 変更内容

- 追加バフ17種類の黒曜石の仮アイコンを既存スプライトへ割り当て。元から専用アイコンがあるアイテムと鋼鴉さんの10種は維持。
- 全8レッスンの説明枠・人物画像・本文・ページ案内を共通配置に整理。操作案内も同じ位置・余白に統一。
- 最後のレッスンにショップの商品説明・購入・EXIT・所持品確認、2日目からの日替わり苦難と表示場所を追加。新しい購入体験や苦難の実演は設けていません。

## 後から変更する場所

- 文言: Assets/Scenes/tutorial/tutorial8.unity の tutorialManager / TutorialSequence / Tutorial Lines（12ページ）。
- 説明枠: Assets/Scripts/TutorialPresentation.cs。
- 練習中の操作案内: Assets/Scripts/TutorialControlGuide.cs。
- アイコン: 以下のプレハブの Image / Source Image。画像の生成・加工はなし。

| プレハブ | 仮割り当て |
| --- | --- |
| BlacksmithClickReduction | tutorial_icon_build |
| BlacksmithFrequency | swords |
| BlacksmithSpecialist | cta_iron_pendant |
| DailyIncome | coins |
| DeliveryStreak | cta_manuscript |
| DoublePurificationLiquid | Assets/UI/inventory/ItemImage/浄化液.png |
| LuckyDelivery | cta_wax_seal |
| MixingFrequency | Assets/UI/inventory/ItemImage/回復薬.png |
| MixingSpecialist | cta_research_points |
| MushroomReward | Assets/UI/inventory/ItemImage/ドクタケ.png |
| OreRetention | cta_obsidianjade |
| PurificationFrequency | cta_ankh |
| PurificationSpecialist | cta_suns_blessing |
| PurificationWindowExpansion | cta_jade |
| RequestForecast | wonder_orb |
| SlowReward | icon_hide |
| TableAging | cta_barrel |

## 確認

Unityメニュー MIS > テスト > チュートリアルのマウス操作を検証。全8シーンの台詞の収まり・操作入力・アイコン参照を確認します。プレイ感や説明文の最終判断はRyzくんと相談して調整する前提です。

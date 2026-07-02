using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;


/// <summary>
/// アイテム配置・クラフト入力・各作業ステーションUIを統合制御する中核クラス。
/// `InventoryManager` `PlacementSlots` `RecipeDatabase` `SoundManager` などと連携して、
/// 配置可否判定、レシピ照合、ミニゲーム完了後の生成処理を行う。
/// </summary>
public class PutItem : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private SlotSelector slotselector;
    [SerializeField] private ItemPickup itempickup;
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private Camera mainCamera;
    [Header("アイテム設置")]
	[SerializeField] private float placementOffset = 0.5f; // 任意の高さ（スロットTransform未設定時の後方互換）
	[SerializeField] private RecipeDatabase recipeDatabase; // craft 用
	[SerializeField] private RecipeDatabase weaponRecipeDatabase; // blacksmith 用
	[SerializeField] private RecipeDatabase washRecipeDatabase;   // wash 用
	[SerializeField] private Slider powerGageSlider;
	[SerializeField] private Slider washSlider;
	//[SerializeField] private TMPro.TextMeshProUGUI washText;
	[SerializeField] private Image blacksmithImageA; // 画像A（レシピresultItemのiconを表示）
	[SerializeField] private Image[] blacksmithImagesB; // 画像B群（複数指定可）
	[SerializeField] private FirstPersonController playerController; // 視点/WASD制御の無効化用
	[SerializeField] private DeliveryStation deliveryStation; // DeliveryStationのCursorActive連動
	
	[Header("タグ別UI表示")]
	[SerializeField] private GameObject craftPromptUI; // craft用UI（画面内固定位置）
	[SerializeField] private GameObject blacksmithPromptUI; // blacksmith用UI（画面内固定位置）
	[SerializeField] private GameObject washPromptUI; // wash用UI（画面内固定位置）
	[SerializeField] private GameObject putPromptUI; // E キー用UI（アイテムを置ける場合）
	[SerializeField] private TextMeshProUGUI putPromptText; // E キーUI用テキスト
	[SerializeField] private GameObject recipePromptUI; // Recipe タグ用UI（画面内固定位置）
	[SerializeField] private GameObject karasuPromptUI; // karasu タグ用UI（画面内固定位置）


	private string scene;

	private string[] targets = { SceneNames.Tutorial3, SceneNames.Tutorial5, SceneNames.Tutorial6 };

	// 開発環境（60fps）での per-frame 値を基準にした秒あたりの変化量
	private const float ReferenceFrameRate = 60f;
	private const float PowerGageDecayPerSecond = 0.05f * ReferenceFrameRate;
	private const float PowerGageIncreasePerSecond = 0.15f * ReferenceFrameRate;
	private const float WashSliderMovePerSecond = 0.025f * ReferenceFrameRate;

	// PowerGage関連の変数
	private float powerGagePower;
	private bool isPowerGageCompleted = false;
	
	// SliderMove関連の変数
	private bool isWashClicked = false;
	private bool isWashMaxValue = false;
	private bool isWashCompleted = false;
	
	// コルーチン処理中のキー無効化用フラグ
	private bool isProcessingCoroutine = false;
	
	// UI表示管理用変数
	private GameObject currentPromptUI;
	private string currentTargetTag;
	private GameObject lastTargetObject;
	private bool isCraftingInProgress = false; // クラフト処理中かどうか
	private bool recipePromptSuppressed; // Esc/F で閉じたあと、視線が外れるまで再表示しない


	void Awake(){
		scene = SceneManager.GetActiveScene().name;
	}

	void Start()
	{
		// 初期表示は全て非表示
		if (powerGageSlider != null) powerGageSlider.gameObject.SetActive(false);
		if (washSlider != null) washSlider.gameObject.SetActive(false);
		if (blacksmithImageA != null) blacksmithImageA.gameObject.SetActive(false);
		if (blacksmithImagesB != null)
		{
			for (int i = 0; i < blacksmithImagesB.Length; i++)
			{
				if (blacksmithImagesB[i] != null) blacksmithImagesB[i].gameObject.SetActive(false);
			}
		}
		
		// タグ別UIの初期化
		if (craftPromptUI != null) craftPromptUI.SetActive(false);
		if (blacksmithPromptUI != null) blacksmithPromptUI.SetActive(false);
		if (washPromptUI != null) washPromptUI.SetActive(false);
		if (putPromptUI != null) putPromptUI.SetActive(false);
		if (recipePromptUI != null) recipePromptUI.SetActive(false);
		if (karasuPromptUI != null) karasuPromptUI.SetActive(false);
	}

	private void BeginPlayerUiBlock()
	{
		if (playerController != null) playerController.enabled = false;
		if (deliveryStation != null)
		{
			deliveryStation.CursorActive = true;
		}
		Cursor.lockState = CursorLockMode.Confined; // ゲームウィンドウ内に制限＆表示
		Cursor.visible = true; // カーソルを明示的に表示
	}

	private void EndPlayerUiBlock()
	{
		if (deliveryStation != null)
		{
			deliveryStation.CursorActive = false;
		}
		Cursor.lockState = CursorLockMode.Locked; // 画面中央に固定＆非表示
		Cursor.visible = false; // カーソルを明示的に非表示
		if (playerController != null) playerController.enabled = true;
	}

	// タグに応じたUIを表示するメソッド（ItemPickupスタイル）
	private void ShowUIForTag(string tag)
	{
		Debug.Log($"ShowUIForTag called: tag={tag}");
		
		// 既に同じタグを表示している場合は何もしない
		if (currentTargetTag == tag) return;
		
		// 現在表示中のUIを非表示にする
		HideCurrentUI();
		
		// 新しいUIを表示
		GameObject uiToShow = null;
		
		switch (tag)
		{
			case "craft":
				uiToShow = craftPromptUI;
				break;
			case "blacksmith":
				uiToShow = blacksmithPromptUI;
				break;
			case "wash":
				uiToShow = washPromptUI;
				break;
			case "Recipe":
				uiToShow = recipePromptUI;
				break;
			case "karasu":
				uiToShow = karasuPromptUI;
				break;
		}
		
		if (uiToShow != null)
		{
			Debug.Log($"Activating UI: {uiToShow.name}");
			uiToShow.SetActive(true);
			currentPromptUI = uiToShow;
			currentTargetTag = tag;
		}
		else
		{
			Debug.LogWarning($"UI for tag '{tag}' is null! Make sure to assign the UI prefab in the inspector.");
		}
	}
	
	// データベースにマッチするレシピがあるかチェックするメソッド
	private bool HasMatchingRecipe(GameObject taggedObject, PlacementSlots slotsOnParent)
	{
		// PlacementSlotsを取得
		PlacementSlots slots = slotsOnParent != null ? slotsOnParent : taggedObject.GetComponent<PlacementSlots>();
		if (slots == null)
		{
			Debug.Log("No PlacementSlots found");
			return false;
		}
		
		// タグに応じて参照するデータベースを決定
		RecipeDatabase activeDB = null;
		if (taggedObject.CompareTag("craft")) activeDB = recipeDatabase;
		else if (taggedObject.CompareTag("blacksmith")) activeDB = weaponRecipeDatabase;
		else if (taggedObject.CompareTag("wash")) activeDB = washRecipeDatabase;
		
		if (activeDB == null)
		{
			Debug.Log($"No database found for tag: {taggedObject.tag}");
			return false;
		}
		
		// 現在のスロットの組み合わせを取得
		var combo = slots.GetCombination();
		RecipeData match = activeDB.FindMatch(combo.Item1, combo.Item2);
		
		bool hasMatch = match != null;
		Debug.Log($"Recipe match for {taggedObject.tag}: {hasMatch}");
		if (hasMatch)
		{
			Debug.Log($"Matching recipe: {match.resultItem?.itemName}");
		}
		
		return hasMatch;
	}
	
	// 現在表示中のUIを非表示にするメソッド
	private void HideCurrentUI()
	{
		if (currentPromptUI != null)
		{
			currentPromptUI.SetActive(false);
			currentPromptUI = null;
		}
		if (putPromptUI != null)
		{
			putPromptUI.SetActive(false);
		}
		currentTargetTag = "";
		lastTargetObject = null;
	}

	private void CloseRecipePromptUI()
	{
		if (recipePromptUI != null)
		{
			recipePromptUI.SetActive(false);
		}
		if (currentPromptUI == recipePromptUI)
		{
			currentPromptUI = null;
			currentTargetTag = "";
		}
		recipePromptSuppressed = true;
		EndPlayerUiBlock();

		if (scene == SceneNames.Tutorial7)
		{
			ConditionalSceneTransition.TriggerTransitionStatic();
		}
	}
	
	// E キー用UIを表示するメソッド
	private void ShowPutPromptUI(ItemData itemToPlace)
	{
		if (putPromptUI == null || putPromptText == null) return;
		
		if (itemToPlace != null)
		{
			putPromptUI.SetActive(true);
			PromptUIUtility.SetTextAndResizeWidth(
				putPromptText,
				putPromptUI.GetComponent<RectTransform>(),
				$"<sprite name=E> 置く：{itemToPlace.itemName}");
		}
		else
		{
			putPromptUI.SetActive(false);
		}
	}
	
	// E キー用UIを非表示にするメソッド
	private void HidePutPromptUI()
	{
		if (putPromptUI != null)
		{
			putPromptUI.SetActive(false);
		}
	}
	
	


    void Update()
    {
		if (!isProcessingCoroutine && recipePromptUI != null && recipePromptUI.activeSelf
			&& Input.GetKeyDown(KeyCode.Escape))
		{
			CloseRecipePromptUI();
			return;
		}

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
			GameObject targetObject = hit.collider.gameObject;
			var slotsOnParent = targetObject.GetComponentInParent<PlacementSlots>();
			bool isCraftTarget = targetObject.CompareTag("craft") || targetObject.CompareTag("blacksmith") || targetObject.CompareTag("wash") || targetObject.CompareTag("put") || targetObject.CompareTag("Recipe") || targetObject.CompareTag("karasu") || (slotsOnParent != null);

			if (isCraftTarget)
            {
				// タグに応じたUIを表示
				GameObject taggedObject = targetObject;
				if (slotsOnParent != null) taggedObject = slotsOnParent.gameObject;
				
				string objectTag = taggedObject.tag;
				if (objectTag != "Recipe")
				{
					recipePromptSuppressed = false;
				}
				if (objectTag == "craft" || objectTag == "blacksmith" || objectTag == "wash")
				{
					// クラフト処理中はUIを表示しない
					if (isCraftingInProgress)
					{
						HideCurrentUI();
						HidePutPromptUI();
						return;
					}
					
					// 対象オブジェクトが変更された場合はUIを非表示にする
					if (lastTargetObject != null && lastTargetObject != taggedObject)
					{
						HideCurrentUI();
						HidePutPromptUI();
					}
					
					// データベースにマッチするレシピがある場合のみUIを表示
					if (HasMatchingRecipe(taggedObject, slotsOnParent))
					{
						ShowUIForTag(objectTag);
						HidePutPromptUI(); // レシピマッチ時はput UIは非表示
						lastTargetObject = taggedObject;
					}
					else
					{
						// レシピがマッチしない場合はクラフトUIは非表示
						HideCurrentUI();
						lastTargetObject = null;
						
						// インベントリに選択中のアイテムがあり、かつ空きスロットがある場合のみputUIを表示
						ItemData selectedItem = inventoryManager != null ? (inventoryManager.selectedItem ?? inventoryManager.GetSlot(slotselector.selectedIndex)?.CurrentItem) : null;
						PlacementSlots slots = slotsOnParent != null ? slotsOnParent : targetObject.GetComponent<PlacementSlots>();
						if (selectedItem != null && slots != null && slots.HasEmptySlot())
						{
							ShowPutPromptUI(selectedItem);
						}
						else
						{
							HidePutPromptUI();
						}
					}
				}
				// putタグの場合
				else if (objectTag == "put")
				{
					// まず他のUIを非表示
					HideCurrentUI();
					// putタグは PlacementSlots が無い場合も配置可。Slots があるなら空き時のみ表示
					ItemData selectedItem = inventoryManager != null ? (inventoryManager.selectedItem ?? inventoryManager.GetSlot(slotselector.selectedIndex)?.CurrentItem) : null;
					PlacementSlots slots = slotsOnParent != null ? slotsOnParent : targetObject.GetComponent<PlacementSlots>();
					bool canShow = false;
					if (selectedItem != null)
					{
						if (slots == null)
						{
							canShow = true; // スロット無しでもOK
						}
						else
						{

							canShow = slots.HasEmptySlot();
						}
					}
					if (canShow) ShowPutPromptUI(selectedItem); else HidePutPromptUI();
				}
				// Recipe タグの場合
				else if (objectTag == "Recipe")
				{
					if (!recipePromptSuppressed)
					{
						ShowUIForTag("Recipe");
					}
					HidePutPromptUI();

					if (!isProcessingCoroutine && Input.GetKeyDown(KeyCode.F)
						&& recipePromptUI != null && recipePromptUI.activeSelf
						&& taggedObject.GetComponent<RecipeStation>() == null)
					{
						CloseRecipePromptUI();
						return;
					}
				}
				// karasu タグの場合
				else if (objectTag == "karasu")
				{
					ShowUIForTag("karasu");
					HidePutPromptUI();
				}
				
				// コルーチン処理中はR、E、Fキーを無効化
				if (!isProcessingCoroutine)
				{
					if (Input.GetKeyDown(KeyCode.E))
					{
						slotselector.SelectSlot(slotselector.selectedIndex);
						PutSelectedItem();
					}

					// Rキーでクラフト結果を生成
					if (Input.GetKeyDown(KeyCode.R))
					{
						PlacementSlots slots = slotsOnParent != null ? slotsOnParent : targetObject.GetComponent<PlacementSlots>();
						// タグに応じて参照するデータベースを切替
						RecipeDatabase activeDB = null;
						// taggedObjectは既に上で宣言されているので、再宣言は不要
						if (taggedObject.CompareTag("craft")) activeDB = recipeDatabase;
						else if (taggedObject.CompareTag("blacksmith")) activeDB = weaponRecipeDatabase;
						else if (taggedObject.CompareTag("wash")) activeDB = washRecipeDatabase;

						if (slots != null && activeDB != null)
						{
							// コルーチン開始直前にUIを非表示にする
							HideCurrentUI();
							HidePutPromptUI();
							// クラフト処理開始フラグを設定
							isCraftingInProgress = true;
							StartCoroutine(ProcessCrafting(slots, activeDB, taggedObject, hit, targetObject));
						}
						else
						{
							if (slots == null) Debug.LogWarning("PlacementSlots が見つかりません。対象または親に付与してください");
							if (activeDB == null) Debug.LogWarning("対応する RecipeDatabase が未設定です（craft/blacksmith/wash を確認）");
						}
					}
					
				}
				
            }
            else
            {
				recipePromptSuppressed = false;
                // 範囲内にクラフト対象がない場合はUIを非表示
                HideCurrentUI();
                HidePutPromptUI();
            }
        }
        else
        {
			recipePromptSuppressed = false;
            // レイキャストが何も当たらない場合はUIを非表示
            HideCurrentUI();
            HidePutPromptUI();
        }

    }


	private IEnumerator ProcessCrafting(PlacementSlots slots, RecipeDatabase activeDB, GameObject taggedObject, RaycastHit hit, GameObject targetObject)
	{
		// コルーチン処理開始時にフラグを設定
		isProcessingCoroutine = true;
		
		var combo = slots.GetCombination();
		RecipeData match = activeDB.FindMatch(combo.Item1, combo.Item2);
		
		if (match != null && match.resultItem != null && match.resultItem.prefab != null)
		{
			// PowerGageの処理が必要な場合
			if (taggedObject.CompareTag("craft"))
			{
				// PowerGageの完了を待つ
				yield return StartCoroutine(WaitForPowerGageCompletion());
			}
		else if (taggedObject.CompareTag("wash"))
			{
				// Washの完了を待つ
				yield return StartCoroutine(WaitForWashCompletion());
			}
		else if (taggedObject.CompareTag("blacksmith"))
		{
			// Blacksmith用のUI完了を待つ
			yield return StartCoroutine(WaitForBlacksmithCompletion(match));
		}

            // PowerGageの処理が完全に終了してから以下の処理を実行
            yield return null; // 1フレーム待機してから処理を続行

			Transform anchor = slots.GetResultAnchor();
			Vector3 pos = anchor != null ? anchor.position : hit.point + targetObject.transform.up * placementOffset;
			Quaternion rot = anchor != null ? anchor.rotation : Quaternion.identity;

			slots.ClearAllAndDestroyChildren();


			// 基本的な効果音再生
			if (SoundManager.Instance != null)
			{
				SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.recipeCompleteSound);
			}
						Instantiate(match.resultItem.prefab, pos, rot);
			Debug.Log($"クラフト生成: {match.resultItem.itemName}");
			if (targets.Contains(scene))

			{
				ConditionalSceneTransition.TriggerTransitionStatic();
			}

		}
		else
		{
			Debug.Log("クラフト可能なレシピがありません");
		}
		
		// コルーチン処理終了時にフラグをリセット
		isProcessingCoroutine = false;
		// クラフト処理終了フラグを設定
		isCraftingInProgress = false;
	}

	private IEnumerator WaitForBlacksmithCompletion(RecipeData match)
	{
		BeginPlayerUiBlock();
		// 画像A: レシピのresultItemのiconを表示
		if (blacksmithImageA != null)
		{
			Sprite icon = match != null && match.resultItem != null ? match.resultItem.icon : null;
			blacksmithImageA.sprite = icon;
			// 比率が崩れないように表示を固定
			blacksmithImageA.preserveAspect = true;
			blacksmithImageA.gameObject.SetActive(true);
		}

		// 画像B群を表示
		if (blacksmithImagesB != null)
		{
			for (int i = 0; i < blacksmithImagesB.Length; i++)
			{
				if (blacksmithImagesB[i] != null)
				{
					blacksmithImagesB[i].gameObject.SetActive(true);
					// 画像Aの不透明領域上にランダム配置
					TryPlaceImageBOnOpaqueOfA(blacksmithImagesB[i], blacksmithImageA);
				}
			}
		}

		while (true)
		{
			// 画像Bのアクティブ数をカウント
			int count = 0;
			if (blacksmithImagesB != null)
			{
				for (int i = 0; i < blacksmithImagesB.Length; i++)
				{
					if (blacksmithImagesB[i] != null && blacksmithImagesB[i].gameObject.activeInHierarchy)
					{
						count++;
					}
				}
			}

			// 0で終了
			if (count == 0)
			{
				if (blacksmithImageA != null) blacksmithImageA.gameObject.SetActive(false);
				EndPlayerUiBlock();
				break;
			}

			yield return null;
		}
	}

	// 画像Aの不透明領域上に画像Bをランダム配置する
	private void TryPlaceImageBOnOpaqueOfA(Image imageB, Image imageA)
	{
		if (imageB == null || imageA == null || imageA.sprite == null) return;

		// 画像Bを画像Aの子にして、座標系を一致させる
		var rtB = imageB.rectTransform;
		var rtA = imageA.rectTransform;
		if (rtB.parent != rtA)
		{
			rtB.SetParent(rtA, false);
		}
		// アンカーとピボットを中央に揃える
		rtB.anchorMin = new Vector2(0.5f, 0.5f);
		rtB.anchorMax = new Vector2(0.5f, 0.5f);
		rtB.pivot = new Vector2(0.5f, 0.5f);

		Vector2 pos;
		if (TryGetRandomOpaqueAnchoredPosition(imageA, out pos, 200, 0.2f))
		{
			rtB.anchoredPosition = pos;
		}
	}

	// 画像A（Image）のスプライトの不透明画素上に対応するAnchoredPositionを取得
	private bool TryGetRandomOpaqueAnchoredPosition(Image imageA, out Vector2 anchoredPos, int maxAttempts, float alphaThreshold)
	{
		anchoredPos = Vector2.zero;
		var sprite = imageA != null ? imageA.sprite : null;
		if (sprite == null) return false;

		Texture2D tex = sprite.texture;
		if (tex == null) return false;

		// スプライト内のピクセル矩形
		Rect spRect = sprite.rect; // ピクセル単位
		var rtA = imageA.rectTransform;
		Vector2 rectSize = rtA.rect.size; // UI上の矩形サイズ（ローカル）

		// preserveAspect時のレターボックス/ピラーボックスを考慮
		float scale = Mathf.Min(rectSize.x / spRect.width, rectSize.y / spRect.height);
		float drawW = spRect.width * scale;
		float drawH = spRect.height * scale;
		float padX = (rectSize.x - drawW) * 0.5f; // 左右の余白（ローカル）
		float padY = (rectSize.y - drawH) * 0.5f; // 上下の余白（ローカル）

		for (int attempt = 0; attempt < maxAttempts; attempt++)
		{
			int px = Mathf.RoundToInt(Random.Range(spRect.x, spRect.xMax - 1));
			int py = Mathf.RoundToInt(Random.Range(spRect.y, spRect.yMax - 1));

			Color c = tex.GetPixel(px, py);
			if (c.a < alphaThreshold) continue; // 不透明閾値未満はスキップ

			// ピクセル→スプライト内正規化(0..1)
			float nx = (px - spRect.x) / spRect.width;
			float ny = (py - spRect.y) / spRect.height;

			// スプライト中央原点のローカル座標へ変換（描画領域内）
			float lx = (nx - 0.5f) * drawW;
			float ly = (ny - 0.5f) * drawH;

			// レターボックス分をオフセット。RectTransformの原点は中央（pivot=0.5 assumed）
			float ax = lx; // 中央原点
			float ay = ly;
			// 余白は中央原点では0のため、パディングは不要。アンカー中央ならそのままでOK

			anchoredPos = new Vector2(ax, ay);
			return true;
		}

		return false;
	}

	private IEnumerator WaitForPowerGageCompletion()
	{
		// PowerGageのnullチェック
		if (powerGageSlider == null)
		{
			Debug.LogError("PowerGage Slider が null です。Inspector で PowerGage Slider を設定してください。");
			yield break;
		}
		
		// PowerGageを開始
		isPowerGageCompleted = false;
		powerGagePower = 0;
		powerGageSlider.gameObject.SetActive(true);
		BeginPlayerUiBlock();
		
		Debug.Log("PowerGage開始");
		
		// PowerGageミニゲームの処理
		while (!isPowerGageCompleted)
		{
			float deltaTime = Time.deltaTime;

			// パワーの減少
			if (powerGagePower > 0)
			{
				powerGagePower -= PowerGageDecayPerSecond * deltaTime;
			}
			
			// スペースキーでパワー増加
			if (Input.GetKey(KeyCode.Space))
			{
				if (powerGagePower < 10)
				{
					powerGagePower += PowerGageIncreasePerSecond * deltaTime;
				}
				if (powerGagePower >= 10)
				{
					// 完了
					isPowerGageCompleted = true;
					powerGageSlider.gameObject.SetActive(false);
					Debug.Log("PowerGage完了");
				}
			}
			
			// スライダーの更新
			powerGageSlider.value = powerGagePower * 0.1f;
			
			yield return null;
		}
		EndPlayerUiBlock();
	}

	private IEnumerator WaitForWashCompletion()
	{
		// Wash Sliderのnullチェック
		if (washSlider == null)
		{
			Debug.LogError("Wash Slider が null です。Inspector で Wash Slider を設定してください。");
			yield break;
		}
		
		// Wash処理を開始
		isWashCompleted = false;
		isWashClicked = false;
		isWashMaxValue = false;
		washSlider.value = 0;
		washSlider.gameObject.SetActive(true); // スライダーを表示
		BeginPlayerUiBlock();
		
		Debug.Log("Wash処理開始");
		
		// Washミニゲームの処理
		while (!isWashCompleted)
		{
			float deltaTime = Time.deltaTime;

			// スペースキーでクリック状態を切り替え
			if (Input.GetKeyDown(KeyCode.Space))
			{
				if (isWashClicked == false)
				{
					Debug.Log("stop");
					isWashClicked = true;
				}
				else
				{
					Debug.Log("start");
					isWashClicked = false;
				}
			}
			
			// クリック状態の場合は成功判定
			if (isWashClicked)
			{
				if (washSlider.value >= 0.4 && washSlider.value <= 0.6)
				{
					// 完了
					isWashCompleted = true;
					washSlider.gameObject.SetActive(false); // スライダーを非表示
					Debug.Log("Wash完了");
				}
				yield return null;//失敗処理を入れるとしたらこのへん
				continue;
			}
			
			// スライダーの自動移動
			if (washSlider.value >= 1)
			{
				isWashMaxValue = true;
			}
			if (washSlider.value <= 0)
			{
				isWashMaxValue = false;
			}
			
			float washMove = WashSliderMovePerSecond * deltaTime;
			if (isWashMaxValue == true)
			{
				washSlider.value -= washMove;
			}
			else
			{
				washSlider.value += washMove;
			}
			
			yield return null;
		}
		EndPlayerUiBlock();
	}

	void PutSelectedItem()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
			GameObject targetObject = hit.collider.gameObject;
			var slotsOnParent = targetObject.GetComponentInParent<PlacementSlots>();
			bool isCraftTarget = targetObject.CompareTag("craft") || targetObject.CompareTag("blacksmith") || targetObject.CompareTag("wash") || targetObject.CompareTag("put") || (slotsOnParent != null);

			if (isCraftTarget)
            {
                ItemData itemToPlace = inventoryManager.selectedItem != null ? inventoryManager.selectedItem : inventoryManager.GetSlot(slotselector.selectedIndex)?.CurrentItem;

                if (itemToPlace != null)
                {
                    if (inventoryManager == null)
                    {
                        Debug.LogWarning("InventoryManagerが未設定です");
                        return;
                    }
                    if (inventoryManager.selectedItem == null)
                    {
                        Debug.LogWarning("selectedItem が未設定です");
                        return;
                    }

                    // 選択されたスロットインデックスを直接使用
                    InventorySlotUI slot = inventoryManager.GetSlot(slotselector.selectedIndex);
                    if (slot == null)
                    {
                        Debug.LogWarning("選択されたスロットが見つかりません");
                        return;
                    }
                    
                    // 選択されたスロットにアイテムがあるかチェック（selectedItemがnullでも選択中スロットの中身を優先）
                    if (slot.CurrentItem == null || slot.CurrentItem != itemToPlace)
                    {
                        Debug.LogWarning("選択されたスロットに期待されるアイテムがありません");
                        return;
                    }

					if (itemToPlace.prefab != null)
                    {
						// スロットが空いている場合のみ配置（親からも取得可）
						PlacementSlots slots = slotsOnParent != null ? slotsOnParent : targetObject.GetComponent<PlacementSlots>();
						Transform placeSlot = null;
						Vector3 spawnPosition;
						Quaternion spawnRotation = Quaternion.identity;
						if (slots != null && slots.TryPlace(itemToPlace, out placeSlot) && placeSlot != null)
						{
							spawnPosition = placeSlot.position;
							spawnRotation = placeSlot.rotation;
						}
						else
						{
							// PlacementSlots が無い or 空き無し。put タグなら床配置を許可、その他は不可
							string targetStationTag = (slotsOnParent != null ? slotsOnParent.gameObject.tag : targetObject.tag);
							if (targetStationTag == "put" && slots == null)
							{
								spawnPosition = hit.point + targetObject.transform.up * placementOffset;
							}
							else
							{
								Debug.Log("空きスロットがないため配置できません");
								return;
							}
						}

						// 生成（スロットがあれば子に、無ければ通常）
						GameObject placed = placeSlot != null
							? Instantiate(itemToPlace.prefab, spawnPosition, spawnRotation, placeSlot)
							: Instantiate(itemToPlace.prefab, spawnPosition, spawnRotation);

						// タグ名をログに反映
						string stationTag = (slotsOnParent != null ? slotsOnParent.gameObject.tag : targetObject.tag);
						if (SoundManager.Instance != null)
						{
							SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.putSound);
						}
						Debug.Log($"アイテム '{itemToPlace.itemName}' を '{stationTag}' に配置しました");
                        inventoryManager.RemoveItem(slot);

                        inventoryManager.selectedItem = null; // 選択状態も解除

						// レシピ照合（スロットがある場合のみ）
						if (slots != null)
						{
							// タグに応じて参照するデータベースを切替
							RecipeDatabase activeDB = null;
							GameObject taggedObject = (slotsOnParent != null ? slotsOnParent.gameObject : targetObject);
							if (taggedObject.CompareTag("craft")) activeDB = recipeDatabase;
							else if (taggedObject.CompareTag("blacksmith")) activeDB = weaponRecipeDatabase;
							else if (taggedObject.CompareTag("wash")) activeDB = washRecipeDatabase;

							if (activeDB != null)
							{
								var combo = slots.GetCombination();
								RecipeData match = activeDB.FindMatch(combo.Item1, combo.Item2);
							if (match != null && match.resultItem != null)
							{
								Debug.Log($"レシピ一致: {match.requiredItems[0]?.itemName} + {(match.requiredItems.Length > 1 ? match.requiredItems[1]?.itemName : "")} -> {match.resultItem.itemName}");
                            }
							else
							{
								Debug.Log("レシピ一致なし");
							}
							}
							else
							{
								Debug.LogWarning("対応する RecipeDatabase が未設定です（craft/blacksmith/wash を確認）");
							}
						}
						else
						{
							if (recipeDatabase == null && weaponRecipeDatabase == null && washRecipeDatabase == null) Debug.LogWarning("RecipeDatabase が未設定です");
							if (slots == null) Debug.LogWarning("PlacementSlots が見つかりません。対象または親に付与してください");
						}
                    }
                    else
                    {
                        Debug.LogWarning("プレハブが設定されていません。配置できませんでした");
                    }
                }
                else
                {
                    Debug.LogWarning("選択中のアイテムがありません");
                }
            }
        }
    }

}
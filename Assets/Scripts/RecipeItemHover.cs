using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// レシピ項目クリック時の必要素材/結果表示を担当するUIコンポーネント。
/// 複数 `RecipeDatabase` を横断検索し、タグ指定のUIへアイコンとテキストを反映する。
/// </summary>
public class RecipeItemHover : MonoBehaviour, IPointerClickHandler
{
    [Header("UI要素")]
    public TextMeshProUGUI itemNameText;
    public Image[] requiredItemImages; // Required Itemのスプライトを表示するImage配列（配列ベース表示時のみ使用）
    public TextMeshProUGUI resultItemNameText; // Result Itemの名前表示用TextMeshPro
    public TextMeshProUGUI requiredItem1NameText; // Required Item1の名前表示用TextMeshPro
    public TextMeshProUGUI requiredItem2NameText; // Required Item2の名前表示用TextMeshPro
    // 案内テキストはタグ 'craftway' を持つ TextMeshProUGUI を動的に取得して更新する
    
    [Header("タグベースUI表示")]
    public bool useTagBasedDisplay = true; // タグベースの表示を使用するか
    public string resultItemTag = "ans"; // ResultItem表示用のタグ
    public string requiredItem1Tag = "req1"; // RequiredItem1表示用のタグ
    public string requiredItem2Tag = "req2"; // RequiredItem2表示用のタグ
    public string resultItemNameTag = "ansTMP"; // ResultItem名前表示用のタグ
    public string requiredItem1NameTag = "reqtmp1"; // RequiredItem1名前表示用のタグ
    public string requiredItem2NameTag = "reqtmp2"; // RequiredItem2名前表示用のタグ
    
    [Header("自動検索設定")]
    public bool autoFindImages = true; // 自動でImageを検索するかどうか
    public string imageParentName = "RequiredItemImages"; // 検索する親オブジェクト名
    
    [Header("レシピデータ")]
    public RecipeDatabase recipeDatabase;
    public RecipeDatabase weaponRecipeDatabase; // 武器レシピ用データベース
    public RecipeDatabase washRecipeDatabase; // 浄化レシピ用データベース
    public string currentItemName;
    
    [Header("クリック時の動作")]
    public bool toggleDisplay = true; // クリックで表示を切り替えるか
    public bool clearOtherDisplays = true; // 他のボタンの表示をクリアするか
    
    
    private RecipeData currentRecipe;
    private enum RecipeSource { None, Craft, Blacksmith, Wash }
    private RecipeSource currentSource = RecipeSource.None;
    private const string ContextTextTag = "craftway";
    private TextMeshProUGUI cachedContextText;
    private bool isDisplayed = false; // 現在表示されているかどうか
    private static RecipeItemHover currentlyActiveHover = null; // 現在アクティブなHover
    
    void Start()
    {
        InitializeComponent();
    }
    
    void OnDestroy()
    {
        // このオブジェクトが破棄される際に、現在のアクティブなHoverをクリア
        if (currentlyActiveHover == this)
        {
            currentlyActiveHover = null;
        }
    }
    
    /// <summary>
    /// コンポーネントの初期化（手動アタッチ時にも使用可能）
    /// </summary>
    public string resultContextImageTag = ""; // シーン内タグで検索する場合のタグ名（Inspectorから指定可）

    public void InitializeComponent()
    {
        // コンポーネントの自動取得
        if (itemNameText == null)
            itemNameText = GetComponent<TextMeshProUGUI>();
            
        if (itemNameText != null)
        {
            currentItemName = itemNameText.text;
        }
        
        // 自動でImageを検索する場合
        if (autoFindImages && (requiredItemImages == null || requiredItemImages.Length == 0))
        {
            FindRequiredItemImages();
        }

        // resultContextImageUI が Inspector で未設定の場合、タグ検索でシーン内から探す
        if (resultContextImageUI == null && !string.IsNullOrEmpty(resultContextImageTag))
        {
            // まずこのオブジェクトの親/子を探索
            Image found = FindImageByTag(resultContextImageTag);
            if (found == null)
            {
                // シーン全体から探す（非アクティブも含む）
                found = FindImageInSceneByTag(resultContextImageTag);
            }
            if (found != null)
            {
                resultContextImageUI = found;
                Debug.Log($"RecipeItemHover: resultContextImageUI をタグ '{resultContextImageTag}' で自動割当しました ({found.gameObject.name})");
            }
        }
        
    }
    
    /// <summary>
    /// Required Item表示用のImageを自動検索
    /// </summary>
    private void FindRequiredItemImages()
    {
        // 指定された名前の親オブジェクトを検索
        Transform imageParent = transform.Find(imageParentName);
        if (imageParent == null)
        {
            // 親オブジェクトが見つからない場合、子オブジェクトからImageを検索
            Image[] foundImages = GetComponentsInChildren<Image>();
            if (foundImages.Length > 0)
            {
                requiredItemImages = foundImages;
                Debug.Log($"RecipeItemHover: {foundImages.Length}個のImageを自動検索しました");
            }
        }
        else
        {
            // 指定された親オブジェクトの子からImageを取得
            Image[] foundImages = imageParent.GetComponentsInChildren<Image>();
            if (foundImages.Length > 0)
            {
                requiredItemImages = foundImages;
                Debug.Log($"RecipeItemHover: {imageParentName}から{foundImages.Length}個のImageを取得しました");
            }
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // 基本的な効果音再生
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
        }
        if (string.IsNullOrEmpty(currentItemName)) return;
        
        // 他のボタンの表示をクリアする場合
        if (clearOtherDisplays && currentlyActiveHover != null && currentlyActiveHover != this)
        {
            ClearAllDisplays();
        }
        
        // レシピデータを取得
        currentRecipe = FindRecipeByResultItem(currentItemName);
        
        if (currentRecipe != null)
        {
            if (toggleDisplay)
            {
                // トグル表示：現在表示されている場合は非表示、非表示の場合は表示
                if (isDisplayed)
                {
                    HideRequiredItems();
                    isDisplayed = false;
                    currentlyActiveHover = null;
                }
                else
                {
                    DisplayRequiredItems(currentRecipe);
                    isDisplayed = true;
                    currentlyActiveHover = this;
                }
            }
            else
            {
                // 常に表示
                DisplayRequiredItems(currentRecipe);
                isDisplayed = true;
                currentlyActiveHover = this;
            }
        }
    }
    
    /// <summary>
    /// 結果アイテム名からレシピを検索（複数のデータベースから検索）
    /// </summary>
    private RecipeData FindRecipeByResultItem(string resultItemName)
    {
        currentSource = RecipeSource.None;
        // recipeDatabaseから検索
        if (recipeDatabase != null)
        {
            foreach (var recipe in recipeDatabase.allRecipes)
            {
                if (recipe != null && recipe.resultItem != null && 
                    recipe.resultItem.itemName == resultItemName)
                {
                    currentSource = RecipeSource.Craft;
                    return recipe;
                }
            }
        }
        
        // weaponRecipeDatabaseから検索
        if (weaponRecipeDatabase != null)
        {
            foreach (var recipe in weaponRecipeDatabase.allRecipes)
            {
                if (recipe != null && recipe.resultItem != null && 
                    recipe.resultItem.itemName == resultItemName)
                {
                    currentSource = RecipeSource.Blacksmith;
                    return recipe;
                }
            }
        }
        
        // washRecipeDatabaseから検索
        if (washRecipeDatabase != null)
        {
            foreach (var recipe in washRecipeDatabase.allRecipes)
            {
                if (recipe != null && recipe.resultItem != null && 
                    recipe.resultItem.itemName == resultItemName)
                {
                    currentSource = RecipeSource.Wash;
                    return recipe;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Required Itemのスプライトを表示
    /// </summary>
    private void DisplayRequiredItems(RecipeData recipe)
    {
        if (useTagBasedDisplay)
        {
            DisplayItemsByTag(recipe);
        }
        else
        {
            DisplayItemsByArray(recipe);
        }
        UpdateContextText(currentRecipe);
    }
    
    /// <summary>
    /// タグベースでアイテムを表示
    /// </summary>
    private void DisplayItemsByTag(RecipeData recipe)
    {
        // ResultItemのアイコンを表示
        if (recipe.resultItem != null)
        {
            Image resultImage = FindImageByTag(resultItemTag);
            if (resultImage != null)
            {
                resultImage.sprite = recipe.resultItem.icon;
                resultImage.preserveAspect = true;
                resultImage.color = Color.white; // 不透明にする
            }
            
            // ResultItemの名前を表示
            TextMeshProUGUI resultNameText = FindTextMeshProByTag(resultItemNameTag);
            if (resultNameText != null)
            {
                resultNameText.text = recipe.resultItem.itemName;
            }
        }
        
        // RequiredItemのアイコンを表示
        if (recipe.requiredItems != null)
        {
            // 1つ目のRequiredItem
            if (recipe.requiredItems.Length > 0 && recipe.requiredItems[0] != null)
            {
                Image req1Image = FindImageByTag(requiredItem1Tag);
                if (req1Image != null)
                {
                    req1Image.sprite = recipe.requiredItems[0].icon;
                    req1Image.preserveAspect = true;
                    req1Image.color = Color.white; // 不透明にする
                }
                
                // 1つ目のRequiredItemの名前を表示
                TextMeshProUGUI req1NameText = FindTextMeshProByTag(requiredItem1NameTag);
                if (req1NameText != null)
                {
                    req1NameText.text = recipe.requiredItems[0].itemName;
                }
            }
            
            // 2つ目のRequiredItem
            if (recipe.requiredItems.Length > 1 && recipe.requiredItems[1] != null)
            {
                Image req2Image = FindImageByTag(requiredItem2Tag);
                if (req2Image != null)
                {
                    req2Image.sprite = recipe.requiredItems[1].icon;
                    req2Image.preserveAspect = true;
                    req2Image.color = Color.white; // 不透明にする
                }
                
                // 2つ目のRequiredItemの名前を表示
                TextMeshProUGUI req2NameText = FindTextMeshProByTag(requiredItem2NameTag);
                if (req2NameText != null)
                {
                    req2NameText.text = recipe.requiredItems[1].itemName;
                }
            }
        }
    }
    
    /// <summary>
    /// 配列ベースでアイテムを表示（従来の方法）
    /// </summary>
    private void DisplayItemsByArray(RecipeData recipe)
    {
        if (requiredItemImages == null || recipe.requiredItems == null) return;
        
        // 全てのImageを一旦非表示
        for (int i = 0; i < requiredItemImages.Length; i++)
        {
            if (requiredItemImages[i] != null)
            {
                requiredItemImages[i].gameObject.SetActive(false);
            }
        }
        
        // Required Itemの数だけImageを表示
        int displayCount = Mathf.Min(requiredItemImages.Length, recipe.requiredItems.Length);
        
        for (int i = 0; i < displayCount; i++)
        {
            if (requiredItemImages[i] != null && recipe.requiredItems[i] != null)
            {
                requiredItemImages[i].sprite = recipe.requiredItems[i].icon;
                requiredItemImages[i].preserveAspect = true;
                requiredItemImages[i].gameObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 指定されたタグのImageを検索
    /// </summary>
    private Image FindImageByTag(string tag)
    {
        // 現在のオブジェクトから検索（非アクティブも含む）
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img.CompareTag(tag))
            {
                return img;
            }
        }
        
        // 親オブジェクトからも検索
        Transform parent = transform.parent;
        while (parent != null)
        {
            Image[] parentImages = parent.GetComponentsInChildren<Image>(true);
            foreach (Image img in parentImages)
            {
                if (img.CompareTag(tag))
                {
                    return img;
                }
            }
            parent = parent.parent;
        }
        
        return null;
    }
    
    /// <summary>
    /// 指定されたタグのTextMeshProを検索
    /// </summary>
    private TextMeshProUGUI FindTextMeshProByTag(string tag)
    {
        // 現在のオブジェクトから検索（非アクティブも含む）
        TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.CompareTag(tag))
            {
                return text;
            }
        }
        
        // 親オブジェクトからも検索
        Transform parent = transform.parent;
        while (parent != null)
        {
            TextMeshProUGUI[] parentTexts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in parentTexts)
            {
                if (text.CompareTag(tag))
                {
                    return text;
                }
            }
            parent = parent.parent;
        }
        
        return null;
    }
    
    /// <summary>
    /// Required Itemのスプライトを非表示
    /// </summary>
    private void HideRequiredItems()
    {
        if (useTagBasedDisplay)
        {
            HideItemsByTag();
        }
        else
        {
            HideItemsByArray();
        }
    }
    
    /// <summary>
    /// タグベースでアイテムを非表示（空白の画像に戻す）
    /// </summary>
    private void HideItemsByTag()
    {
        // ResultItemを空白にする
        Image resultImage = FindImageByTag(resultItemTag);
        if (resultImage != null)
        {
            resultImage.sprite = null;
            resultImage.color = new Color(1f, 1f, 1f, 0f); // 透明にする
        }
        
        // ResultItemの名前をクリア
        TextMeshProUGUI resultNameText = FindTextMeshProByTag(resultItemNameTag);
        if (resultNameText != null)
        {
            resultNameText.text = "";
        }
        
        // RequiredItem1を空白にする
        Image req1Image = FindImageByTag(requiredItem1Tag);
        if (req1Image != null)
        {
            req1Image.sprite = null;
            req1Image.color = new Color(1f, 1f, 1f, 0f); // 透明にする
        }
        
        // RequiredItem1の名前をクリア
        TextMeshProUGUI req1NameText = FindTextMeshProByTag(requiredItem1NameTag);
        if (req1NameText != null)
        {
            req1NameText.text = "";
        }
        
        // RequiredItem2を空白にする
        Image req2Image = FindImageByTag(requiredItem2Tag);
        if (req2Image != null)
        {
            req2Image.sprite = null;
            req2Image.color = new Color(1f, 1f, 1f, 0f); // 透明にする
        }
        
        // RequiredItem2の名前をクリア
        TextMeshProUGUI req2NameText = FindTextMeshProByTag(requiredItem2NameTag);
        if (req2NameText != null)
        {
            req2NameText.text = "";
        }
        UpdateContextText(null, true);
    }
    
    /// <summary>
    /// 配列ベースでアイテムを非表示（従来の方法）
    /// </summary>
    private void HideItemsByArray()
    {
        if (requiredItemImages == null) return;
        
        for (int i = 0; i < requiredItemImages.Length; i++)
        {
            if (requiredItemImages[i] != null)
            {
                requiredItemImages[i].gameObject.SetActive(false);
            }
        }
        UpdateContextText(null, true);
    }

    [Header("コンテキストアイコン")]
    [Tooltip("コンテキスト表示用のImage（インスペクタで指定）")]
    public Image resultContextImageUI;

    [Tooltip("ソース別のフォールバックスプライト（Result のアイコンが無い場合に使用）")]
    public Sprite craftContextSprite;
    public Sprite blacksmithContextSprite;
    public Sprite washContextSprite;
    public Sprite defaultContextSprite;

    [Tooltip("true の場合、ソース別フォールバックスプライトを優先して表示します（デフォルト: true）")]
    public bool preferSourceSprite = true;

    /// <summary>
    /// コンテキストテキストと（必要に応じて）コンテキストアイコンを更新する
    /// </summary>
    /// <param name="clear">true の場合は非表示にする</param>
    /// <param name="recipe">現在表示しているレシピ（アイコン表示に使われる）</param>
    private void UpdateContextText(RecipeData recipe = null, bool clear = false)
    {
        var recipeContextText = GetOrFindContextText();
        if (recipeContextText == null && resultContextImageUI == null) return;

        if (clear)
        {
            if (recipeContextText != null)
            {
                recipeContextText.gameObject.SetActive(false);
                recipeContextText.text = string.Empty;
            }
            if (resultContextImageUI != null)
            {
                resultContextImageUI.sprite = null;
                resultContextImageUI.color = new Color(1f, 1f, 1f, 0f);
                resultContextImageUI.gameObject.SetActive(false);
            }
            return;
        }

        string ctxText = string.Empty;
        switch (currentSource)
        {
            case RecipeSource.Craft:
                ctxText = "大窯で調合(属性重ね掛け可)";
                break;
            case RecipeSource.Wash:
                ctxText = "洗い場で浄化";
                break;
            case RecipeSource.Blacksmith:
                ctxText = "金床で鍛冶";
                break;
            default:
                ctxText = string.Empty;
                break;
        }

        if (recipeContextText != null)
        {
            recipeContextText.text = ctxText;
            recipeContextText.gameObject.SetActive(!string.IsNullOrEmpty(ctxText));
        }

        // アイコンの表示ロジック
        if (resultContextImageUI != null)
        {
            Sprite chosen = null;

            if (preferSourceSprite)
            {
                // ソース別スプライトを優先して選択
                switch (currentSource)
                {
                    case RecipeSource.Craft: chosen = craftContextSprite; break;
                    case RecipeSource.Blacksmith: chosen = blacksmithContextSprite; break;
                    case RecipeSource.Wash: chosen = washContextSprite; break;
                    default: chosen = defaultContextSprite; break;
                }

                // ソース別が無ければ result アイコンをフォールバックとして使用
                if (chosen == null && recipe != null && recipe.resultItem != null && recipe.resultItem.icon != null)
                {
                    chosen = recipe.resultItem.icon;
                }
            }
            else
            {
                // 既存挙動: result アイコンを優先
                if (recipe != null && recipe.resultItem != null && recipe.resultItem.icon != null)
                {
                    chosen = recipe.resultItem.icon;
                }
                else
                {
                    switch (currentSource)
                    {
                        case RecipeSource.Craft: chosen = craftContextSprite; break;
                        case RecipeSource.Blacksmith: chosen = blacksmithContextSprite; break;
                        case RecipeSource.Wash: chosen = washContextSprite; break;
                        default: chosen = defaultContextSprite; break;
                    }
                }
            }

            if (chosen != null)
            {
                resultContextImageUI.sprite = chosen;
                resultContextImageUI.preserveAspect = true;
                resultContextImageUI.color = Color.white;
                resultContextImageUI.gameObject.SetActive(true);
            }
            else
            {
                resultContextImageUI.sprite = null;
                resultContextImageUI.color = new Color(1f, 1f, 1f, 0f);
                resultContextImageUI.gameObject.SetActive(false);
            }
        }
    }

    private TextMeshProUGUI GetOrFindContextText()
    {
        if (cachedContextText != null) return cachedContextText;
        cachedContextText = FindTextMeshProByTag(ContextTextTag);
        return cachedContextText;
    }

    /// <summary>
    /// シーン全体から指定タグの Image を検索（非アクティブ含む）
    /// </summary>
    private Image FindImageInSceneByTag(string tag)
    {
        try
        {
            var allImages = UnityEngine.Object.FindObjectsOfType<Image>(true);
            foreach (var img in allImages)
            {
                if (img == null || img.gameObject == null) continue;
                if (img.CompareTag(tag)) return img;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"RecipeItemHover: FindImageInSceneByTag failed: {ex.Message}");
        }
        return null;
    }
    
    /// <summary>
    /// 全てのUI要素をクリア（他のボタンが押された時に使用）
    /// </summary>
    public static void ClearAllDisplays()
    {
        // 現在アクティブなHoverがあれば非表示にする
        if (currentlyActiveHover != null)
        {
            currentlyActiveHover.HideRequiredItems();
            currentlyActiveHover.isDisplayed = false;
            currentlyActiveHover = null;
        }
    }
    
    
    /// <summary>
    /// アイテム名を設定
    /// </summary>
    public void SetItemName(string itemName)
    {
        currentItemName = itemName;
        if (itemNameText != null)
        {
            itemNameText.text = itemName;
        }
    }
    
    /// <summary>
    /// RecipeDatabaseを設定
    /// </summary>
    public void SetRecipeDatabase(RecipeDatabase database)
    {
        recipeDatabase = database;
    }
    
    /// <summary>
    /// WeaponRecipeDatabaseを設定
    /// </summary>
    public void SetWeaponRecipeDatabase(RecipeDatabase database)
    {
        weaponRecipeDatabase = database;
    }
    
    /// <summary>
    /// 両方のRecipeDatabaseを設定
    /// </summary>
    public void SetRecipeDatabases(RecipeDatabase recipeDB, RecipeDatabase weaponDB)
    {
        recipeDatabase = recipeDB;
        weaponRecipeDatabase = weaponDB;
    }
    
    /// <summary>
    /// 全てのRecipeDatabaseを設定
    /// </summary>
    public void SetRecipeDatabases(RecipeDatabase recipeDB, RecipeDatabase weaponDB, RecipeDatabase washDB)
    {
        recipeDatabase = recipeDB;
        weaponRecipeDatabase = weaponDB;
        washRecipeDatabase = washDB;
    }
    
    /// <summary>
    /// Required Item表示用のImageを手動で設定
    /// </summary>
    public void SetRequiredItemImages(Image[] images)
    {
        requiredItemImages = images;
        autoFindImages = false; // 手動設定時は自動検索を無効化
    }
    
    /// <summary>
    /// 自動検索設定を変更
    /// </summary>
    public void SetAutoFindImages(bool autoFind, string parentName = "RequiredItemImages")
    {
        autoFindImages = autoFind;
        imageParentName = parentName;
        
        if (autoFindImages && (requiredItemImages == null || requiredItemImages.Length == 0))
        {
            FindRequiredItemImages();
        }
    }
    
    /// <summary>
    /// 手動アタッチ用の完全初期化メソッド
    /// </summary>
    public void ManualSetup(string itemName, RecipeDatabase database, Image[] images = null)
    {
        // アイテム名を設定
        SetItemName(itemName);
        
        // データベースを設定
        SetRecipeDatabase(database);
        
        // Imageを設定（指定された場合）
        if (images != null)
        {
            SetRequiredItemImages(images);
        }
        
        // 初期化を実行
        InitializeComponent();
    }
    
    /// <summary>
    /// タグベース表示の設定
    /// </summary>
    public void SetTagBasedDisplay(bool useTag, string resultTag = "ans", string req1Tag = "req1", string req2Tag = "req2")
    {
        useTagBasedDisplay = useTag;
        resultItemTag = resultTag;
        requiredItem1Tag = req1Tag;
        requiredItem2Tag = req2Tag;
    }
}

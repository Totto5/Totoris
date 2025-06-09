using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    const int FieldXLength = 10; // フィールドのX軸サイズ
    const int FieldYLength = 20; // フィールドのY軸サイズ
    const int NextFieldXLength = 4; // 次のテトリミノフィールドのX軸サイズ
    const int NextFieldYLength = 4; // 次のテトリミノフィールドのY軸サイズ
    
    [Header("フィールド設定")]
    [SerializeField] private GameObject field; // メインフィールドのゲームオブジェクト
    [SerializeField] private GameObject nextField; // 次のテトリミノを表示するフィールドのゲームオブジェクト
    
    [Header("ブロック設定")]
    [SerializeField] private SpriteRenderer blockPrefab; // ブロックのプレハブ
    
    private SpriteRenderer[,] _blockObjects; // ブロックの2次元配列
    private SpriteRenderer[,] _nextFieldObjects; // 次のテトリミノフィールドの2次元配列

    private Tetrimino _tetrimino = new Tetrimino(); // テトリミノのインスタンス
    private Tetrimino _nextTetrimino = new Tetrimino(); // 次のテトリミノのインスタンス
    [Header("ブロック落下速度")]
    [SerializeField] private float fallInterval = 0.3f; // ブロックが落下する間隔
    private DateTime _lastFallTime; // 最後にブロックが落下した時間
    private DateTime _lastControlTime; // 最後にコントロールが行われた時間
    
    private BlockType[,] _fieldBlocks; // ブロックの種類を格納する2次元配列
    
    [Header("UI設定")]
    [SerializeField] private GameObject _gameOverText; // ゲームオーバーのテキストオブジェクト
    [SerializeField] private GameObject _gameTitlePanel; // タイトル画面
    
    [Header("オーディオ設定")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _rotateSe;
    [SerializeField] private AudioClip _moveSe;
    [SerializeField] private AudioClip _deleteLineSe;
    [SerializeField] private AudioClip _fixSe;

    /**
     * ブロックの種類を定義する列挙型
     * 4つのブロックを繋げたテトリミノを表現します。
     */
    public enum BlockType
    {
        None = 0,
        TetrominoI = 1,
        TetrominoO = 2,
        TetrominoS = 3,
        TetrominoZ = 4,
        TetrominoJ = 5,
        TetrominoL = 6,
        TetrominoT = 7
    }

    private GameState State { get; set; } = GameState.None; // ゲームの状態を管理するプロパティ
    
    public enum GameState
    {
        None, // ゲームの状態が未設定
        Playing, // ゲームプレイ中
        Result, // ゲームの結果表示状態
        Title // タイトル画面
    }
   
    private void Start()
    { 
        InitializeBlockObjects(); // ブロックの生成
        Initialize(); // ゲームの初期化
        Draw(); // フィールドの描画
    }

    private void Update()
    {
        switch (State)
        {
            case GameState.None:
                Initialize(); // ゲームの初期化
                break;
            case GameState.Playing:
                UpdatePlay(); // ゲームプレイの更新
                break;
            case GameState.Result:
                UpdateResult(); // ゲーム結果の更新
                break;
            case GameState.Title:
                UpdateTitle(); // タイトル画面の更新
                break;
            
        }
        
        if (Input.GetKeyDown(KeyCode.Escape)) // エスケープキーでゲームを終了
        {
            Application.Quit();
        }
    }

    private void UpdateResult()
    {
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop(); // BGMを停止
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            
            State = GameState.Title;
            _gameOverText.SetActive(false); // ゲームオーバーテキストを非表示にする
            _gameTitlePanel.SetActive(true); // タイトル画面のテキストを表示する
            
        }
        
    }
    
    private void UpdateTitle()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Initialize();
            State = GameState.Playing;
            _gameTitlePanel.SetActive(false); // タイトル画面のテキストを非表示にする
        }
    }
    
    private void UpdatePlay()
    {
        
        if (!_audioSource.isPlaying)
        {
            _audioSource.Play(); // BGMを再生
        }
        
        var isControl = ControlTetrimino(); // テトリミノのコントロールを行う
        
        
        var currentTime = DateTime.UtcNow; // 現在の時間を取得
        // 一定時間ごとにブロックを落下させる
        if ((currentTime - _lastFallTime).TotalSeconds < fallInterval)
        {
            if (!isControl)
            {
                return;
            }
        }
        else
        {
            _lastFallTime = currentTime; // 最後にブロックが落下した時間を更新
            
            // テトリミノを下に移動させる
            if (!TryMoveTetrimino(0, 1))
            {
                //TODO: テトリミノを固定
                var blockPositions = _tetrimino.GetBlockPositions(); // テトリミノのブロック位置を取得
                // テトリミノのブロック位置をフィールドに反映
                foreach (var blockPosition in blockPositions)
                {
                    _fieldBlocks[blockPosition.y, blockPosition.x] = _tetrimino.BlockType; // フィールドのブロックを更新
                }
                if(DeleteLineBlocks()) // 揃った列を消去
                {
                    _audioSource.PlayOneShot(_deleteLineSe); // 列を消去した際の効果音を再生
                }else
                {
                    _audioSource.PlayOneShot(_fixSe); // テトリミノを固定した際の効果音を再生
                }
                
                _tetrimino.Initialize(_nextTetrimino.BlockType); // 次のテトリミノを現在のテトリミノとして初期化
                _nextTetrimino.Initialize(); // 次のテトリミノを新たに初期化

                // 動けなくなった場合はゲームオーバー
                if (!CanMoveTetrimino(0, 0))
                {
                    _gameOverText.SetActive(true); // ゲームオーバーテキストを表示
                    State = GameState.Result;
                }
            }
        }
        
        
        Draw(); // フィールドを再描画
        
    }


    /**
     * 揃った列を消去するメソッド
     * フィールド内で揃った列を検出し、消去します。
     */
    private bool DeleteLineBlocks()
    {
        var isDeleted = false; // 列が消去されたかどうかのフラグ
        //　揃った列を消去
        for (var y = FieldYLength - 1; y >= 0;)
        {
            var hasBlank = false;
            for (var x = 0; x < FieldXLength; x++)
            {
                if (_fieldBlocks[y, x] == BlockType.None)
                {
                    hasBlank = true; // 空白がある場合はフラグを立てる
                    break;
                }
            }
            if (hasBlank)
            {
                y--; // 空白がある場合は次の行へ
                continue;
            }
            isDeleted = true; // 列が消去された場合はフラグを立てる
            // 揃った行を消去して、上の行を下に移動
            for(var downY = y; downY > 0; downY--)
            {
                for (var x = 0; x < FieldXLength; x++)
                {
                    // 上の行のブロックを下の行に移動
                    _fieldBlocks[downY, x] = downY == 0 ? BlockType.None : _fieldBlocks[downY - 1, x];
                }
            }
            
        }
        return isDeleted;
        
    }


    /**
     * テトリミノのコントロールを行うメソッド
     * 左矢印キー、右矢印キー、下矢印キーが押された場合にテトリミノを移動します。
     * 上矢印キーが押された場合はテトリミノを回転します。
     * @return 移動が成功したかどうかの真偽値
     */
    private bool ControlTetrimino()
    {
        var currentTime = DateTime.UtcNow;  
        if((currentTime - _lastControlTime).TotalSeconds < 0.1f) // コントロールの間隔を0.1秒に設定
            return false; // コントロールが行われていない場合はfalseを返す

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // 左矢印キーが押された場合、テトリミノを左に移動
            if (TryMoveTetrimino(-1, 0))
            {
                _audioSource.PlayOneShot(_moveSe); // 移動の効果音を再生
                _lastControlTime = currentTime; // コントロールの時間を更新
                return true; // 移動が成功した場合はtrueを返す
            }
            
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            // 右矢印キーが押された場合、テトリミノを右に移動
            if (TryMoveTetrimino(1, 0))
            {
                _audioSource.PlayOneShot(_moveSe); // 移動の効果音を再生
                _lastControlTime = currentTime; // コントロールの時間を更新
                return true; // 移動が成功した場合はtrueを返す
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            // 下矢印キーが押された場合、テトリミノを下に移動
            if (TryMoveTetrimino(0, 1))
            {
                _audioSource.PlayOneShot(_moveSe); // 移動の効果音を再生
                _lastControlTime = currentTime; // コントロールの時間を更新
                return true; // 移動が成功した場合はtrueを返す
            }
        }
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Space))
        {
            // 上矢印キーまたはスペースキーが押された場合、テトリミノを回転
            if (TryRollTetrimino())
            {
                _audioSource.PlayOneShot(_rotateSe); // 回転の効果音を再生
                _lastControlTime = currentTime; // コントロールの時間を更新
                return true; // 回転が成功した場合はtrueを返す
            }
        }
        // 他のキーが押されていない場合は何もしない
        return false;
    }


    /**
     * テトリミノを移動するメソッド
     * 指定された座標にテトリミノを移動できるかどうかを判定し、移動できる場合はテトリミノを移動します。
     * @param x X座標の移動量
     * @param y Y座標の移動量
     * @return 移動が成功したかどうかの真偽値
     */
    private bool TryMoveTetrimino(int x, int y)
    {
        // テトリミノを移動できるかどうかを判定
        if (CanMoveTetrimino(x, y))
        {
            // テトリミノを移動
            _tetrimino.Move(x, y);
            return true;
        }
        // 移動できない場合はfalseを返す
        return false;
    }
    
    /**
     * テトリミノを下に移動できるかどうかを判定するメソッド
     * テトリミノのブロック位置を取得し、移動先がフィールドの範囲内であり、かつ移動先に既にブロックが存在しない場合はtrueを返します。
     * @param x X座標の移動量
     * @param y Y座標の移動量
     * @return 移動可能かどうかの真偽値
     */
    private bool CanMoveTetrimino(int x, int y)
    {
        var blockPositions = _tetrimino.GetBlockPositions(); // テトリミノのブロック位置を取得
        foreach (var blockPosition in blockPositions)
        {
            var movedX = blockPosition.x + x; // X座標を移動
            var movedY = blockPosition.y + y; // Y座標を移動
            
            // フィールドの範囲外に出ている場合は移動できない
            if (movedX < 0 || movedX >= FieldXLength)
                return false;
            if (movedY < 0 || movedY >= FieldYLength)
                return false;
            
            // 移動先に既にブロックが存在する場合は移動できない
            if (_fieldBlocks[movedY, movedX] != BlockType.None)
                return false;
        }
        return true;
    }


    private bool TryRollTetrimino()
    {
        // テトリミノを回転できるかどうかを判定
        if (CanRollTetrimino())
        {
            // テトリミノを回転
            _tetrimino.Roll();
            return true; // 回転が成功した場合はtrueを返す
        }
        // 回転できない場合はfalseを返す
        return false;
    }

    private bool CanRollTetrimino()
    {
        var blockPositions = _tetrimino.GetRolledBlockPositions(); // 回転後のブロック位置を取得
        foreach (var blockPosition in blockPositions)
        {
            var movedX = blockPosition.x; // X座標を取得
            var movedY = blockPosition.y; // Y座標を取得
            
            // フィールドの範囲外に出ている場合は回転できない
            if (movedX < 0 || movedX >= FieldXLength)
                return false;
            if (movedY < 0 || movedY >= FieldYLength)
                return false;
            
            // 回転後の位置に既にブロックが存在する場合は回転できない
            if (_fieldBlocks[movedY, movedX] != BlockType.None)
                return false;
        }
        return true; // 全ての条件を満たす場合はtrueを返す
    }
    
    
    

    /**
     * ブロックを生成するメソッド
     * フィールドのサイズに基づいて、ブロックを生成し、配列に格納します。
     */
    private void InitializeBlockObjects()
    {
        _blockObjects = new SpriteRenderer[FieldYLength, FieldXLength]; // ブロックの2次元配列を初期化
        // フィールドのサイズに基づいてブロックを生成
        for (var y = 0; y < FieldYLength; y++)
        {
            for (var x = 0; x < FieldXLength; x++) 
            { 
                var block = Instantiate(blockPrefab, field.transform); // ブロックを生成
                block.transform.localPosition = new Vector3(x, y, 0); // ブロックの位置を設定
                block.transform.localRotation = Quaternion.identity; // ブロックの回転を初期化
                block.transform.localScale = new Vector3(0.9f, 0.9f, 1); // ブロックのスケールを設定

                block.color = Color.black; // ブロックの色を黒に設定(Fieldの背景色に合わせる)
                _blockObjects[y, x] = block; // 生成したブロックを配列に格納

            }
        }
        
        _nextFieldObjects = new SpriteRenderer[NextFieldYLength, NextFieldXLength]; // 次のテトリミノフィールドの2次元配列を初期化
        // 次のテトリミノフィールドのサイズに基づいてブロックを生成
        for (var y = 0; y < NextFieldYLength; y++)
        {
            for (var x = 0; x < NextFieldXLength; x++) 
            { 
                var block = Instantiate(blockPrefab, nextField.transform); // 次のテトリミノフィールドのブロックを生成
                block.transform.localPosition = new Vector3(x, y, 0); // ブロックの位置を設定
                block.transform.localRotation = Quaternion.identity; // ブロックの回転を初期化
                block.transform.localScale = new Vector3(0.9f, 0.9f, 1); // ブロックのスケールを設定(見た目のために0.9fに設定)

                block.color = Color.black; // ブロックの色を黒に設定(次のフィールドの背景色に合わせる)
                _nextFieldObjects[y, x] = block; // 生成したブロックを配列に格納
            }
        }
        
        _fieldBlocks = new BlockType[FieldYLength, FieldXLength]; // ブロックの種類を格納する配列を初期化
    }

    /**
     * ゲームの初期化メソッド
     * テトリミノを初期化し、フィールドのブロックを初期化します。
     * 
     */
    private void Initialize()
    {
        _gameOverText.SetActive(false); // ゲームオーバーテキストを非表示にする
        _tetrimino.Initialize(); // テトリミノを初期化
        _nextTetrimino.Initialize(); // 次のテトリミノを初期化
        _lastFallTime = DateTime.UtcNow; // 最後にブロックが落下した時間を現在の時間に設定
        _lastControlTime = DateTime.UtcNow; // 最後にコントロールが行われた時間を現在の時間に設定
        State = GameState.Playing;
        
        // フィールドのブロックを初期化
        for (var y = 0; y < FieldYLength; y++)
        {
            for (var x = 0; x < FieldXLength; x++)
            {
                _fieldBlocks[y, x] = BlockType.None; // 全てのブロックをNoneに初期化
            }
        }
    }


    /**
     * ゲームの更新メソッド
     * 一定時間ごとにブロックを落下させ、フィールドを描画します。
     */
    private void Draw()
    {
        // フィールドのブロックを描画
        for (var y = 0; y < FieldYLength; y++)
        {
            for (var x = 0; x < FieldXLength; x++)
            {
                var blockOBj = _blockObjects[y, x]; // ブロックのオブジェクトを取得
                var blockType = _fieldBlocks[y, x]; // ブロックの種類を取得
                blockOBj.color = GetBlockColor(blockType); // ブロックの色を設定
                
            }
        }
        
        // テトリミノを描画
        var positions = _tetrimino.GetBlockPositions(); // テトリミノの位置を取得
        foreach (var position in positions)
        {
            // テトリミノの位置に対応するブロックのオブジェクトを取得
            var tetriminoBlock = _blockObjects[position.y, position.x];
            tetriminoBlock.color = GetBlockColor(_tetrimino.BlockType); // テトリミノの色を設定
        }
        
        // 次のフィールドを描画
        for (var y = 0; y < NextFieldYLength; y++)
        {
            for (var x = 0; x < NextFieldXLength; x++)
            {
                 _nextFieldObjects[y, x].color = GetBlockColor(BlockType.None);
            }
        }
        // 次のテトリミノを描画
        var nextPositions = _nextTetrimino.GetBlockPositions(); // 次のテトリミノの位置を取得
        foreach (var position in nextPositions)
        {
            // 次のテトリミノの位置に対応するブロックのオブジェクトを取得
            var nextBlock = _nextFieldObjects[position.y + 1, position.x - 3];
            nextBlock.color = GetBlockColor(_nextTetrimino.BlockType); // 次のテトリミノの色を設定
        }
    }
    
    /**
     * ブロックの色を取得するメソッド
     * ブロックの種類に応じて、対応する色を返します。
     * @param blockType ブロックの種類
     * @return ブロックの色
     */
    
    private Color GetBlockColor(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.TetrominoI:
                return Color.cyan;
            case BlockType.TetrominoO:
                return Color.yellow;
            case BlockType.TetrominoS:
                return Color.green;
            case BlockType.TetrominoZ:
                return Color.red;
            case BlockType.TetrominoJ:
                return Color.blue;
            case BlockType.TetrominoL:
                return new Color(1f, 0.5f, 0f); // オレンジ色
            case BlockType.TetrominoT:
                return new Color(0.5f, 0f, 0.5f); // 紫色
            default:
                return Color.black; // デフォルトは黒
        }
    }
}

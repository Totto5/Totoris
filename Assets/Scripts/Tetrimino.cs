using System.Collections.Generic;
using UnityEngine;

public class Tetrimino
{
    const int PatternXLength = 4; // ブロックのパターンのX軸の長さ
    const int PatternYLength = 4; // ブロックのパターンのY軸の長さ
    
    
    private Vector2Int _basePosition; // 基準位置
    private int _rollPattern; // 回転パターン
    // ブロックの種類を定義する列挙型
    public GameManager.BlockType BlockType { get; private set; } // ブロックの種類
    // 回転パターンの数を取得するプロパティ
    private int RollPatternNum {
        get
        {
            return BlockType == GameManager.BlockType.TetrominoO ? 1 : 4; // Oブロックは回転しないため、パターン数は1、それ以外は4
        }
    }
    // 次の回転パターンを取得するプロパティ
    private int NextRollPattern {get {return _rollPattern + 1< RollPatternNum ? _rollPattern + 1 : 0; } }


        /**
         * 初期化メソッド
         * まずは縦４マスのブロックを生成します。
         * @param blockType ブロックの種類
         */
    public void Initialize(GameManager.BlockType blockType = GameManager.BlockType.None)
    {
        if (blockType == GameManager.BlockType.None)
        {
            //TODO: ランダムにブロックタイプを選択する(1〜8の範囲)
            blockType = (GameManager.BlockType)Random.Range(1, 8);
        }
        _basePosition = new Vector2Int(3,0); // 基準位置を初期化
        _rollPattern = 0; // 回転パターンを初期化
        BlockType = blockType; // ブロックの種類を設定
    }


    public Vector2Int[] GetBlockPositions()
    {
        return GetBlockPositions(_rollPattern);
    }
    
    
    /**
     * ブロックの基準位置を設定するメソッド
     */
    public Vector2Int[] GetBlockPositions(int rollPattern)
    {
        var positions = new Vector2Int[4];
        var pattern = _typePatterns[BlockType];
        var positionIndex = 0;
        // ブロックのパターンを基に、ブロックの位置を計算
        for (var y = 0; y < PatternYLength; y++)
        {
            for (var x = 0; x < PatternXLength; x++)
            {
                if (pattern[rollPattern, y, x] == 1) // パターンが1の位置をブロックの位置として設定
                {
                    // 基準位置からのオフセットを計算
                    positions[positionIndex] = new Vector2Int(_basePosition.x + x, _basePosition.y + y);
                    positionIndex++;
                }
            }
        }
        return positions;
    }

    /**
     * ブロックを移動するメソッド
     * @param x X座標
     * @param y Y座標
     */
    public void Move(int x, int y)
    {
        _basePosition.Set(_basePosition.x + x, _basePosition.y + y);
    }
    
    /**
     * ブロックの位置を取得するメソッド
     * 現在の回転パターンに基づいてブロックの位置を取得します。
     */
    public Vector2Int[] GetRolledBlockPositions()
    {
        return GetBlockPositions(NextRollPattern); // 次の回転パターンのブロック位置を取得
    }
    
    /**
     * ブロックを回転させるメソッド
     * 次の回転パターンに更新します。
     */
    public void Roll()
    {
        _rollPattern = NextRollPattern; // 回転パターンを更新
    }

    /**
     * ブロックのパターンを定義する辞書
     */
    static readonly Dictionary<GameManager.BlockType, int[,,]> _typePatterns =
        new Dictionary<GameManager.BlockType, int[,,]>()
        {
            {
                GameManager.BlockType.TetrominoI,
                new int[,,]
                {
                    {
                        { 0, 0, 0, 0 },
                        { 1, 1, 1, 1 },
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 0, 1, 0 },
                        { 0, 0, 1, 0 },
                        { 0, 0, 1, 0 },
                        { 0, 0, 1, 0 }
                    },
                    {
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 },
                        { 1, 1, 1, 1 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 1, 0, 0 }
                    }
                    
                }
                
            },
            {
                GameManager.BlockType.TetrominoO,
                new int[,,]
                {
                    {
                        { 0, 1, 1, 0 },
                        { 0, 1, 1, 0 },
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    }
                }
                
            },
            {
                GameManager.BlockType.TetrominoS,
                new int[,,]
                {
                    {
                        { 0, 1, 1, 0 },
                        { 1, 1, 0, 0 },
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 0, 0 },
                        { 0, 1, 1, 0 },
                        { 0, 0, 1, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 0, 0, 0 },
                        { 0, 1, 1, 0 },
                        { 1, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 1, 0, 0, 0 },
                        { 1, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    }
                }
                
            },
            {
                GameManager.BlockType.TetrominoZ,
                new int[,,]
                {
                    {
                        { 1, 1, 0, 0 },
                        { 0, 1, 1, 0 },
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 0, 1, 0 },
                        { 0, 1, 1, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 0, 0, 0 },
                        { 1, 1, 0, 0 },
                        { 0, 1, 1, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 0, 0 },
                        { 1, 1, 0, 0 },
                        { 1, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    }
                    
                }
                
            },
            {
                GameManager.BlockType.TetrominoJ,
                new int[,,]
                {
                    {
                        { 1, 0, 0, 0 },
                        { 1, 1, 1, 0 },
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 1, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 0, 0, 0 },
                        { 1, 1, 1, 0 },
                        { 0, 0, 1, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 1, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    }
                }
                
            },
            {
                GameManager.BlockType.TetrominoL,
                new int[,,]
                {
                    {
                        { 0, 0, 1, 0 },
                        { 1, 1, 1, 0 },
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 1, 1, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 0, 0, 0 },
                        { 1, 1, 1, 0 },
                        { 1, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 1, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    }
                }
                
            },
            {
                GameManager.BlockType.TetrominoT,
                new int[,,]
                {
                    {
                        { 0, 1, 0, 0 },
                        { 1, 1, 1, 0 },
                        { 0, 0, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 0, 0 },
                        { 0, 1, 1, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 0, 0, 0 },
                        { 1, 1, 1, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    },
                    {
                        { 0, 1, 0, 0 },
                        { 1, 1, 0, 0 },
                        { 0, 1, 0, 0 },
                        { 0, 0, 0, 0 }
                    }
                }
                
            }
        };
}

using System;

public class BaseState
{
  public BlockState[] blockStates;

  public float GetMass()
  {
    var mass = 0.0f;

    foreach (var bState in blockStates)
    {
      mass += GetMass(bState);
    }

    return mass;
  }

  private float GetMass(BlockState blockState)
  {
    // TODO: These should be based on the block prototypes
    var mass = 0.0f;

    switch (blockState.blockType)
    {
      case BlockType.Square:
        mass = 20;
        break;
      case BlockType.Hexagon:
        mass = 60;
        break;
      case BlockType.Trapezoid:
        mass = 30;
        break;
      case BlockType.Rhombus:
        mass = 20;
        break;
      case BlockType.Triangle:
        mass = 10;
        break;
      case BlockType.ThinRhombus:
        mass = 10;
        break;
      case BlockType.Unknown:
      default:
        mass = 0;
        break;
    }

    return mass;
  }
}

public class BlockState
{
  public BlockType blockType;
  public float x;
  public float y;
  public float z;
  public float qx;
  public float qy;
  public float qz;
  public float qw;

  // TODO: Add attachment states


  public static BlockType GetBlockType(string blockName)
  {
    var name = blockName.Substring(3);

    foreach (var bType in Enum.GetValues(typeof(BlockType)))
    {
      var bTypeName = Enum.GetName(typeof(BlockType), bType);

      if (name.StartsWith(bTypeName))
      {
        return (BlockType)bType;
      }
    }

    return BlockType.Unknown;
  }
}

public enum BlockType
{
  Unknown,
  Square,
  Hexagon,
  Trapezoid,
  Rhombus,
  Triangle,
  ThinRhombus
}


public class BaseStateGroup
{
  public string[] baseNames;
  public BaseState[] baseStates;

  public BaseStateGroup()
  {
    baseNames = new string[0];
    baseStates = new BaseState[0];
  }

  //public BaseStateGroup( string[] baseNames, BaseState[] baseStates )
  //{
  //  this.baseNames = baseNames;
  //  this.baseStates = baseStates;
  //}
}
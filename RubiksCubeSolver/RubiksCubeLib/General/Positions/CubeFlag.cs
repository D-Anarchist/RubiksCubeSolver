﻿using System;
using System.Collections.Generic;

namespace RubiksCubeLib
{
  [Serializable]
  [Flags]
  public enum CubeFlag
  {
    None = 0,
    TopLayer = 1,
    MiddleLayer = 2,
    BottomLayer = 4,
    FrontSlice = 8,
    MiddleSlice = 16,
    BackSlice = 32,
    LeftSlice = 64,
    MiddleSliceSides = 128,
    RightSlice = 256
  }
}
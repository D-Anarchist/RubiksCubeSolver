﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace RubiksCubeLib.CubeModel
{
  public partial class CubeModel
  {
    private List<double> _frameTimes;
    private IEnumerable<Face3D>[] _buffer;
    private Thread _updateThread, _renderThread;
    private AutoResetEvent[] _updateHandle;
    private AutoResetEvent[] _renderHandle;
    private int _currentBufferIndex;

    /// <summary>
    /// Gets or sets the zoom
    /// </summary>
    public double Zoom { get; set; }

    /// <summary>
    /// Gets or sets the screen the renderer projects on
    /// </summary>
    public Rectangle Screen { get; set; }

    /// <summary>
    /// Gets or sets the FPS limit
    /// </summary>
    public double MaxFps { get; set; }

    /// <summary>
    /// Gets the current FPS
    /// </summary>
    public double Fps { get; private set; }

    /// <summary>
    /// Gets if the render cycle is running
    /// </summary>
    public bool IsRunning { get; private set; }




    private void InitRenderer()
    {
      SetDrawingArea(this.ClientRectangle);

      _frameTimes = new List<double>();
      this.IsRunning = false;
      this.MaxFps = 10000;

      _updateHandle = new AutoResetEvent[2];
      for (int i = 0; i < _updateHandle.Length; i++)
        _updateHandle[i] = new AutoResetEvent(false);

      _renderHandle = new AutoResetEvent[2];
      for (int i = 0; i < _renderHandle.Length; i++)
        _renderHandle[i] = new AutoResetEvent(true);

      _buffer = new IEnumerable<Face3D>[2];
      for (int i = 0; i < _buffer.Length; i++)
        _buffer[i] = new List<Face3D>();
    }

    /// <summary>
    /// Reset the rendering screen
    /// </summary>
    /// <param name="screen">Screen measures</param>
    public void SetDrawingArea(Rectangle screen)
    {
      this.Screen = screen;
      int min = Math.Min(screen.Height, screen.Width);
      this.Zoom = 3 * ((double)min / (double)400);
      if (screen.Width > screen.Height)
        screen.X = (screen.Width - screen.Height) / 2;
      else if (screen.Height > screen.Width)
        screen.Y = (screen.Height - screen.Width) / 2;
    }

    /// <summary>
    /// Starts the render cycle
    /// </summary>
    public void StartRender()
    {
      if (!this.IsRunning)
      {
        this.IsRunning = true;
        _updateThread = new Thread(UpdateLoop);
        _updateThread.Start();
        _renderThread = new Thread(RenderLoop);
        _renderThread.Start();
      }
    }

    /// <summary>
    /// Stops the render cycle
    /// </summary>
    public void StopRender()
    {
      if (this.IsRunning)
      {
        this.IsRunning = false;
        _updateThread.Join();
        _renderThread.Join();
        this.Fps = 0;
        _frameTimes.Clear();
      }
    }

    /// <summary>
    /// Aborts the render cycle
    /// </summary>
    public void AbortRender()
    {
      if (this.IsRunning)
      {
        this.IsRunning = false;
        _updateThread.Abort();
        _renderThread.Abort();
      }
    }

    private Stopwatch _sw = new Stopwatch();

    private void RenderLoop()
    {
      int bufferIndex = 0x0;

      while (this.IsRunning)
      {
        _sw.Restart();
        Render(bufferIndex);
        bufferIndex ^= 0x1;

        double minTime = 1000.0 / this.MaxFps;
        double start = _sw.Elapsed.TotalMilliseconds;
        while (_sw.Elapsed.TotalMilliseconds < start + 20) { } // 20 ms timeout for rendering other UI controls

        while (_sw.Elapsed.TotalMilliseconds < minTime) { }

        _sw.Stop();

        _frameTimes.Add(_sw.Elapsed.TotalMilliseconds);
        int counter = 0;
        int index = _frameTimes.Count - 1;
        double ms = 0;
        while (index >= 0 && ms + _frameTimes[index] <= 1000)
        {
          ms += _frameTimes[index];
          counter++;
          index--;
        }
        if (index > 0)
          _frameTimes.RemoveRange(0, index);
        this.Fps = counter + ((1000 - ms) / _frameTimes[0]);
        Console.WriteLine(this.Fps);
      }
    }

    private void UpdateLoop()
    {
      int bufferIndex = 0x0;
      _currentBufferIndex = 0x1;

      while (this.IsRunning)
      {
        Update(bufferIndex);
        _currentBufferIndex = bufferIndex;
        bufferIndex ^= 0x1;
      }
    }


    private void Render(int bufferIndex)
    {
      _updateHandle[bufferIndex].WaitOne();
      this.Invalidate();
      _renderHandle[bufferIndex].Set();
    }

    private void Update(int bufferIndex)
    {
      _renderHandle[bufferIndex].WaitOne();

      if (this.Moves.Count > 0)
      {
        RotationInfo currentRotation = this.Moves.Peek();

        foreach (AnimatedLayerMove rotation in currentRotation.Moves)
        {
          double step = (double)rotation.Target / (double)((double)(currentRotation.Milliseconds / 1000.0) * (double)(this.Fps));
          this.LayerRotation[rotation.Move.Layer] += step;
        }

        if (RotationIsFinished(currentRotation.Moves))
          this.RotationFinished(this.Moves.Dequeue());
      }
      _buffer[bufferIndex] = this.GenFacesProjected(this.Screen, this.Zoom);
      _updateHandle[bufferIndex].Set();
    }


    private bool RotationIsFinished(List<AnimatedLayerMove> moves)
    {
      foreach (AnimatedLayerMove m in moves)
      {
        if (!(m.Target > 0 && this.LayerRotation[m.Move.Layer] >= m.Target) && !(m.Target < 0 && this.LayerRotation[m.Move.Layer] <= m.Target))
          return false;
      }
      return true;
    }

    private void RotationFinished(RotationInfo move)
    {
      ResetLayerRotation();
      foreach (AnimatedLayerMove m in move.Moves)
      {
        this.Rubik.RotateLayer(new LayerMove(m.Move.Layer, m.Move.Direction, m.Move.Twice));
      }
      _selections.Reset();
      if (Moves.Count < 1) this.MouseHandling = true;
    }
  }
}

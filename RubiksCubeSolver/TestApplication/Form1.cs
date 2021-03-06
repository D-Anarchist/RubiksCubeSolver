﻿using RubiksCubeLib;
using RubiksCubeLib.RubiksCube;
using RubiksCubeLib.Solver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestApplication
{
  public partial class Form1 : Form
  {
    PluginCollection<CubeSolver> solverPlugins = new PluginCollection<CubeSolver>();

    public Form1()
    {
      InitializeComponent();
      foreach (CubeSolver solver in solverPlugins.GetAll())
      {
        solver.OnSolutionFound += ExecuteSolution;
      }
    }

    private void cubeModel_OnSelectionChanged(object sender, RubiksCubeLib.CubeModel.SelectionChangedEventArgs e)
    {
      statusLblSelection.Text = string.Format("[{0}] | {1}", e.Position.CubePosition, e.Position.FacePosition);
    }

    private void loadToolStripMenuItem_Click(object sender, EventArgs e)
    {
      FolderBrowserDialog fbd = new FolderBrowserDialog();
      if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        solverPlugins.AddFolder(fbd.SelectedPath);
      }
    }

    private void solveToolStripMenuItem_Click(object sender, EventArgs e)
    {
      PluginSelectorDialog<CubeSolver> dlg = new PluginSelectorDialog<CubeSolver>(solverPlugins);
      if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        solverPlugins.StandardPlugin.TrySolveAsync(cubeModel.Rubik);
      }
    }

    private void ExecuteSolution(object sender, SolutionFoundEventArgs e)
    {
      if (e.Solvable)
      {
        MessageBox.Show(string.Format("Solution found with {0}: Moves count: {1}; Elapsed Milliseconds {2}", e.Solution.SolvingMethod,e.Solution.MovesCount,e.Milliseconds / 1000));
        e.Solution.Algorithm.Moves.ForEach(m => cubeModel.RotateLayerAnimated(m));
      }
      else MessageBox.Show("Unsolvable");
    }

    private void resetToolStripMenuItem_Click(object sender, EventArgs e)
    {
      cubeModel.ResetCube();
    }

    private void scrambleToolStripMenuItem_Click(object sender, EventArgs e)
    {
      cubeModel.Rubik.Scramble(50);
    }

    private void solveToolStripMenuItem1_Click(object sender, EventArgs e)
    {
      solverPlugins.StandardPlugin.TrySolveAsync(cubeModel.Rubik);
    }

    private void cornerTestToolStripMenuItem_Click(object sender, EventArgs e)
    {
      MessageBox.Show(Solvability.FullTest(cubeModel.Rubik) ? "Solvable" : "Unsolvable");
    }

    private void saveToolStripMenuItem_Click(object sender, EventArgs e)
    {
      using (SaveFileDialog sfd = new SaveFileDialog())
      {
        sfd.Filter = "XML-Files|*.xml";
        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          cubeModel.SavePattern(sfd.FileName);
        }
      }
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
      using (OpenFileDialog ofd = new OpenFileDialog())
      {
        ofd.Filter = "XML-Files|*.xml";
        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          cubeModel.LoadPattern(ofd.FileName);
        }
      }
    }
  }
}

﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using DunGen.Engine;
using DunGen.Engine.Contracts;
using DunGen.Engine.Implementations;
using DunGen.Engine.Models;
using System.Linq;
namespace DunGen.Visualizer
{
    public class ViewModel
    {
        public Dispatcher Dispatcher { get; set; }
        private BindingList<Cell> mCells = new BindingList<Cell>();
        private Thread mWorkerThread;
        public BindingList<Cell> Cells
        {
            get { return mCells; }
            set
            {
                if (mCells == value) return;
                mCells = value;
                RaisePropertyChanged("Cells");
            }
        }

        public bool IsRunning { get; set; }
        private int mWidth;
        public int Width
        {
            get { return mWidth; }
            set
            {
                if (mWidth == value) return;
                mWidth = value;
                RaisePropertyChanged("Width");
            }
        }

        public ICommand GenerateCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        private readonly DunGenerator mGenerator = new DunGenerator();
        private readonly DungeonConfiguration mConfiguration;
        public ViewModel()
        {
            mGenerator.MapChanged += MapChangedHandler;
            mConfiguration = new DungeonConfiguration()
            {
                Height = 20,
                Width = 20,
                //Randomness = 0.2,
                Sparseness = 5

            };
            Width = mConfiguration.Width;

            GenerateCommand = new RelayCommand(StartGeneration);
            RefreshCommand = new RelayCommand(o =>
            {
                var cells = Cells.ToList();
                Cells.Clear();
                foreach (var cell in cells)
                {
                    Cells.Add(cell);
                }
            });


            for (int j = 0; j < mConfiguration.Height; j++)
            {
                for (var i = 0; i < mConfiguration.Width; i++)
                {
                    Cells.Add(new Cell());
                }
            }
        }

        private void StartGeneration(object input)
        {
            if (IsRunning)
            {
                mWorkerThread.Abort();
            }
            Cells.Clear();

            IsRunning = true;
            mWorkerThread = new Thread(() =>
            {
                mGenerator.Generate(mConfiguration);
                IsRunning = false;
            }) { IsBackground = true };
            mWorkerThread.Start();
        }

        void MapChangedHandler(Engine.Contracts.IMapProcessor sender, Engine.Contracts.MapChangedDelegateArgs args)
        {
            DelayForVisualEffect(sender);
            Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(delegate()
                {
                    if (Cells.Count == 0)
                    {
                        for (int j = 0; j < mConfiguration.Height; j++)
                        {
                            for (var i = 0; i < mConfiguration.Width; i++)
                            {
                                Cells.Add(args.Map.GetCell(i, j));
                            }
                        }
                    }
                    else
                    {
                        foreach (var cell in args.CellsChanged)
                        {
                            var index = Cells.IndexOf(cell);
                            Cells.Remove(cell);
                            Cells.Insert(index, cell);
                        }
                    }
                }
            ));
           

        }

        private void DelayForVisualEffect(IMapProcessor sender)
        {
            if (sender == null) return;
            
            if (sender.GetType() == typeof(SparsenessReducer))
                Thread.Sleep(300);
            else
                Thread.Sleep(10);

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
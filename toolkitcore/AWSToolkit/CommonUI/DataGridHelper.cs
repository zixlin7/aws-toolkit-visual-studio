using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using log4net;

namespace Amazon.AWSToolkit.CommonUI
{
    public static class DataGridHelper
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(DataGridHelper));

        public static void PutCellInEditMode(DataGrid grid, int row, int col)
        {
            try
            {
                // Commit anything in process.
                grid.CommitEdit();

                //first focus the grid
                grid.Focus();
                //then create a new cell info, with the item we wish to edit and the column number of the cell we want in edit mode
                DataGridCellInfo cellInfo = new DataGridCellInfo(grid.Items[row], grid.Columns[col]);
                //set the cell to be the active one
                grid.CurrentCell = cellInfo;
                if (grid.SelectedItem != null)
                {
                    //scroll the item into view
                    grid.ScrollIntoView(grid.SelectedItem);
                }
                //begin the edit
                grid.BeginEdit();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error putting grid in edit mode.", e);
            }
        }

        public static void PutCellInEditMode(DataGrid grid, DataGridRow row, int col)
        {
            try
            {
                //first focus the grid
                grid.Focus();
                //then create a new cell info, with the item we wish to edit and the column number of the cell we want in edit mode
                DataGridCellInfo cellInfo = new DataGridCellInfo(row, grid.Columns[col]);
                //set the cell to be the active one

                grid.CurrentCell = cellInfo;
                //scroll the item into view
                grid.ScrollIntoView(grid.SelectedItem);
                //begin the edit
                grid.BeginEdit();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error putting grid in edit mode.", e);
            }
        }

        public static DataGridRow GetSelectedRow(DataGrid grid)
        {
            return (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem);
        }

        public static DataGridRow GetRow(DataGrid grid, int index)
        {
            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // May be virtualized, bring into view and try again.
                grid.UpdateLayout();
                grid.ScrollIntoView(grid.Items[index]);
                row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        public static DataGridCell GetCell(DataGrid grid, DataGridRow row, int column)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);

                if (presenter == null)
                {
                    grid.ScrollIntoView(row, grid.Columns[column]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(row);
                }

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                return cell;
            }
            return null;
        }

        public static DataGridCell GetCell(DataGrid grid, int row, int column)
        {
            try
            {
                DataGridRow rowContainer = GetRow(grid, row);
                return GetCell(grid, rowContainer, column);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error attempting get cell.", e);
                return null;
            }
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        public static T GetVisualParent<T>(Visual child) where T : Visual
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                T p = parent as T;
                if (p != null)
                    return p;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return default(T);
        }

        public static void TurnOffAutoScroll(DataGrid grid)
        {
            var scp = GetVisualChild<ScrollContentPresenter>(grid);
            if (scp != null)
            {
                scp.RequestBringIntoView += (s, e) =>
                {
                    e.Handled = true;
                };
            }
        }

        public static IList<T> GetSelectedItems<T>(DataGrid grid)
        {
            IList<T> items = new List<T>();
            foreach (var obj in grid.SelectedItems)
            {
                if (obj is T)
                {
                    items.Add((T)obj);
                }
            }

            return items;
        }

        public static void SelectAndScrollIntoView(DataGrid grid, object obj)
        {
            grid.SelectedItem = obj;

            ThreadPool.QueueUserWorkItem((WaitCallback)(x =>
            {
                Thread.Sleep(100);
                try
                {
                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                    {
                        grid.ScrollIntoView(obj);
                    }));
                }
                catch { }
            }));
        }
    }
}

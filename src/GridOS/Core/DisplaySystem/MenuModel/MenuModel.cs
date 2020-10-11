﻿using System.Collections.Generic;
using System;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        class MenuModel
        {
            public struct NavigationPayload
            {
                public IMenuGroup NavigatedTo;
                public IMenuGroup NavigatedFrom;
            }

            /// <summary>
            /// The title of the menu group currently open.
            /// </summary>
            public string CurrentTitle => _activeGroup.Label;

            /// <summary>
            /// Notifies when the title of the currently opened group changes.
            /// </summary>
            public event Action<string> CurrentTitleChanged;

            /// <summary>
            /// The content of the menu group currently open.
            /// </summary>
            public IReadOnlyList<IMenuItem> CurrentView => _currentView;

            /// <summary>
            /// Notifies when an item was added to, or removed from, the currently open group.
            /// </summary>
            public event Action<IEnumerable<IMenuItem>> CurrentViewChanged;

            /// <summary>
            /// Notifies when an item has changed in the currently open group.
            /// </summary>
            public event Action<IMenuItem> MenuItemChanged;

            /// <summary>
            /// Notifies when another group has been opened.
            /// </summary>
            public event Action<NavigationPayload> NavigatedTo;

            /// <summary>
            /// The path of the group currently open.
            /// </summary>
            public IEnumerable<string> NavigationPath => _navigationStack.Select(x => x.Label);

            private readonly List<IMenuItem> _currentView;
            private readonly IMenuGroup _rootGroup;
            private IMenuGroup _activeGroup;

            private readonly List<IMenuGroup> _navigationStack = new List<IMenuGroup>(); // Navigation history for back traversal. List is used, because Stack is enumerated backwards, and would be less performant to convert it to FIFO NavigationPath.
            private readonly MenuCommand _backCommandTop;
            private readonly MenuCommand _backCommandBottom; // Separate instance; top and bottom back command shouldn't evaluate to equal.

            public MenuModel(IMenuGroup menuRoot)
            {
                _rootGroup = menuRoot;
                _currentView = new List<IMenuItem>();
                _backCommandTop = new MenuCommand("Back «", MoveBack);
                _backCommandBottom = new MenuCommand("Back «", MoveBack);
                NavigateTo(_rootGroup);
            }

            public void Select(IMenuItem item)
            {
                if (item == null || !CurrentView.Contains(item))
                    return;

                if (item is IMenuGroup)
                {
                    NavigateTo(item as IMenuGroup);
                }
                else if (item is IMenuCommand)
                {
                    (item as IMenuCommand).Execute();
                }
            }

            public int GetIndexOf(IMenuItem item)
            {
                return _currentView.IndexOf(item);
            }

            private void NavigateTo(IMenuGroup group)
            {
                var navPayload = new NavigationPayload()
                { 
                    NavigatedTo = group,
                    NavigatedFrom = _activeGroup
                };

                if (_activeGroup != null)
                    CloseActiveGroup();

                OpenGroup(group);

                NavigatedTo?.Invoke(navPayload);
            }

            private void MoveBack()
            {
                _navigationStack.RemoveAt(_navigationStack.Count - 1); // Pop last group.
                var previousGroup = _navigationStack.Last(); 
                _navigationStack.RemoveAt(_navigationStack.Count - 1); // Pop previous group too, to be able to treat it as a new navigation target.

                NavigateTo(previousGroup);
            }

            private void CloseActiveGroup()
            {
                _activeGroup.LabelChanged -= Handle_GroupTitleChanged;
                _activeGroup.ChildrenChanged -= Handle_ListChanged;
                _activeGroup.ChildLabelChanged -= Handle_ItemChanged;
                _activeGroup.Close();
                _activeGroup = null;

                BuildCurrentView();
            }

            private void OpenGroup(IMenuGroup group)
            {
                _navigationStack.Add(group);
                group.Open();
                group.LabelChanged += Handle_GroupTitleChanged;
                group.ChildrenChanged += Handle_ListChanged;
                group.ChildLabelChanged += Handle_ItemChanged;
                _activeGroup = group;

                BuildCurrentView();
            }

            private void BuildCurrentView()
            {
                _currentView.Clear();

                if (_activeGroup == null)
                    return;

                if (_activeGroup != _rootGroup)
                    _currentView.Add(_backCommandTop);

                _currentView.AddRange(_activeGroup.GetChildren());

                if (_activeGroup.ShowBackCommandAtBottom)
                    _currentView.Add(_backCommandBottom);
            }

            private void Handle_GroupTitleChanged(IMenuItem group)
            {
                CurrentTitleChanged?.Invoke(group.Label);
            }

            private void Handle_ListChanged(IMenuItem _)
            {
                CurrentViewChanged?.Invoke(CurrentView);
            }

            private void Handle_ItemChanged(IMenuItem item)
            {
                MenuItemChanged?.Invoke(item);
            }
        }
    }
}
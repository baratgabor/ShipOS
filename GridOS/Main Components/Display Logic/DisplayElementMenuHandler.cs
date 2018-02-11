﻿using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        class DisplayElementMenuHandler
        {
            private Flush _flush = Flush.All;
            private StringBuilder _builder_Temp = new StringBuilder();
            private StringBuilder _builder_Formatted = new StringBuilder();
            private StringBuilder _builder_FormattedView = new StringBuilder();
            private List<LineInfo> _lineInfo = new List<LineInfo>();
            public event Action MenuChanged;

            private const char _itemBulletDefault = '·';
            private const char _itemBulletSelected = '•';
            private const char _selectedLineBullet = '›';
            private const char _groupItemSuffix = '»';

            private int _selectedLine = 0;
            private List<IDisplayElement> _elements;
            public IDisplayElement SelectedElement => _selectedElement;
            private IDisplayElement _selectedElement;

            // Standard getters & setters, with added book-keeping of what needs to be flushed
            public int MaxWidth { get { return _maxWidth; } set { if (_maxWidth != value) { _maxWidth = value; _flush |= Flush.All; } } }
            private int _maxWidth = 0;
            public int LineHeight { get { return _lineHeight; } set { if (_lineHeight != value) { _lineHeight = value; _flush |= Flush.View; } } }
            private int _lineHeight = 0;
            public int VerticalOffset { get { return _verticalOffset; } set { if (_verticalOffset != value) { _verticalOffset = value; _flush |= Flush.View; } } }
            private int _verticalOffset = 0;
            public int LeftPadding { get { return _leftPadding; } set { if (_leftPadding != value) { _leftPadding = value; _flush |= Flush.All; } } }
            private int _leftPadding = 0;

            // TODO: implement WordWrap switch
            public bool WordWrap { get; set; } = false;

            public void SetMenuElements(List<IDisplayElement> elements)
            {
                _elements = elements;
                _selectedLine = 0;
                _selectedElement = null;
                _verticalOffset = 0;
                _flush = Flush.All;
            }

            public void SetSelectedElement(IDisplayElement element)
            {
                if (element == null)
                    return;

                _selectedElement = element;
                
                // TODO: we probably shouldn't call buildformat directly from here
                BuildFormat();
                _flush = Flush.View;
                AdjustViewport(_selectedLine);
            }

            public void MoveUp()
            {
                if (_selectedLine <= 0)
                    return;

                // TODO: Consider adding error checking to see if _selectedLine is not out of bounds for _lines array

                UpdateSelection(_selectedLine - 1);
            }

            public void MoveDown()
            {
                if ((_flush & Flush.All) != 0)
                {
                    BuildFormat(); // Build the _lines data necessary for safe selection increment
                    _flush &= ~Flush.All;
                }

                if (_selectedLine >= _lineInfo.Count-1)
                    return;

                UpdateSelection(_selectedLine+1);
            }

            private void UpdateSelection(int newSelectedLine)
            {
                // TODO: refactor these bullet changing hacks
                if (_builder_Formatted[_lineInfo[_selectedLine].StartPosition + 1] == _selectedLineBullet)
                    _builder_Formatted[_lineInfo[_selectedLine].StartPosition + 1] = ' ';
                if(_builder_Formatted[_lineInfo[newSelectedLine].StartPosition + 1] == ' ')
                    _builder_Formatted[_lineInfo[newSelectedLine].StartPosition + 1] = _selectedLineBullet;

                // If selected display element needs to be changed
                if (_selectedElement != null && _lineInfo[newSelectedLine].ParentDisplayElement != _selectedElement)
                {
                    // Remove selection bullet from previously selected element
                    _builder_Formatted[_lineInfo[_selectedLine].BulletCharPosition] = _itemBulletDefault;
                    // Add selection bullet to newly selected element
                    _builder_Formatted[_lineInfo[newSelectedLine].BulletCharPosition] = _itemBulletSelected;
                }

                _selectedLine = newSelectedLine;
                _selectedElement = _lineInfo[_selectedLine].ParentDisplayElement;
                AdjustViewport(_selectedLine);
                _flush |= Flush.View;
                MenuChanged?.Invoke();
            }

            private void AdjustViewport(int selectedLine)
            {
                if (selectedLine < _verticalOffset)
                    _verticalOffset--;
                if (selectedLine + 2 > _verticalOffset + _lineHeight)
                {
                    _verticalOffset = selectedLine + 2 - _lineHeight;
                }
            }

            public string GetContent()
            {
                switch (_flush)
                {
                    case Flush.None:
                        break;
                    case Flush.All:
                        BuildFormat();
                        BuildView();
                        break;
                    case Flush.All | Flush.View:
                        BuildFormat();
                        BuildView();
                        break;
                    case Flush.View:
                        BuildView();
                        break;
                }

                _flush = Flush.None;

                return _builder_FormattedView.ToString();
            }

            public void FlushContent()
            {
                _flush = Flush.All;
            }

            // TODO: Try refactor, make it pretty
            private void BuildView()
            {
                _builder_FormattedView.Clear();

                if (LineHeight > 0 && LineHeight < _lineInfo.Count)
                {
                    // Get a portion of full result, based on VerticalOffset and MaxLines - aka "viewport content"
                    int ViewPortStart = _lineInfo[VerticalOffset].StartPosition;
                    int ViewPortEnd;

                    int ContentEnd = VerticalOffset + LineHeight;
                    if (ContentEnd > _lineInfo.Count - 1)
                    {
                        ContentEnd = _lineInfo.Count - 1;
                        ViewPortEnd = _builder_Formatted.Length;
                    }
                    else
                    {
                        ViewPortEnd = _lineInfo[ContentEnd].StartPosition;
                    }

                    for (int i = ViewPortStart; i < ViewPortEnd; i++)
                    {
                        _builder_FormattedView.Append(_builder_Formatted[i]);
                    }

                    if (_builder_FormattedView[_builder_FormattedView.Length - 2] == '\r')
                    {
                        _builder_FormattedView.Remove(_builder_FormattedView.Length - 2, 2);
                    }
                }
                else // Get full content
                {
                    for (int i = 0; i < _builder_Formatted.Length; i++)
                    {
                        _builder_FormattedView.Append(_builder_Formatted[i]);
                    }
                }
            }

            private string BuildDefaultBulletLinePrefix()
            {
                _builder_Temp.Clear();

                if (LeftPadding >= 2)
                {
                    _builder_Temp.Clear();
                    for (int i = 0; i < LeftPadding-2; i++)
                    {
                        _builder_Temp.Append(' ');
                    }
                }
                _builder_Temp.Append(_itemBulletDefault + " ");
                return _builder_Temp.ToString();
            }

            private string BuildSelectedBulletLinePrefix()
            {
                _builder_Temp.Clear();

                if (LeftPadding >= 2)
                {
                    _builder_Temp.Clear();
                    for (int i = 0; i < LeftPadding - 2; i++)
                    {
                        _builder_Temp.Append(' ');
                    }
                }
                _builder_Temp.Append(_itemBulletSelected + " ");
                return _builder_Temp.ToString();
            }

            private string BuildPaddingLinePrefix()
            {
                if (LeftPadding > 0)
                    return new string(' ', LeftPadding);
                else
                    return null;
            }

            // TODO: Need to keep track of previous line number, and update vertical offset accordingly
            // e.g. if there are less lines by 2 after the content changes, vertical offset should be decreased by 2
            // BUT ONLY if the decrease was above the displayed content :D :D how to implement that?
            // TODO: Refactor method below; it's way too long
            private void BuildFormat()
            {
                _builder_Formatted.Clear();
                _lineInfo.Clear();

                // Create prefix strings to be added in front of lines
                // TODO: cache prefix strings, and only create them if they need to be flushed due to property changes
                string leftPaddingStr = BuildPaddingLinePrefix();
                string defaultBulletPrefix = BuildDefaultBulletLinePrefix();
                string selectedBulletPrefix = BuildSelectedBulletLinePrefix();

                foreach (var element in _elements)
                {
                    int lineWidth = 0;
                    int bulletCharPosition;

                    // TODO: not the responsibility of the build; move this to somewhere sane
                    if (_selectedElement == null)
                        _selectedElement = element;

                    // Add appropriate special prefix in front of element
                    // TODO: refactor to remove repetition
                    if (element == _selectedElement)
                    {
                        _builder_Formatted.Append(selectedBulletPrefix);
                        lineWidth += selectedBulletPrefix.Length;
                        bulletCharPosition = _builder_Formatted.Length - 2;

                        // TODO: shouldn't be here, resolve in a sane manner
                        _selectedLine = _lineInfo.Count;
                    }
                    else
                    {
                        _builder_Formatted.Append(defaultBulletPrefix);
                        lineWidth += defaultBulletPrefix.Length;
                        bulletCharPosition = _builder_Formatted.Length - 2;
                    }

                    // Save line data for subsequent operations
                    _lineInfo.Add(new LineInfo(
                        startPosition: _builder_Formatted.Length - defaultBulletPrefix.Length,
                        // TODO: solve temporal conflict: startPosition needs to be saved BEFORE adding prefix string; bulletCharPosition is saved AFTER prefix already added
                        parentDisplayElement: element,
                        bulletCharPosition: bulletCharPosition
                    ));

                    string elementString = element.Label;

                    // Enumerate raw stringbuilder as char array
                    for (int i = 0, lastWhiteSpace = 0; i < elementString.Length; i++)
                    {
                        char c = elementString[i];

                        // If newline found, reset line width counter
                        if (c == '\r')
                        {
                            lineWidth = 0;
                            lastWhiteSpace = 0; // 0 is a magic number for "none" :'(

                            // Replicate newline in result builder
                            _builder_Formatted.Append(Environment.NewLine);

                            // Save line data for subsequent operations
                            _lineInfo.Add(new LineInfo(
                                startPosition: _builder_Formatted.Length,
                                parentDisplayElement: element,
                                bulletCharPosition: bulletCharPosition
                                ));

                            // Add default left padding
                            _builder_Formatted.Append(leftPaddingStr);
                            lineWidth += leftPaddingStr.Length;

                            // If two-character newline sequence (\r\n) found, skip second character
                            if ((i + 1 < elementString.Length) && (elementString[i + 1] == '\n'))
                                i++;
                        }
                        else
                        {
                            if (Char.IsWhiteSpace(c))
                                lastWhiteSpace = _builder_Formatted.Length;

                            // Line too long, break it
                            else if (MaxWidth > 0 && lineWidth >= MaxWidth)
                            {
                                int newlineStartPosition = 0;

                                // Simple linebreak at current position:
                                // Max length reached at a whitespace char OR we don't have last whitespace ("word" longer than max line length)
                                if ((lastWhiteSpace == _builder_Formatted.Length) || (lastWhiteSpace == 0))
                                {
                                    _builder_Formatted.Append(Environment.NewLine + leftPaddingStr);
                                    newlineStartPosition = _builder_Formatted.Length - leftPaddingStr.Length;
                                    lineWidth = leftPaddingStr.Length;
                                }

                                // Smart linebreak at last whitespace:
                                // Max length reached at mid-word; break at last known whitespace position
                                else if (lastWhiteSpace != 0)
                                {
                                    _builder_Formatted.Insert(lastWhiteSpace + 1, Environment.NewLine + leftPaddingStr);
                                    newlineStartPosition = lastWhiteSpace + 1 + 2; // +2 is to take into account the length of \r\n
                                    lineWidth = _builder_Formatted.Length - lastWhiteSpace - 2;
                                }

                                lastWhiteSpace = 0;
                                _lineInfo.Add(new LineInfo(
                                    startPosition: newlineStartPosition,
                                    parentDisplayElement: element,
                                    bulletCharPosition: bulletCharPosition
                                    ));
                            }

                            // Add the next character to the resulting stringbuilder
                            _builder_Formatted.Append(c);
                            lineWidth++;
                        }
                    }
                    // TODO: think about some same place for this; not really the responsibility of format builder
                    if (element is IDisplayGroup)
                        _builder_Formatted.Append(" " + _groupItemSuffix);

                    // Add newline after menu element
                    _builder_Formatted.Append(Environment.NewLine);
                }
            }

            [Flags]
            private enum Flush
            {
                None,
                View,
                All
            }

            private struct LineInfo
            {
                public readonly int StartPosition;
                public readonly IDisplayElement ParentDisplayElement;
                public readonly int BulletCharPosition;

                public LineInfo(int startPosition, IDisplayElement parentDisplayElement, int bulletCharPosition)
                {
                    StartPosition = startPosition;
                    ParentDisplayElement = parentDisplayElement;
                    BulletCharPosition = bulletCharPosition;
                }
            }
        }
    }
}
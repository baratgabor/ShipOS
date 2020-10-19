﻿namespace IngameScript
{
    interface IDisplayConfig
    {
        float FontSize { get; }
        string FontName { get;}

        Color FontColor { get; }
        Color BackgroundColor { get; }

        IMyTextSurface OutputSurface { get; }
        float OutputWidth { get; }
        float OutputHeight { get; }
        int OutputLineCapacity { get; }
    }

    interface IMenuPresentationConfig
    {
        int MenuLines { get; }
        int PaddingLeft { get; }
        char PaddingChar { get; }
        char SelectionMarker { get; }
        
        AffixConfig Prefixes_Unselected { get; }
        AffixConfig Prefixes_Selected { get; }
        AffixConfig Suffixes { get; }
    }

    interface IBreadcrumbConfig
    {
        string PathSeparator { get; }
        string SeparatorLineTop { get; }
        string SeparatorLineBottom { get; }
        int PaddingLeft { get; }
        char PaddingChar { get; }
    }
}
